#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
if [[ $# != 2 || ! $1 =~ ^[a-z0-9][a-z0-9./_-]*$ || ! $2 =~ ^[0-9a-f]{40}$ ]]; then
  printf 'Usage: %s <lowercase-image-prefix> <40-character-commit-sha>\n' "$0" >&2
  exit 2
fi
engine="${CONTAINER_ENGINE:-docker}"
for target in web api tools; do
  test -d "artifacts/containers/$target"
  "$engine" build --platform linux/amd64 --target "$target" -f deploy/Dockerfile \
    --label "org.opencontainers.image.revision=$2" \
    --label "org.opencontainers.image.source=https://github.com/${GITHUB_REPOSITORY:-Monatrox/MonatroxPortfolio}" \
    --tag "$1-$target:$2" artifacts/containers
done
