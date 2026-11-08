#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p data
export ConnectionStrings__Portfolio="${ConnectionStrings__Portfolio:-Data Source=$PWD/data/portfolio.db;Foreign Keys=True}"
case "${1:-run}" in
  validate)
    dotnet run --project Hosts/MonatroxPortfolio.Migrations -- --validate "$PWD/content"
    ;;
  import)
    dotnet run --project Hosts/MonatroxPortfolio.Migrations -- --import "$PWD/content"
    ;;
  migrate)
    dotnet run --project Hosts/MonatroxPortfolio.Migrations
    ;;
  run)
    export ASPIRE_SUPPRESS_CLI_RUN_HOOK=true
    exec dotnet run --project Hosts/MonatroxPortfolio.AppHost
    ;;
  *) printf 'Usage: %s [run|migrate|validate|import]\n' "$0" >&2; exit 1 ;;
esac
