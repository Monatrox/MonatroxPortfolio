#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
if [[ $# != 2 || ! $1 =~ ^[a-z0-9][a-z0-9./_-]*$ || ! $2 =~ ^[0-9a-f]{40}$ ]]; then
  printf 'Usage: %s <image-prefix> <commit-sha>\n' "$0" >&2
  exit 2
fi
engine="${CONTAINER_ENGINE:-docker}"
prefix="$1"
sha="$2"
name="portfolio-ci-${sha:0:12}-$$"
temporary="$(mktemp -d)"
mkdir "$temporary/data" "$temporary/keys"
common=(--read-only --tmpfs /tmp --cap-drop ALL --security-opt no-new-privileges)
cleanup() {
  result=$?
  trap - EXIT
  for service in web api smtp; do
    "$engine" logs "$name-$service" > "$temporary/$service.log" 2>&1 || true
    "$engine" rm -f "$name-$service" >/dev/null 2>&1 || true
  done
  "$engine" network rm "$name" >/dev/null 2>&1 || true
  mkdir -p artifacts/ci-logs
  cp "$temporary/"*.log artifacts/ci-logs/ || true
  "$engine" run --rm --user 0 --entrypoint sh -v "$temporary:/cleanup:z" "$prefix-tools:$sha" \
    -c 'rm -rf /cleanup/data /cleanup/keys' >/dev/null 2>&1 || true
  rm -rf -- "$temporary"
  exit "$result"
}
trap cleanup EXIT
"$engine" run --rm --user 0 --entrypoint sh -v "$temporary:/prepare:z" "$prefix-tools:$sha" \
  -c 'chown 1654:1654 /prepare/data /prepare/keys'
connection='Data Source=/data/portfolio.db;Foreign Keys=True'
"$engine" run --rm "${common[@]}" -v "$temporary/data:/data:z" \
  -e "ConnectionStrings__Portfolio=$connection" "$prefix-tools:$sha" --import /app/content
wait_for() {
  for _ in {1..60}; do
    if curl --fail --silent --max-time 2 "$1" >/dev/null; then return; fi
    sleep 1
  done
  printf 'Timed out waiting for %s\n' "$1" >&2
  return 1
}
# First check Production defaults, bridge DNS, both localhost bindings, and disabled outbound mail.
"$engine" network create "$name" >/dev/null
"$engine" run -d --name "$name-api" --network "$name" --network-alias api "${common[@]}" \
  -p 127.0.0.1:19510:8080 -v "$temporary/data:/data:z" \
  -e "ConnectionStrings__Portfolio=$connection" "$prefix-api:$sha" >/dev/null
wait_for http://127.0.0.1:19510/api/profile
"$engine" run -d --name "$name-web" --network "$name" "${common[@]}" \
  -p 127.0.0.1:19520:8080 -p '[::1]:19520:8080' \
  -v "$temporary/keys:/home/app/.aspnet/DataProtection-Keys:z" \
  -e Api__BaseUrl=http://api:8080 "$prefix-web:$sha" >/dev/null
wait_for http://127.0.0.1:19520/
html="$(curl --fail --silent --max-time 15 --noproxy '*' 'http://[::1]:19520/projects/portfolio')"
grep -Fq 'Blazor-WebAssembly-Component-State:' <<< "$html"
curl --fail --silent http://127.0.0.1:19520/api/contact \
  | python3 -c 'import json,sys; info=json.load(sys.stdin); assert info["available"] is False and info["capturedLocally"] is False'
for service in api web; do
  "$engine" logs "$name-$service" > "$temporary/production-$service.log" 2>&1
  "$engine" rm -f "$name-$service" >/dev/null
done
"$engine" run -d --name "$name-smtp" -p 127.0.0.1:19525:25 -p 127.0.0.1:19530:80 \
  -e ServerOptions__Urls=http://0.0.0.0:80 docker.io/rnwood/smtp4dev:3.16.1 >/dev/null
wait_for http://127.0.0.1:19530/
# Capture is loopback-only. Host networking gives the isolated API its own test-only SMTP port.
"$engine" run -d --name "$name-api" --network host "${common[@]}" \
  -v "$temporary/data:/data:z" -e "ConnectionStrings__Portfolio=$connection" \
  -e ASPNETCORE_ENVIRONMENT=Test -e DOTNET_ENVIRONMENT=Test \
  -e Smtp__Mode=Capture -e Smtp__Host=127.0.0.1 -e Smtp__Port=19525 \
  -e Smtp__Security=None -e 'Smtp__From=Portfolio <portfolio@monatrox.invalid>' \
  "$prefix-api:$sha" --urls http://127.0.0.1:19510 >/dev/null
wait_for http://127.0.0.1:19510/api/profile
"$engine" run -d --name "$name-web" --network host "${common[@]}" \
  -v "$temporary/keys:/home/app/.aspnet/DataProtection-Keys:z" \
  -e Api__BaseUrl=http://127.0.0.1:19510 "$prefix-web:$sha" --urls http://127.0.0.1:19520 >/dev/null
wait_for http://127.0.0.1:19520/
dotnet run --project Tests/MonatroxPortfolio.BrowserTests --no-build -- http://127.0.0.1:19520
