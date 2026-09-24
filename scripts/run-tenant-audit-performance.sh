#!/usr/bin/env bash
set -euo pipefail

repo_root=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
apphost_project="$repo_root/src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj"
apphost_dll="$repo_root/src/Hexalith.Tenants.AppHost/bin/Release/net10.0/Hexalith.Tenants.AppHost.dll"
performance_dir="$repo_root/tests/performance/tenant-audit"
memories_server_project="$repo_root/references/Hexalith.Memories/src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj"
result_dir=${AUDIT_PERF_RESULT_DIR:-"$repo_root/_bmad-output/implementation-artifacts/story-5-1-performance-run-$(date -u +%Y%m%dT%H%M%SZ)"}
fallback_source=${AUDIT_PERF_FALLBACK_FROM:-}
smoke=${AUDIT_PERF_SMOKE:-0}
apphost_pid=
apphost_log=$(mktemp)

cleanup() {
    trap - EXIT INT TERM
    unset AUDIT_PERF_USERNAME AUDIT_PERF_PASSWORD
    if [[ -n "$apphost_pid" ]] && kill -0 "$apphost_pid" 2>/dev/null; then
        kill -TERM "$apphost_pid" 2>/dev/null || true
        for _ in {1..30}; do
            kill -0 "$apphost_pid" 2>/dev/null || break
            sleep 1
        done
        if kill -0 "$apphost_pid" 2>/dev/null; then
            kill -KILL "$apphost_pid" 2>/dev/null || true
        fi
        wait "$apphost_pid" 2>/dev/null || true
    fi
    rm -f "$apphost_log"
}
trap cleanup EXIT INT TERM

if [[ -n "$fallback_source" ]]; then
    if [[ ! -d "$fallback_source" ]]; then
        printf '%s\n' 'Fallback source directory does not exist.' >&2
        exit 1
    fi
    fallback_source=$(realpath -e "$fallback_source")
    if [[ $(realpath -m "$result_dir") == "$fallback_source" ]]; then
        printf '%s\n' 'Fallback source and result directories must differ.' >&2
        exit 1
    fi
fi
if [[ -d "$result_dir" && -n $(find "$result_dir" -mindepth 1 -maxdepth 1 -print -quit) ]]; then
    printf '%s\n' 'Result directory must be empty to preserve earlier evidence.' >&2
    exit 1
fi
mkdir -p "$result_dir"
result_dir=$(cd "$result_dir" && pwd)
printf '%s\n' 'scripts/run-tenant-audit-performance.sh' > "$result_dir/command.txt"

cd "$repo_root"
if ! node "$performance_dir/prepare-run.mjs" "$result_dir" \
    "$fallback_source" "${AUDIT_PERF_PAGE_SIZE:-}" > "$result_dir/run-mode.json"; then
    printf '%s\n' 'Audit measurement mode validation failed.' > "$result_dir/setup-failure.txt"
    exit 1
fi
audit_mode=$(jq -r '.mode' "$result_dir/run-mode.json")
ui_page_size=$(jq -r '.uiPageSize' "$result_dir/run-mode.json")
if [[ $(aspire ps --format Json | jq 'length') -ne 0 ]]; then
    printf '%s\n' 'An AppHost is already running. Use an isolated, idle runner.' | tee "$result_dir/setup-failure.txt" >&2
    exit 1
fi

cpu_count=$(nproc)
host_cpu_count=$(awk '/^cpu[0-9]+ / { count++ } END { print count + 0 }' /proc/stat)
memory_kib=$(awk '/^MemTotal:/ { print $2 }' /proc/meminfo)
hardware_match=false
if [[ "$cpu_count" -eq 4 && "$host_cpu_count" -eq 4 \
    && "$memory_kib" -ge 7340032 && "$memory_kib" -le 9437184 ]]; then
    hardware_match=true
fi
dedicated_runner_declared=false
if [[ ${AUDIT_PERF_DEDICATED_RUNNER:-0} == 1 ]]; then
    dedicated_runner_declared=true
fi

# Sample host CPU before any build or Aspire resource starts. A dedicated runner declaration
# cannot make an already loaded 4-vCPU machine valid evidence.
cpu_snapshot() {
    awk '/^cpu / { total = 0; for (i = 2; i <= NF; i++) total += $i; printf "%.0f %.0f\n", total, $5; exit }' /proc/stat
}
read -r cpu_total_before cpu_idle_before < <(cpu_snapshot)
sleep 5
read -r cpu_total_after cpu_idle_after < <(cpu_snapshot)
cpu_delta=$((cpu_total_after - cpu_total_before))
cpu_busy_delta=$((cpu_delta - cpu_idle_after + cpu_idle_before))
cpu_busy_percent=$(awk -v busy="$cpu_busy_delta" -v total="$cpu_delta" \
    'BEGIN { if (total <= 0) { print "100.0" } else { printf "%.1f", 100 * busy / total } }')
load_one_minute=$(awk '{ print $1 }' /proc/loadavg)
idle_preflight=false
if awk -v busy="$cpu_busy_percent" \
    'BEGIN { exit !(busy < 10.0) }'; then
    idle_preflight=true
fi
reference_runner=false
if [[ "$hardware_match" == true && "$dedicated_runner_declared" == true \
    && "$idle_preflight" == true && "$smoke" != 1 ]]; then
    reference_runner=true
fi

jq -n \
    --arg command 'scripts/run-tenant-audit-performance.sh' \
    --arg gitRevision "$(git rev-parse HEAD)" \
    --arg os "$(uname -srm)" \
    --arg cpuModel "$(lscpu | sed -n 's/^Model name:[[:space:]]*//p' | head -1)" \
    --argjson cpuCount "$cpu_count" \
    --argjson hostCpuCount "$host_cpu_count" \
    --argjson memoryKiB "$memory_kib" \
    --arg availableMemoryKiB "$(awk '/^MemAvailable:/ { print $2 }' /proc/meminfo)" \
    --arg loadAverage "$(cat /proc/loadavg)" \
    --argjson hardwareMatch "$hardware_match" \
    --argjson dedicatedRunnerDeclared "$dedicated_runner_declared" \
    --argjson idlePreflight "$idle_preflight" \
    --argjson preflightCpuBusyPercent "$cpu_busy_percent" \
    --argjson preflightLoadOneMinute "$load_one_minute" \
    --arg measurementMode "$([[ "$smoke" == 1 ]] && echo smoke || echo contract)" \
    --arg auditMode "$audit_mode" \
    --argjson uiPageSize "$ui_page_size" \
    --arg dotnet "$(dotnet --version)" \
    --arg dapr "$(dapr --version | tr '\n' ' ')" \
    --arg aspire "$(aspire --version)" \
    --arg node "$(node --version)" \
    --arg playwright "$(node -p "require('$performance_dir/node_modules/@playwright/test/package.json').version" 2>/dev/null || echo not-installed)" \
    --argjson referenceRunner "$reference_runner" \
    '{command:$command,gitRevision:$gitRevision,os:$os,cpuModel:$cpuModel,cpuCount:$cpuCount,hostCpuCount:$hostCpuCount,memoryKiB:$memoryKiB,availableMemoryKiB:$availableMemoryKiB,loadAverage:$loadAverage,hardwareMatch:$hardwareMatch,dedicatedRunnerDeclared:$dedicatedRunnerDeclared,idlePreflight:$idlePreflight,preflightCpuBusyPercent:$preflightCpuBusyPercent,preflightLoadOneMinute:$preflightLoadOneMinute,preflightLimits:{cpuBusyPercentBelow:10},measurementMode:$measurementMode,auditMode:$auditMode,uiPageSize:$uiPageSize,dotnet:$dotnet,dapr:$dapr,aspire:$aspire,node:$node,playwright:$playwright,referenceRunner:$referenceRunner,viewports:["1365x768","390x844"],network:"loopback; no artificial throttling"}' \
    > "$result_dir/environment.json"

if [[ "$reference_runner" != true && "$smoke" != 1 ]]; then
    printf '%s\n' 'Reference run requires 4 vCPU/8 GiB, AUDIT_PERF_DEDICATED_RUNNER=1, and an idle preflight (CPU <10%). Set AUDIT_PERF_SMOKE=1 for an invalid smoke run.' \
        | tee "$result_dir/setup-failure.txt" >&2
    exit 1
fi

# Linux .NET development certificates need their OpenSSL trust directory in child processes.
# Without it, the UI's HTTPS call to Tenants API fails even while Aspire reports both healthy.
aspire certs trust --non-interactive > "$result_dir/certificate-trust.txt" 2>&1
if [[ ! -d "$HOME/.aspnet/dev-certs/trust" ]]; then
    printf '%s\n' 'Development certificate trust directory is unavailable.' | tee "$result_dir/setup-failure.txt" >&2
    exit 1
fi
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/etc/ssl/certs"

dotnet restore Hexalith.Tenants.slnx -p:Configuration=Release
dotnet restore "$performance_dir/seed.csproj" -p:Configuration=Release
dotnet restore "$memories_server_project" -p:Configuration=Release
dotnet build Hexalith.Tenants.slnx --configuration Release -m:1 --no-restore
dotnet build "$performance_dir/seed.csproj" --configuration Release -m:1 --no-restore
# The solution intentionally excludes Memories from Release, but AppHost launches it with --no-build.
dotnet build "$memories_server_project" --configuration Release -m:1 --no-restore
dotnet build-server shutdown
npm ci --prefix "$performance_dir" --ignore-scripts
npm run typecheck --prefix "$performance_dir"
(cd "$performance_dir" && npx playwright install chromium)

# Executing the Release AppHost assembly is essential: Aspire's development CLI starts project
# resources with --configuration Debug even after a Release solution build. The assembly's
# configuration metadata makes the AppHost launch every project resource in Release.
DOTNET_LAUNCH_PROFILE=https ASPNETCORE_ENVIRONMENT=Development DOTNET_ENVIRONMENT=Development \
    dotnet "$apphost_dll" > "$apphost_log" 2>&1 &
apphost_pid=$!

apphost_registered=false
for _ in {1..60}; do
    if ! kill -0 "$apphost_pid" 2>/dev/null; then
        printf '%s\n' 'Release AppHost exited before it registered with Aspire.' | tee "$result_dir/setup-failure.txt" >&2
        exit 1
    fi
    if aspire ps --format Json | jq -e --arg path "$apphost_project" \
        'any(.[]; .appHostPath == $path and .status == "running")' >/dev/null; then
        apphost_registered=true
        break
    fi
    sleep 1
done
if [[ "$apphost_registered" != true ]]; then
    printf '%s\n' 'Release AppHost did not register with Aspire within 60 seconds.' | tee "$result_dir/setup-failure.txt" >&2
    exit 1
fi

aspire wait tenants-ui --status healthy --timeout 180 --apphost "$apphost_project" --non-interactive
aspire wait tenants --status healthy --timeout 180 --apphost "$apphost_project" --non-interactive

resources=$(aspire describe --apphost "$apphost_project" --format Json --non-interactive)
api_https_url=$(jq -r '.resources[] | select(.displayName=="tenants-api") | .urls[] | select(.name=="https") | .url' <<< "$resources")
if [[ -z "$api_https_url" ]] || ! curl --fail --silent --show-error --max-time 10 "$api_https_url/health" > /dev/null; then
    printf '%s\n' 'Tenants API HTTPS trust check failed.' | tee "$result_dir/setup-failure.txt" >&2
    exit 1
fi
(while IFS=$'\t' read -r resource container_id; do
    jq -n --arg resource "$resource" \
        --arg image "$(docker inspect "$container_id" --format '{{.Config.Image}}' 2>/dev/null || echo unavailable)" \
        --arg imageId "$(docker inspect "$container_id" --format '{{.Image}}' 2>/dev/null || echo unavailable)" \
        '{resource:$resource,image:$image,imageId:$imageId}'
done < <(jq -r '.resources[] | select(.properties["container.id"] != null) | [.displayName,.properties["container.id"]] | @tsv' <<< "$resources")) \
    | jq -s . > "$result_dir/component-images.json"
ui_url=$(jq -r '.resources[] | select(.displayName=="tenants-ui") | .urls[] | select(.name=="http") | .url' <<< "$resources")
if [[ "$ui_url" != 'http://localhost:62448' ]]; then
    printf '%s\n' 'UI callback URL does not match the Keycloak registration.' | tee "$result_dir/setup-failure.txt" >&2
    exit 1
fi

for resource in tenants-ui tenants-api tenants eventstore; do
    project_pid=$(jq -r --arg name "$resource" '.resources[] | select(.displayName==$name) | .properties["executable.pid"] // empty' <<< "$resources")
    if [[ -z "$project_pid" ]] || [[ $(ps -o args= -p "$project_pid") != *'--configuration Release'* ]]; then
        printf 'Resource %s is not running its Release build.\n' "$resource" | tee "$result_dir/setup-failure.txt" >&2
        exit 1
    fi
done

export DAPR_HTTP_ENDPOINT=$(jq -r '.resources[] | select(.displayName=="tenants") | .environment.DAPR_HTTP_ENDPOINT' <<< "$resources")
export DAPR_GRPC_ENDPOINT=$(jq -r '.resources[] | select(.displayName=="tenants") | .environment.DAPR_GRPC_ENDPOINT' <<< "$resources")

security_container=$(jq -r '.resources[] | select(.displayName=="security") | .properties["container.id"]' <<< "$resources")
if [[ -z "$security_container" || "$security_container" == null ]]; then
    printf '%s\n' 'Keycloak container is absent.' | tee "$result_dir/setup-failure.txt" >&2
    exit 1
fi
security_env=$(docker inspect "$security_container" --format '{{json .Config.Env}}')
export AUDIT_PERF_USERNAME=${AUDIT_PERF_USERNAME:-$(jq -r '.[] | select(startswith("HEXALITH_EVENTSTORE_CLIENT_USERNAME=")) | split("=")[1]' <<< "$security_env")}
export AUDIT_PERF_PASSWORD=${AUDIT_PERF_PASSWORD:-$(jq -r '.[] | select(startswith("HEXALITH_EVENTSTORE_CLIENT_PASSWORD=")) | split("=")[1]' <<< "$security_env")}
unset security_env
if [[ -z "$AUDIT_PERF_USERNAME" || -z "$AUDIT_PERF_PASSWORD" ]]; then
    printf '%s\n' 'The global administrator test account is unavailable.' | tee "$result_dir/setup-failure.txt" >&2
    exit 1
fi

loopback_times=()
for _ in 1 2 3 4 5; do
    loopback_times+=("$(curl -sS -o /dev/null -w '%{time_connect}' "$ui_url/tenants" || echo unavailable)")
done
printf '%s\n' "${loopback_times[@]}" | jq -R -s 'split("\n")[:-1]' > "$result_dir/loopback-tcp-connect-seconds.json"

manifest_path="$result_dir/dataset-manifest.json"
if [[ "$audit_mode" == fallback ]]; then
    dotnet run --project "$performance_dir/seed.csproj" --configuration Release --no-build -- \
        --replay "$manifest_path" | tee "$result_dir/seed-result.txt"
else
    anchor_utc=$(date -u +%Y-%m-%dT%H:%M:00Z)
    tenant_id="audit-perf-$(date -u +%Y%m%d%H%M%S)"
    dotnet run --project "$performance_dir/seed.csproj" --configuration Release --no-build -- \
        "$tenant_id" "$anchor_utc" "$manifest_path" | tee "$result_dir/seed-result.txt"
fi

export AUDIT_PERF_BASE_URL="$ui_url"
export AUDIT_PERF_MANIFEST="$manifest_path"
export AUDIT_PERF_RESULT_DIR="$result_dir"
export AUDIT_PERF_RUN_MODE="$result_dir/run-mode.json"
export AUDIT_PERF_PAGE_SIZE="$ui_page_size"
if [[ "$smoke" == 1 ]]; then
    export AUDIT_PERF_SAMPLES=1 AUDIT_PERF_BATCHES=1 AUDIT_PERF_WARMUPS=1
fi

npm run measure --prefix "$performance_dir"
