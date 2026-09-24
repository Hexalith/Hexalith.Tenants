# Story 5.1 audit performance evidence

**Status:** Acceptance pending a repeat run. The authenticated Release full-stack run completed on a dedicated 4 vCPU/8 GiB Linux KVM guest on 2026-09-24, but review found that its v3 result clock stopped at row paint before paging controls were usable. Its passing percentiles cannot establish the approved result-completion budget or rule out the 25-row fallback.

## Review correction, 2026-09-24

The approved contract ends a filter or page result after expected rows **and paging controls** have painted and loading has cleared. The v3 script recorded the row-paint timestamp while `TenantAuditPage` could still await supplementary reads with paging disabled. Its `requestAnimationFrame` timestamp was also taken before that frame painted. The v4 script now waits for the expected usable pager state and a later animation frame. All recorded v3 samples below remain historical data; they are not current acceptance evidence. Run the complete 3×40 contract again on the approved dedicated runner, then evaluate fallback only from that valid result.

The archived source patch also labels EventStore, FrontComposer, and Memories gitlinks `-dirty` without preserving their nested status or diffs. The recorded gitlink commits identify the checked-out revisions, but the patch alone cannot prove whether those local changes affected source. The repeat run should record each root-declared submodule's source status or use clean submodule working trees.

## Contract and exact run

[Approved revision 1](story-5-1-performance-decision.md) requires five untimed warmups per viewport, then three independent Chromium batches of 40 samples per applicable action at 1365×768 and 390×844. Every batch must meet nearest-rank p75 and p95 budgets. The complete run used the unchanged 50-row baseline and restarted Chromium between batches.

From `/home/benchmark/tenants` in the dedicated guest:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"
AUDIT_PERF_DEDICATED_RUNNER=1 \
AUDIT_PERF_RESULT_DIR="$HOME/tenants/_bmad-output/implementation-artifacts/story-5-1-performance-vm-authoritative-2026-09-24" \
scripts/run-tenant-audit-performance.sh
```

The command exited **0**. Playwright reported **1 passed (33.6m)**. The runner restored and built the Release solution, seed, and Memories server with zero warnings or errors; typechecking passed. It started the Release Aspire AppHost, verified healthy Tenants UI and service, checked the Tenants API HTTPS connection, seeded the read model, measured the authenticated browser, and stopped the AppHost. `aspire ps --format Json` returned `[]` afterward.

The recorded Git HEAD is `2729deefdde44dd89ded1e410abb9ef767b0fb9d`. [The exact working-tree source diff](story-5-1-performance-vm-authoritative-2026-09-24/source-diff.patch.gz) used for this run is compressed for storage; its uncompressed SHA-256 is `0eacf920dc4fb13b96ff867df1e40c223e8de095a2916ac9a2003d11fcd5d9aa`. [The checksum file](story-5-1-performance-vm-authoritative-2026-09-24/source-diff.patch.gz.sha256) verifies the compressed artifact. The browser script identifies itself as `audit-performance-v3` in the [summary](story-5-1-performance-vm-authoritative-2026-09-24/summary.json).

## Runner and dataset

The guest was a reserved Ubuntu 26.04 KVM VM with 4 assigned vCPUs and 8 GiB RAM, running Linux `7.0.0-31-generic` on an AMD Ryzen 9 9950X3D host CPU. Both `nproc` and `/proc/stat` reported four CPUs; guest `MemTotal` was 8,126,492 KiB and `MemAvailable` was 6,901,704 KiB at preflight. The five-second CPU sample was 2.9% busy, below the runner's 10% idle gate. One-minute load was 4.01 and is recorded for review; the required idle Dapr services were already running. No unrelated workload ran in the guest. [Environment](story-5-1-performance-vm-authoritative-2026-09-24/environment.json) records `referenceRunner=true`, viewport sizes, versions, and preflight data.

The pinned tools were .NET SDK 10.0.401, Dapr CLI 1.18.2/runtime 1.18.4, Aspire 13.5.3, Node 22.22.1, Playwright 1.63.0, and Chromium 153.0.8010.12. [Aspire container images and immutable IDs](story-5-1-performance-vm-authoritative-2026-09-24/component-images.json) include Keycloak 26.6 and Redis Stack; [Dapr runtime image IDs](story-5-1-performance-vm-authoritative-2026-09-24/dapr-runtime-images.json) include Redis 6 and Dapr 1.18.4. The UI used Keycloak's registered `http://localhost:62448` callback. Browser and services ran over loopback without artificial throttling. The five [loopback TCP connect samples](story-5-1-performance-vm-authoritative-2026-09-24/loopback-tcp-connect-seconds.json) ranged from 0.000299 to 0.000655 seconds.

The [support-safe manifest](story-5-1-performance-vm-authoritative-2026-09-24/dataset-manifest.json) records generator `tenant-audit-seed-v2`, random seed 5101, UTC anchor `2026-09-24T07:20:00Z`, isolated tenant `audit-perf-20260924072039`, and SHA-256 `ff43a37a5771f86d501aaf36f61e2cc2d9c3cac37a804edcda5999c6338d666d`. The approved 500 entries comprise 250 Access and 250 Administrative events, including equal-timestamp ordering ties. The [seed result](story-5-1-performance-vm-authoritative-2026-09-24/seed-result.txt) confirms a reread of all 500 persisted rows and a current projection timestamp before timing.

## Historical v3 results

Each value below is the **largest batch percentile** across applicable actions and both viewports. The individual desktop and phone cases remain separate in the raw files and summary; these maxima are a compact acceptance overview.

| Observable | Largest p75 | p75 budget | Largest p95 | p95 budget |
| --- | ---: | ---: | ---: | ---: |
| Initial audit-ready render | 1,015.7 ms | 2,500 ms | 1,245.4 ms | 4,000 ms |
| Filter or page result | 307.3 ms | 1,500 ms | 389.0 ms | 3,000 ms |
| Visible interaction feedback | 50.9 ms | 200 ms | 85.7 ms | 500 ms |

All six raw batches contain 840 observations, for **5,040 observations and 126 complete percentile groups**. Every group has 40 failure-free samples and passes the v3 script's budgets. The ten browser checks passed: grid semantics and critical fields, keyboard paging and exact server order, forced colors and reduced motion, phone read-only correction safety and support-safe output, and French localization at each viewport. The summary has `setupFailures=[]`, `failedGroups=[]`, and no failed functional check. An independent read of the raw JSON recomputed all 126 nearest-rank percentiles, checked each recorded response count, page size, next-cursor flag, displayed row count, dataset hash, and check, and passed. These checks do not repair the v3 timing endpoint. No credentials or session state were written to the artifacts.

Raw batches: [desktop 1](story-5-1-performance-vm-authoritative-2026-09-24/raw-desktop-batch-1.json), [desktop 2](story-5-1-performance-vm-authoritative-2026-09-24/raw-desktop-batch-2.json), [desktop 3](story-5-1-performance-vm-authoritative-2026-09-24/raw-desktop-batch-3.json); [phone 1](story-5-1-performance-vm-authoritative-2026-09-24/raw-phone-batch-1.json), [phone 2](story-5-1-performance-vm-authoritative-2026-09-24/raw-phone-batch-2.json), [phone 3](story-5-1-performance-vm-authoritative-2026-09-24/raw-phone-batch-3.json).

## Setup attempts and fallback boundary

Earlier guest attempts were invalid setup checks and were never used as acceptance or fallback triggers. The first stopped before browser timing because the fresh guest lacked Docker buildx and the Memories server Release output, which the solution intentionally excludes. The next stopped at the idle preflight (22.2% CPU busy) while a manual build's compiler server was still active; the runner now shuts build servers down. The following full-stack attempt seeded 500 entries but its browser warmups found the audit grid unavailable: the guest did not trust the local .NET HTTPS certificate used by the UI-to-API call. The runner now provisions Linux development-certificate trust, exports its OpenSSL trust directory, and checks the API HTTPS health endpoint. Short diagnostic smokes then exposed a second DOM read that could race a later projection refresh after a correctly painted grid; the timing script now uses its exact-row painted-frame check as the completion point. A complete fresh 3×40 run was performed after these fixes. [The certificate trust record](story-5-1-performance-vm-authoritative-2026-09-24/certificate-trust.txt) notes partial Linux browser-store trust; the runner's OpenSSL HTTPS check and authenticated Chromium run both succeeded.

The earlier [shared-host smoke](story-5-1-performance-smoke-2026-09-23/summary.json) used one sample in one batch per viewport on a 24-CPU WSL2 host. Its 42 singleton groups and ten browser gates were useful wiring checks, but it was never authoritative. The guarded fallback mode was tested with `npm run test:mode --prefix tests/performance/tenant-audit` (4/4 passing): a complete synthetic valid miss selects 25 UI rows and copies the original manifest byte for byte; a passing, shared-runner, or incomplete baseline is rejected. The historical v3 baseline recorded no percentile miss, but its endpoint is incomplete, so it cannot decide whether the approved fallback is triggered.

Focused UI audit-page tests passed 89/89 in the original run; Release solution and seed builds passed with zero warnings or errors; TypeScript typecheck, shell syntax, and `git diff --cached --check` passed. Review corrections were then checked separately, including 91/91 focused UI tests. A new dedicated browser run is required for performance acceptance of the current 50-row candidate.
