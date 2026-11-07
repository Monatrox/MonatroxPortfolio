#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
test -f MonatroxPortfolio.slnx
bash -n scripts/dev.sh scripts/verify.sh
dotnet tool restore
dotnet restore MonatroxPortfolio.slnx
dotnet build MonatroxPortfolio.slnx --no-restore
dotnet test MonatroxPortfolio.slnx --no-build --no-restore
# Design-time model checks do not open or mutate this database.
export ConnectionStrings__Portfolio="Data Source=:memory:"
dotnet ef migrations has-pending-model-changes --no-build --project Modules/Profile/MonatroxPortfolio.Profile.Infrastructure
dotnet ef migrations has-pending-model-changes --no-build --project Modules/Projects/MonatroxPortfolio.Projects.Infrastructure
if [[ -n "${PORTFOLIO_BROWSER_URL:-}" ]]; then
    dotnet run --project Tests/MonatroxPortfolio.BrowserTests --no-build -- "$PORTFOLIO_BROWSER_URL"
else
    printf '\nBrowser smoke not run. Start imported app, then rerun with PORTFOLIO_BROWSER_URL=http://127.0.0.1:5420\n'
fi
