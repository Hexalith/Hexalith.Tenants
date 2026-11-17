#!/usr/bin/env bash
set -euo pipefail

if [[ "${1:-}" == cleanup ]]; then
    progress_directory="${2:-}"
    runner_temp="${RUNNER_TEMP:?A canonical RUNNER_TEMP is required.}"
    [[ "$runner_temp" == "$(realpath -- "$runner_temp")" && "$runner_temp" != / &&
       "$progress_directory" =~ ^"$runner_temp"/tenants-performance-progress-[[:alnum:]]{8}$ &&
       -d "$progress_directory" && ! -L "$progress_directory" &&
       "$progress_directory" == "$(realpath -- "$progress_directory")" &&
       "$(stat -c %a -- "$progress_directory")" == 700 &&
       -f "$progress_directory/owner" && ! -L "$progress_directory/owner" &&
       "$(stat -c %a -- "$progress_directory/owner")" == 600 ]] || { echo 'performance-progress-cleanup-invalid' >&2; exit 1; }
    cmp -s "$progress_directory/owner" <(printf 'tenants-performance-progress-v1\n') || { echo 'performance-progress-cleanup-invalid' >&2; exit 1; }
    rm -rf -- "$progress_directory"
    exit
fi

# The retained tier has daily cadence. Central release scheduling invokes this
# hook; new cadences must name another real product tier instead of rerunning CI.
[[ "${1:-}" == daily ]] || { echo 'The scheduled product tier requires daily cadence.' >&2; exit 1; }
runner_temp="${RUNNER_TEMP:?A canonical RUNNER_TEMP is required.}"
[[ "$runner_temp" == "$(realpath -- "$runner_temp")" && "$runner_temp" != / ]] || { echo 'performance-progress-root-invalid' >&2; exit 1; }
umask 077
progress_directory="$(mktemp -d "$runner_temp/tenants-performance-progress-XXXXXXXX")"
printf 'tenants-performance-progress-v1\n' > "$progress_directory/owner"
printf '{"schemaVersion":1,"phase":"RunnerStarting","status":"incomplete"}\n' > "$progress_directory/progress.json"
export HEXALITH_TENANTS_PERFORMANCE_PROGRESS_DIRECTORY="$progress_directory"
if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
    printf 'progress_directory=%s\nprogress_path=%s/progress.json\n' "$progress_directory" "$progress_directory" >> "$GITHUB_OUTPUT"
fi
HEXALITH_EVENTSTORE_RUN_PERFORMANCE_TESTS=1 HEXALITH_TENANTS_RUN_PERFORMANCE_TESTS=1 \
    dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj \
        --configuration Release --no-build --no-restore --stop-on-fail off \
        --filter-trait Category=Performance --minimum-expected-tests 1 --fail-skips on \
        --results-directory TestResults/performance \
        --report-xunit-trx --report-xunit-trx-filename perf-results.trx
