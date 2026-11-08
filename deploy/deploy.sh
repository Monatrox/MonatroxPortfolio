#!/bin/bash
# Install root-owned as /usr/local/sbin/monatrox-portfolio-deploy. Never execute a CI-uploaded Compose file as root.
set -Eeuo pipefail
export PATH=/usr/sbin:/usr/bin:/sbin:/bin
export DOCKER_CONFIG=/root/.docker
export DOCKER_HOST=unix:///var/run/docker.sock
unset DOCKER_CONTEXT DOCKER_TLS_VERIFY DOCKER_CERT_PATH COMPOSE_FILE COMPOSE_PROJECT_NAME COMPOSE_PROFILES
umask 077
if [[ $# != 1 || ! $1 =~ ^[0-9a-f]{40}$ ]]; then
  printf 'Usage: monatrox-portfolio-deploy <40-character-commit-sha>\n' >&2
  exit 2
fi
if [[ $EUID != 0 ]]; then printf 'Run through the approved root deployment command.\n' >&2; exit 1; fi
configuration=/etc/monatrox-portfolio
state=/var/lib/monatrox-portfolio
database="$state/data/portfolio.db"
release="$1"
for file in "$configuration" "$state" "$configuration/deployment.env" "$configuration/compose.yaml" "$configuration/api.env"; do
  if [[ ! -e $file || -L $file || $(stat -c %u "$file") != 0 ]] || (( (8#$(stat -c %a "$file") & 022) != 0 )); then
    printf 'Configuration must be root-owned and not group/other writable: %s\n' "$file" >&2
    exit 1
  fi
done
# This configuration is administrator-owned, never supplied by SSH or CI.
# shellcheck source=/dev/null
source "$configuration/deployment.env"
if [[ ! ${IMAGE_PREFIX:-} =~ ^ghcr\.io/[a-z0-9][a-z0-9/_-]+$ || ! ${PUBLIC_HOST:-} =~ ^[a-z0-9][a-z0-9.-]+$ ]]; then
  printf 'Invalid registry prefix or public hostname.\n' >&2; exit 1
fi
exec 9>"$state/deploy.lock"
flock -n 9 || { printf 'Another deployment is running.\n' >&2; exit 1; }
if systemctl is-active --quiet MonatroxPortfolio.service; then
  printf 'Old frontend still owns port 44350. First cutover needs an administrator; nothing stopped.\n' >&2
  exit 1
fi
previous=""
if [[ -f $state/current ]]; then
  previous="$(< "$state/current")"
  [[ $previous =~ ^[0-9a-f]{40}$ ]] || { printf 'Invalid recorded release.\n' >&2; exit 1; }
fi
compose() {
  IMAGE_PREFIX="$IMAGE_PREFIX" RELEASE_SHA="$release" docker compose \
    --project-directory "$configuration" -f "$configuration/compose.yaml" --profile maintenance "$@"
}
wait_for() {
  for _ in {1..30}; do
    if curl --fail --silent --noproxy '*' --max-time 3 "$1" >/dev/null; then return 0; fi
    sleep 1
  done
  return 1
}
changing=false
backup=""
had_database=false
restore_database() {
  # Stop every writer before replacing database files; mv replaces a symlink rather than following it.
  compose stop web api || return 1
  if [[ -n $backup ]]; then
    rm -f -- "$database-wal" "$database-shm" "$database-journal"
    local restored
    restored="$(mktemp "$state/data/.restore.XXXXXX")" || return 1
    cp -- "$backup" "$restored" && chown 1654:1654 "$restored" && chmod 640 "$restored" && mv -fT -- "$restored" "$database"
  elif [[ $had_database == false ]]; then
    rm -f -- "$database" "$database-wal" "$database-shm" "$database-journal"
  fi
}
failed() {
  local status="$1" recovered=true
  # Command substitutions report failure to the parent; only the parent may roll back.
  if (( BASH_SUBSHELL > 0 )); then exit "$status"; fi
  trap - ERR INT TERM HUP
  set +e
  if [[ $changing == true ]]; then
    if ! restore_database; then recovered=false; fi
    if [[ $recovered == true && -n $previous ]]; then
      release="$previous"
      if ! compose up -d api || ! wait_for http://127.0.0.1:44352/api/profile || ! compose up -d web || ! wait_for http://127.0.0.1:44350/; then recovered=false; fi
    fi
    if [[ $recovered == true && -n $previous ]]; then
      if ! printf '%s\n' "$previous" > "$state/current"; then recovered=false; fi
    elif [[ $recovered == true ]]; then
      if ! rm -f -- "$state/current"; then recovered=false; fi
    fi
    if [[ $recovered == true && -n $previous ]]; then
      printf 'Deployment failed; previous images and pre-deployment database restored.\n' >&2
    elif [[ $recovered == true ]]; then
      printf 'First deployment failed; new containers stopped. Administrator must restart old frontend.\n' >&2
    else
      printf 'CRITICAL: rollback incomplete. Operator intervention required; backups retained.\n' >&2
    fi
  fi
  exit "$status"
}
trap 'failed $?' ERR
trap 'failed 130' INT
trap 'failed 143' TERM
trap 'failed 129' HUP
# Pull and inspect before interrupting the running release.
compose pull api web tools
for service in api web tools; do
  revision="$(docker image inspect "$IMAGE_PREFIX-$service:$release" --format '{{ index .Config.Labels "org.opencontainers.image.revision" }}')"
  [[ $revision == "$release" ]] || { printf 'Image revision mismatch: %s\n' "$service" >&2; exit 1; }
done
changing=true
compose stop web api
if [[ -e $database || -L $database ]]; then
  had_database=true
  [[ -f $database && ! -L $database ]] || { printf 'Refusing non-regular database file.\n' >&2; false; }
  candidate="$state/backups/$(date -u +%Y%m%dT%H%M%SZ)-$release.db"
  python3 - "$database" "$candidate" <<'PY'
import os, sqlite3, sys
with sqlite3.connect('file:' + sys.argv[1] + '?mode=ro', uri=True) as source:
    with sqlite3.connect(sys.argv[2]) as target:
        source.backup(target)
        if target.execute('PRAGMA quick_check').fetchall() != [('ok',)]:
            raise RuntimeError('Database backup integrity check failed')
os.chmod(sys.argv[2], 0o600)
PY
  backup="$candidate"
fi
compose run --rm --no-deps tools --import /app/content
compose up -d api
wait_for http://127.0.0.1:44352/api/profile
curl --fail --silent --noproxy '*' --max-time 10 http://127.0.0.1:44352/api/projects/portfolio/page >/dev/null
compose up -d web
wait_for http://127.0.0.1:44350/
# Verify the existing HTTPS route, not just the containers' process health. No certificate changes.
html="$(curl --fail --silent --noproxy '*' --max-time 15 --resolve "$PUBLIC_HOST:443:127.0.0.1" "https://$PUBLIC_HOST/projects/portfolio")"
grep -Fq 'Blazor-WebAssembly-Component-State:' <<< "$html"
record="$(mktemp "$state/.current.XXXXXX")"
printf '%s\n' "$release" > "$record"
mv -fT -- "$record" "$state/current"
trap - ERR INT TERM HUP
printf 'Deployed %s. Previous images and database backup retained.\n' "$release"
