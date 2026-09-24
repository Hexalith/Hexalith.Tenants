# Story 5.1 audit performance contract

**Status:** Approved performance acceptance contract, satisfied by the corrected v4 dedicated run on 2026-09-24. All 126 measured percentile groups and ten functional gates passed with the 50-row UI; the 25-row fallback was not triggered. See the [evidence record](story-5-1-performance-evidence.md). The approved requirements below are unchanged.

**Approved revision:** 1.

**Approval:** Jérôme Piquot, Owner (role stated by approver), 2026-09-23. The approver explicitly approved revision 1 in the build conversation.

## Decision and scope

The dataset, reference environment, measurement procedure, budgets, and fallback below form one approved contract. The existing Story 5.1 functional verification remains valid but does not substitute for a performance run that measures every approved endpoint.

## Representative data and workload

- Seed one isolated tenant audit projection with **500 entries**: 100 `UserAddedToTenant`, 100 `UserRoleChanged`, and 50 `UserRemovedFromTenant` (`Access`); plus one `TenantCreated`, 99 `TenantUpdated`, 100 `TenantConfigurationSet`, and 50 `TenantConfigurationRemoved` (`Administrative`). Use synthetic safe 16–32-character actors, targets, keys, and distinct event references with supported narrative fields; include no real PII or secrets. Spread event timestamps evenly over the 30 days before the run's recorded UTC anchor, with some equal timestamps to exercise the event-reference ordering tie break. Set projection freshness to the run time and wait for an authoritative read showing all 500 rows before timing.
- Use the current flat `AuditDataGrid`, including its receipt and correction availability cells. The browser UI requests **50 rows per page**; the test must not replace it with a simplified grid or request all 500 in one page. Record the response count, page size, cursor presence, and displayed row count for every sample.
- Run three separate filter cases with equal sample counts: unfiltered (500 matching entries), `Access` category (250), and a 7-day UTC range plus `Administrative` category (approximately 58). Report each case separately. Exercise Apply/Reset filters and Next/Previous paging where a next page exists. A missing expected page or incorrect row order is a functional failure, regardless of timing.

## Reference environment and network

- Authoritative tier: an **authenticated, full-stack Chromium browser run** against a Release-built Tenants UI, Tenants API, DAPR, Redis, and Keycloak started by the repository's Aspire AppHost. Use a dedicated Linux runner with 4 vCPU and 8 GiB RAM, no concurrent load, and pinned .NET SDK and Chromium/Playwright versions. Record the OS, CPU model, available memory, Git revision, component versions, and viewport in the evidence.
- Use a stable local callback URL registered in Keycloak for the test account. The browser and services run on the same runner over loopback with no artificial network or CPU throttle; record a pre-run loopback RTT and any resource contention. This is a **controlled lab budget**, not a claim about production networks or physical phones. Measure both 1365×768 desktop and 390×844 phone layouts on that same hardware; keep their results separate.
- The test account must be authorized as a global administrator and the audit route must render a populated, current grid. Unauthorized, empty, skipped, or self-skipped runs are invalid evidence. No credentials or token values may enter the result artifact.

## Approved budgets

| Observable | Start and end | Budget in each viewport and filter case |
| --- | --- | --- |
| Initial audit-ready render | Start at navigation to `/tenants/{tenantId}/audit` in a new browser context with an existing authenticated session; stop after the first populated, current 50-row unfiltered grid has painted. | p75 ≤ **2.5 s** and p95 ≤ **4.0 s** |
| Filter or page result | Start at click, tap, or Enter on Apply, Reset, Next, or Previous; stop after the expected rows and paging controls have painted and the loading state has cleared. | p75 ≤ **1.5 s** and p95 ≤ **3.0 s** |
| Visible interaction feedback | Start at the same action; stop at the first painted loading or state change. | p75 ≤ **200 ms** and p95 ≤ **500 ms** |

These are approved audit-specific budgets. The p75 initial-render and feedback anchors were informed by the web's published 2.5-second LCP and 200-millisecond INP guidance, but **audit-ready render and result completion are different observables** and are measured directly here. The p95 limits and result-completion limits are this contract's Product/Operations decisions, not inherited standards. Sources: [Web Vitals threshold methodology](https://web.dev/articles/defining-core-web-vitals-thresholds) and [INP definition](https://web.dev/articles/inp).

## Repeatability and acceptance

1. Use a deterministic seed and record its generator revision, random seed, UTC anchor, entry mix, and a content hash of the support-safe dataset manifest. Do not store raw credentials or unsafe audit values.
2. Warm the AppHost, projection, and browser with five untimed samples per viewport. Then take **three independent batches of 40 samples** for each measured action in each viewport, restarting the browser between batches. Initial navigation uses the unfiltered case and a fresh authenticated browser context per sample; filter and paging actions run on a loaded page with 40 samples per applicable case. Use a monotonic browser clock from action start to the expected `tenants-audit-ready`/`tenants-audit-grid`/row milestone followed by a painted frame; use the loading state or first changed frame for visible feedback. Do not use network-idle as the endpoint because the InteractiveServer connection remains open. Record every raw duration, including failures and timeouts, without discarding slow valid samples.
3. For each batch, viewport, applicable filter case, action, and observable, sort durations and calculate nearest-rank p75 and p95. **All three batches must meet both percentiles**. Record the exact command, build revision, environment, dataset manifest, timing script version, raw samples, percentile calculation, and pass/fail result. A setup failure invalidates the batch and must be recorded before a complete rerun; it is never counted as a pass.
4. Keep keyboard, screen-reader semantics, forced-colors, EN/FR, mobile read-only correction gating, stable server order, protected cursor behavior, and support-safe output checks as separate required functional gates. A fast but incorrect or inaccessible grid fails the story. Run the authoritative performance tier for the Story 5.1 release candidate and after changes to the audit query, BFF mapping, grid, or paging. A self-skipped or unavailable tier leaves performance acceptance open.

## Fallback trigger and sequence

Any valid batch missing either percentile budget triggers fallback work. First reduce the UI audit page size from 50 to **25** while preserving opaque server cursors, ordering, safety-critical fields, and accessible paging; rerun all measurements and functional gates against the same 500-entry dataset. If the 25-row grid still misses, implement bounded rendering/virtualization compatible with Fluent DataGrid and rerun the full contract. Diagnose backend or network bottlenecks separately; a fallback does not turn a failing measurement into a pass. If neither fallback satisfies the contract, Story 5.1 remains open for an explicit Product/Operations revision rather than a relaxed claim.

## Approval boundary

The approval recorded above makes revision 1 authoritative and authorizes implementation of the measurement lane and any triggered fallback. It does not itself prove that the UI meets the contract. Only the complete, passing measurements and functional gates defined here can close Story 5.1.
