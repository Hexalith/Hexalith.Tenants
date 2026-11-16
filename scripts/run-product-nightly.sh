#!/usr/bin/env bash
set -euo pipefail

# The retained tier has daily cadence. Central release scheduling invokes this
# hook; new cadences must name another real product tier instead of rerunning CI.
[[ "${1:-}" == daily ]] || { echo 'The scheduled product tier requires daily cadence.' >&2; exit 1; }
HEXALITH_EVENTSTORE_RUN_PERFORMANCE_TESTS=1 HEXALITH_TENANTS_RUN_PERFORMANCE_TESTS=1 \
    dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj \
        --configuration Release --no-build --no-restore --stop-on-fail off \
        --filter-trait Category=Performance --minimum-expected-tests 1 --fail-skips on \
        --results-directory TestResults/performance \
        --report-xunit-trx --report-xunit-trx-filename perf-results.trx
