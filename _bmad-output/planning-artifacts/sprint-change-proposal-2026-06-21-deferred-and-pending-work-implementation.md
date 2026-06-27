# Sprint Change Proposal — Deferred + Pending Work Implementation

Date: 2026-06-21
Author: Correct Course workflow (Developer agent)
Approver: Administrator (Jérôme Piquot)
Mode: Batch
Scope classification: **Moderate** (cross-submodule owner handoffs executed under approval) + one **Minor** in-repo fix

## 1. Issue Summary

Trigger: Administrator invoked Correct Course with "implement any deferred or pending work."

Investigation established the ground truth before acting:

- The working tree was clean and all five epics plus all prior cross-cutting Correct Course stories were `done`.
- **Every Tenants-owned deferred item from prior code reviews was already implemented and committed** in the 2026-06-21 hardening pass (CSS logical-longhand guard, forced-colors unterminated-block handling, DLQ operator-scope doc note, stale `test-summary.md` line). The `DomainUiFluentConformanceTests` suite was green (44/44).
- The only genuinely-remaining work lived **outside** the Tenants repo boundary (FrontComposer + EventStore owner handoffs) or was an explicit future-epic deferral (Epic 11).
- A separate, **newly discovered** problem surfaced during verification: the already-committed Memories search-index integration left **3 Server.Tests red on `main`** (doc/config-conformance drift), i.e. genuinely pending Tenants-owned work.

Administrator confirmed scope: implement **all four** remaining workstreams, explicitly authorizing crossing the submodule boundary for the owner handoffs.

## 2. Impact Analysis

| Area | Impact |
| ---- | ------ |
| Epic Impact | No epic scope change. Closes the cross-submodule owner handoffs and the Epic 11 deferral recorded against the deferred-work routing index. |
| Story Impact | Four new cross-cutting items recorded in `sprint-status.yaml` (3 handoffs + Epic 11) at `review`; one in-repo drift fix at `done`. |
| Artifact Conflicts | `deferred-work.md` routing index was stale (listed already-closed items as open) → truthed up. `docs/cross-aggregate-timing.md` + `docs/sample-consuming-service-walkthrough.md` were factually wrong about `memories-server` → corrected. |
| Technical Impact | Edits span 3 repos: `Hexalith.Tenants` (main), `Hexalith.FrontComposer` (submodule), `Hexalith.EventStore` (submodule). Tenants consumes both submodules via live `ProjectReference`, so the integrated solution builds against all edits at once; integration is via submodule commit + gitlink pointer update (NOT package bumps). |

## 3. Recommended Approach

Direct adjustment — implement each item in its correct owner repo, keep changes backward-compatible, prove each with tests, and verify the integrated Tenants solution builds and is green. The risky submodule **commit + pointer + push** step is deliberately held for explicit Administrator authorization (per the submodule-pointer-consistency lesson: pushing the parent gitlink while the referenced submodule commit is local-only makes the repo un-cloneable / breaks CI).

## 4. Detailed Change Proposals

### 4.1 FrontComposer owner handoff — `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`

Repo: `Hexalith.FrontComposer` (submodule). Backward-compatible (new params default to null; one strictly-safer behavior change).

- `FcPageHeader.razor` — header root now `role="presentation"` (guarantees exactly one `banner` per page, owned by the shell); `<h1>` is conditional on a non-blank `Heading`.
- `FcPageHeader.razor.cs` — removed the blank-`Heading` throw in favor of fail-safe suppression; `FocusHeadingAsync()` now throws `InvalidOperationException` diagnostically when the heading is not focusable (no `HeadingTabIndex`, or suppressed) instead of silently no-op'ing; landmark/heading contract documented.
- `FrontComposerShell.razor[.cs]` — new `ContentLabel` / `ContentLabelledBy` parameters emit `aria-label` / `aria-labelledby` on the `#fc-main-content` `main` landmark (labelledby wins).
- New `FcContentLabel.razor[.cs]` + `FcContentLabelCoordinator.cs` — a page-level marker so a route can name the shell `main` by its heading id, with no orphaned page-level `aria-labelledby`.
- Tests: `FcPageHeaderTests`, `FrontComposerShellParameterSurfaceTests` (append-only surface snapshot), new `FrontComposerShellContentLandmarkTests`.

Evidence: FrontComposer Shell suite **1962 total, 0 failed**.

### 4.2 EventStore owner handoff (Admin.UI a11y) — `eventstore-2026-06-19-admin-ui-and-query-record-followup`

Repo: `Hexalith.EventStore` (submodule).

- `Index.razor` — clickable StatCards → `role="button"` + `tabindex` + `aria-label` + Enter/Space; `app.css` focus-visible affordance.
- `ActivityChart.razor` — interactive bars container `role="img"` → labelled `role="group"`; real `type="button"` bars; sr-only data table retained.
- `StorageTreemap.razor` — clickable SVG cells get `role="button"`/`tabindex`/`aria-label`/`aria-pressed` + keyboard activation.
- `RelatedTypeList.razor`, `TypeDetailPanel.razor`, `DaprHealthHistory.razor` — keyboard-accessible interactive semantics; `Commands.razor` / `Events.razor` — removed misleading non-functional `cursor:pointer` from non-interactive spans.
- Tests updated/added: `ActivityChartTests`, `StorageTreemapTests`, `IndexPageTests`, conformance carve-out comment.
- The retired actor-routing sub-item was confirmed already stale/resolved (no source matches).

Evidence: affected classes green; Admin.UI.Tests **829/835** — the 6 failures are pre-existing unrelated `Dw5GovernanceAtddTests` (missing DW5 evidence artifact), not introduced here.

### 4.3 EventStore owner handoff (read-model freshness) — `eventstore-2026-06-19-read-model-freshness-metadata`

Repo: `Hexalith.EventStore`, namespace `Hexalith.EventStore.Client.Projections`.

- `IReadModelFreshness` (`DateTimeOffset? ProjectedAt`, `string? ProjectionVersion`), `ReadModelFreshnessState` (`Unknown`/`Current`/`Aging`/`Stale`), `ReadModelFreshnessThresholds`, pure `ReadModelFreshness.Classify/Age`.
- Bridges: `IReadModelStore.GetWithFreshnessAsync<T>()` (store path) and `ToQueryResponseMetadata()` (query-metadata path, fills existing `QueryResponseMetadata.IsStale`/`ProjectionVersion`/`ServedAt`).
- Generic, persisted-timestamp replacement for the Tenants hand-rolled `TenantFreshnessState`. **Follow-up:** Tenants UI adoption is intentionally left open.

Evidence: `ReadModelFreshnessTests` 23/23; Client.Tests **462/462**.

### 4.4 Epic 11 — persisted DataProtection key ring (`Program.cs:77`)

Repos: `Hexalith.EventStore.DomainService` (host-SDK layer) + `Hexalith.Tenants` (host wiring + config + deploy docs).

- New `DaprXmlRepository` (`IXmlRepository` over `DaprClient`, ETag compare-and-swap), `EventStoreDataProtectionOptions`, `AddEventStoreDataProtection(config, appName)`.
- Backend is chosen entirely by `statestore.yaml` (Redis in prod) → **Tenants domain package gains NO infra SDK** (constraint satisfied).
- `src/Hexalith.Tenants/Program.cs` swaps the ephemeral `AddDataProtection()` for `AddEventStoreDataProtection(builder.Configuration, "Hexalith.Tenants")`; `appsettings.json` persists to `statestore` under the application-specific key `hexalith-tenants-dataprotection-keys`; Development stays explicitly ephemeral; `deploy/dapr/README.md` + `statestore.yaml` document the contract.

Evidence: DomainService.Tests **36/36** (incl. cross-replica reload + concurrent-write ETag retry); integrated Tenants Release build clean.

### 4.5 Newly discovered pending work — Memories-integration doc/test drift (in-repo, Minor)

Repo: `Hexalith.Tenants` (main). The committed Memories integration (`4273bbe`/`7ef796f`) added `memories-server` to the local AppHost `pubsub.yaml` scopes and 4 `MemoriesSearchIndexEventPublisher` handlers to the Sample program, but left 3 conformance/doc tests red on `main`. Config/code is the intended source of truth; tests/docs were stale:

- `EventPublicationConfigurationTests` — expected local scopes now include `memories-server`.
- `CrossAggregateTimingDocumentationTests` — split local (`eventstore`+`sample`+`memories-server`) vs production (`eventstore`+`sample`).
- `docs/cross-aggregate-timing.md` — corrected the local component-scope sentences.
- `docs/sample-consuming-service-walkthrough.md` — snippet + handler table + teaching bullet now include the Memories publisher handlers.

Evidence: Tenants Server.Tests **700/700** (was 697/700).

### 4.6 Tenants routing-index truth-up

- `deferred-work.md` — header + new run-summary section; handoff statuses → IMPLEMENTED; the 4 already-closed code-review-deferred items → RESOLVED.
- `sprint-status.yaml` — new 2026-06-21 (session 2) cross-cutting section.

## 5. Implementation Handoff

Scope: **Moderate**. All implementation is complete and verified; what remains is governance:

1. **Administrator decision required — submodule commit + push.** The FrontComposer and EventStore edits are live in their submodule working trees, uncommitted. To land them they must be committed in each submodule, the parent gitlink pointers updated, and **all three repos pushed together** so the parent never references a local-only submodule commit. This step is held for explicit authorization.
2. Tenants-side follow-up (non-blocking): adopt the new `IReadModelFreshness` surface in the Tenants UI to retire the hand-rolled `TenantFreshnessState`.

### Verification evidence (this run)

- Integrated Tenants `.slnx` Release build: **0 warnings, 0 errors** (compiles live FrontComposer + EventStore submodule source).
- Tenants: UI **757/757**, Server **700/700**, Contracts **106/106**, Client **48/48**, Testing **181/181**, Sample **39/39**. (IntegrationTests require live DAPR/Aspire and skip here.)
- FrontComposer Shell **1962/0 failed**; EventStore Client **462/462**, DomainService **36/36**, Admin.UI 829/835 (6 pre-existing unrelated).

## 6. Approval Decision

Scope approved by Administrator on 2026-06-21 (all four workstreams + boundary crossing). The commit/push step in §5.1 was then authorized and executed with submodule-first pointer consistency:

- FrontComposer `main` → `c6c3c39` (pushed)
- EventStore `main` → `5613fed4` (pushed; 3 focused commits: Admin.UI a11y, read-model freshness, Dapr DataProtection)
- Tenants branch `correct-course/2026-06-21-deferred-and-pending-work` → `62a94b0` (pushed; gitlink pointers advanced to the two pushed submodule commits). PR can be opened at the GitHub branch URL.

Remaining non-blocking follow-up: adopt the new `IReadModelFreshness` surface in the Tenants UI to retire the hand-rolled `TenantFreshnessState`.

## 7. Review Findings — Admin.UI a11y (`cc-2026-06-21-eventstore-admin-ui-a11y-remediation`, code review 2026-06-25)

Adversarial review (Blind Hunter + Edge Case Hunter + Acceptance Auditor) of EventStore commit `d6d3ea69` (`feat(admin-ui): keyboard-accessible interactive semantics across Admin.UI`) — the §4.2 handoff only. All findings live in the `Hexalith.EventStore` submodule; any fix is a new follow-up commit, not an amendment. Verified-good: Index StatCards (NS-1), ActivityChart `role=group` (C3/H-ES-3), StorageTreemap button semantics (DV-1/H-ES-4), Commands/Events `cursor:pointer` removal, RelatedTypeList→FluentButton governance compliance, sr-only data tables present.

### Decision-needed (resolved by Administrator 2026-06-25 → patch)

- [x] [Review][Patch] **(from Decision) Dapr heatmap → roving-tabindex ARIA grid** — implement the full ARIA grid keyboard model on the `role="grid"` heatmap: one tab stop into the grid, arrow keys move the active cell (roving tabindex), Enter/Space activates `FilterByCell`; convert the per-row `tabindex="0"`/row-as-activation-target into the roving model so the data cells become keyboard-reachable inside the grid (not as N independent tab stops). `Pages/DaprHealthHistory.razor:129,145,157-161`.
- [x] [Review][Patch] **(from Decision) Single-select `aria-current` for selection state** — replace the treemap cells' `aria-pressed` with `aria-current` (single-select, not toggle) and add `aria-current` to the Dapr component selectors so selected state is exposed to AT consistently across both. `StorageTreemap.razor:67`, `DaprHealthHistory.razor:145,275`.

### Patch

- [x] [Review][Patch] **(Critical) Space activation scrolls the page — no `preventDefault`** on the 6 hand-rolled `role=button`/focusable elements (Index ×2 stat-card divs, StorageTreemap `<g>`, DaprHealthHistory row + transition span). Prevent the Space default **conditionally** (Space-only) — a blanket `@onkeydown:preventDefault="true"` is wrong (it also cancels Tab → focus trap). Prefer native/`FluentButton` where geometry allows; for the SVG `<g>` and grid use Space-gated preventDefault via JS interop or a `:preventDefault`-bound flag. `Index.razor:213,225`, `StorageTreemap.razor:72`, `DaprHealthHistory.razor:145,275`.
- [x] [Review][Patch] **Missing keyboard/aria test coverage** — add tests for TypeDetailPanel (Enter→navigate), DaprHealthHistory (Enter+Space on row + span → SelectComponent), RelatedTypeList (FluentButton renders + `aria-label`), and Space-key (`" "`/`"Spacebar"`) activation for StorageTreemap + Index (currently Enter-only). Space is the headline of the commit message yet untested everywhere.
- [x] [Review][Patch] **Carve-out comment factual fix** — `AdminUiFluentConformanceTests.cs:~39` claims ActivityChart bars "carry aria-label/data-testid"; the bar `<button>` has no `data-testid`. Add `data-testid` to the bar button (makes the governance claim true + aids testing) or correct the comment. `ActivityChart.razor:38`.
- [x] [Review][Patch] **Treemap accessible-name verbosity + redundant `<title>`** — per-cell `aria-label` repeats "Filter by this aggregate type." on every cell, and the inner `<rect><title>@rect.Tooltip</title>` is now redundant with the `<g>` label. Move the instruction to the container group label (mirror ActivityChart); keep per-cell name = "{Label}: {Value} events". `StorageTreemap.razor:72,84`.
- [x] [Review][Patch] **RelatedTypeList interactive target size** — `font-size: 0.8em` + `ButtonSize.Small` risks a target below the ~24×24px WCAG 2.5.8 minimum (and changes the visual from the prior Ghost badge). Verify rendered size ≥24px; drop the `font-size` override or add padding. `RelatedTypeList.razor`.
- [x] [Review][Patch] **DRY the activation-key predicate** — three byte-identical `e.Key is "Enter" or " " or "Spacebar"` handlers; extract a shared `KeyboardEventArgs.IsActivationKey()` helper so the preventDefault fix + key set stay consistent.

### Deferred

- [x] [Review][Defer] **SVG `<g tabindex=0>` focusability not guaranteed cross-browser** `StorageTreemap.razor:72` — deferred, validate against target browser matrix (see deferred-work.md 2026-06-25).

### Applied (2026-06-25)

All 8 patches (6 + 2 resolved decisions) were applied to the `Hexalith.EventStore` submodule and verified:
- Build: Admin.UI Release `-warnaserror` **0 warnings / 0 errors**.
- Tests: **Admin.UI.Tests 839/839** (prior 829 baseline + 10 new keyboard/aria tests — roving-grid single-tab-stop + arrow nav + Enter-cell + Space-rowheader, treemap aria-current + Space, StatCard Space, TypeDetailPanel Enter/not-Space, RelatedTypeList FluentButton, ActivityChart data-testid). 0 failed / 0 skipped.
- New shared helper `KeyboardActivation.IsActivationKey`; conditional Space/arrow `preventDefault` + roving focus via one delegated `interop.js` keydown listener (avoids the focus-trapping blanket `@onkeydown:preventDefault`).
- **Status: UNCOMMITTED in the EventStore submodule working tree** — landing requires a follow-up `feat`/`fix(admin-ui)` commit + push (cross-repo; not an amendment of `d6d3ea69`), then a Tenants gitlink bump for reachability.
- Known follow-up folded into the deferred item: the heatmap remains a flat CSS grid (no `role="row"` wrappers around cells) — a pre-existing structural-ARIA limitation independent of the keyboard model; revisit with the browser/AT validation.

### Dismissed (considered, not actionable)

- `"Spacebar"` legacy `KeyboardEvent.key` alias — dead on modern browsers, harmless.
- `NavigateTo` one-line wrapper in Index.razor — a reasonable shared onclick+keydown seam.
- `role="group"` vs "toolbar" comment wording — `group` is a defensible choice; the sr-only data table it references was verified to exist.
- "role=link vs role=button inconsistency" for the two type-name spans — **correctly** differentiated by action: TypeDetailPanel *navigates* (`role=link`, Enter-only is ARIA-correct), DaprHealthHistory *selects in-page* (`role=button`, Enter+Space). The commit-message "Enter/Space" blanket wording is immaterial.

## 8. Review Findings — Epic 11 persisted DataProtection key ring (code review 2026-06-25)

- [x] [Review][Decision][Dismissed] DataProtection key material is persisted without an at-rest encryptor — Decision 2026-06-25: accept the DAPR state store as the trusted boundary for Epic 11; no additional key encryptor is required in this story.
- [x] [Review][Patch] Namespace or isolate the Tenants key-ring state [references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDataProtectionOptions.cs:30] — patched: the shared option now defaults blank `StateKey` to an application-specific key, Tenants uses `hexalith-tenants-dataprotection-keys`, and deploy docs describe the shared-state-store trust boundary.
- [x] [Review][Patch] Add bounded timeout handling for synchronous repository calls [references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DaprXmlRepository.cs:56] — patched: `DaprXmlRepository` now uses a configurable positive `OperationTimeout` and bounded cancellation for synchronous key-ring load/store calls.
- [x] [Review][Patch] Add backoff or stronger retry policy for concurrent key writes [references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DaprXmlRepository.cs:85] — patched: concurrent-write retries now use additional attempts with exponential backoff instead of five immediate retries.
- [x] [Review][Patch] Prove the actual Data Protection contract end-to-end [references/Hexalith.EventStore/tests/Hexalith.EventStore.DomainService.Tests/EventStoreDataProtectionTests.cs:31] — patched: tests now protect with one `IDataProtectionProvider` and unprotect with a second provider over the same persisted key ring.
- [x] [Review][Patch] Fix the non-persistent mode contract [references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDataProtectionServiceCollectionExtensions.cs:35] — patched: `PersistToStateStore=false` now explicitly uses `UseEphemeralDataProtectionProvider()`, with a test proving a fresh provider cannot unprotect the prior provider's payload.
- [x] [Review][Patch] Document the ETag-capable state-store requirement [references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DaprXmlRepository.cs:94] — patched: the registration remarks, deployment README, and `statestore.yaml` now state that the selected DAPR state store must support ETag / first-write concurrency.
- [x] [Review][Patch] Correct stale landing-state prose in the routing docs [_bmad-output/implementation-artifacts/deferred-work.md:10] — already corrected in the current workspace: the routing index records the pushed FrontComposer/EventStore/Tenants commits and no longer says the submodule edits are pending commit/pointer update/push.

## 9. Review Findings — EventStore read-model freshness metadata (code review 2026-06-27)

- [ ] [Review][Patch] Validate `ReadModelFreshnessThresholds` even when callers bypass `Create` [references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/ReadModelFreshnessThresholds.cs:21] — the public record-struct primary constructor allows `new ReadModelFreshnessThresholds(...)` with negative or inverted values despite the documented validation contract. `ReadModelFreshness.Classify` accepts the struct without revalidating it, so malformed thresholds can silently produce incorrect freshness states. Add runtime validation on the public use path or make the thresholds type self-validating, and add a regression test for direct construction.
