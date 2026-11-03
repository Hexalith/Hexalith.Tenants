#!/usr/bin/env bash
set -euo pipefail

# The current catalog topology is qualified on fresh, isolated backends by the
# authenticated-routes matrix and the source-reference owner-profile job.
# This tier retains every in-process and slim Dapr actor integration test.
test "${ASPIRE_TEST_PROJECT:-}" = tests/Hexalith.Tenants.IntegrationTests
mkdir -p TestResults/aspire
dotnet test "$ASPIRE_TEST_PROJECT" \
    --no-build --configuration Release \
    --filter-not-trait Category=Performance \
    --filter-not-class \
        Hexalith.Tenants.IntegrationTests.AspireTopologyTests \
        Hexalith.Tenants.IntegrationTests.TenantsUiRouteSmokeTests \
        Hexalith.Tenants.IntegrationTests.AuthenticatedTenantsRouteTests \
        Hexalith.Tenants.IntegrationTests.AuthenticatedTenantsUiRouteTests \
        Hexalith.Tenants.IntegrationTests.SourceCatalogOwnerProfileTests \
    --minimum-expected-tests 669 --fail-skips on \
    --results-directory TestResults/aspire \
    --report-xunit-trx --report-xunit-trx-filename aspire-results.trx
