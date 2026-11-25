#!/usr/bin/env bash
set -euo pipefail

# Contracts.Tests owns the executable recovery, destination and NuGet range
# suites. These remaining product suites have no second CI owner.
for suite in \
    test_bootstrap_handoff.py \
    test_catalog_artifact_guards.py \
    test_catalog_package_consumer.py \
    test_create_synthetic_tenant.py \
    test_dapr_read_policy_evidence.py \
    test_hosted_package_profile.py \
    test_release_validation_context.py \
    test_tenant_audit_source_provenance.py; do
    python3 "tests/scripts/$suite"
done
python3 -m unittest discover -s scripts/tests -p 'test_*.py' -v
node --test \
    tests/Hexalith.Tenants.UI.Tests/Browser/authenticated-live-route-waits.test.mjs \
    tests/Hexalith.Tenants.UI.Tests/Browser/authenticated-live-route-evidence.test.mjs \
    tests/Hexalith.Tenants.UI.Tests/Browser/published-frontcomposer-evidence.test.mjs

# Use the browser installed centrally for the locked functional workspace.
export CHROMIUM_BIN
CHROMIUM_BIN="$(node -e 'const {createRequire}=require("node:module");const {resolve}=require("node:path");const load=createRequire(resolve("tests/performance/tenant-audit/package.json"));console.log(load("@playwright/test").chromium.executablePath());')"
TENANTS_BROWSER_BUILD_CONFIGURATION=Release tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh

# Pack the existing Release assets and prove both archive integrity and actual
# package consumers. No solution rebuild or publication occurs in this hook.
package_directory="${RUNNER_TEMP}/tenants-product-packages"
mkdir "$package_directory"
for package in Contracts Client Server Testing Aspire; do
    dotnet pack "src/Hexalith.Tenants.${package}/Hexalith.Tenants.${package}.csproj" \
        --configuration Release --no-build --no-restore --output "$package_directory" \
        -p:Version="0.0.0-ci.${GITHUB_RUN_ID}" -p:RepositoryCommit="$GITHUB_SHA" \
        -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true
done
python3 scripts/validate-nuget-packages.py "$package_directory"
python3 scripts/verify-catalog-package-consumer.py "$package_directory" \
    --expected-source-sha "$GITHUB_SHA" --output "${RUNNER_TEMP}/tenants-product-catalog-consumer.json"
python3 scripts/validate-consumer-package-references.py "$package_directory" \
    --work-directory "${RUNNER_TEMP}/tenants-product-package-consumers"
