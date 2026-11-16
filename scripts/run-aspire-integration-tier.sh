#!/usr/bin/env bash
set -euo pipefail

# The current catalog topology is qualified on fresh, isolated backends by the
# serial catalog profiles in scripts/run-product-integration.py.
# This tier retains every in-process and slim Dapr actor integration test.
mkdir -p TestResults/aspire
dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj \
    --no-build --no-restore --configuration Release --stop-on-fail off \
    --filter-not-trait Category=Performance \
    --filter-not-class \
        Hexalith.Tenants.IntegrationTests.AspireTopologyTests \
        Hexalith.Tenants.IntegrationTests.TenantsUiRouteSmokeTests \
        Hexalith.Tenants.IntegrationTests.AuthenticatedTenantsRouteTests \
        Hexalith.Tenants.IntegrationTests.AuthenticatedTenantsUiRouteTests \
        Hexalith.Tenants.IntegrationTests.SourceCatalogOwnerProfileTests \
    --minimum-expected-tests 671 --fail-skips on \
    --results-directory TestResults/aspire \
    --report-xunit-trx --report-xunit-trx-filename aspire-results.trx
