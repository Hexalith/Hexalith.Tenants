# Test Automation Summary

## Generated Tests

### API Tests
- [x] Not applicable for revised Story 1.8. Support-safe copy remains browser Clipboard API interop over explicitly approved values already rendered by server-authorized UI projections; no backend endpoint, query contract, direct browser API call, token storage, telemetry, or command path was added.
- [x] Story 2.1 command gateway tests cover EventStore `SubmitCommandRequest` mapping, ULID-shaped message id idempotency key creation through the registered factory, literal tenant id preservation, returned correlation-id capture, correlation-id status lookup, validation blocking before submit, safe gateway exception mapping, `TenantAlreadyExistsRejection`, insufficient permissions, missing/malformed status lookup, publish failure, and timeout.
- [x] Story 2.2 add-member gateway tests cover `AddUserToTenant` `SubmitCommandRequest` shape (`Tenant=system`, `Domain=tenants`, `AggregateId=tenantId`, `CommandType=AddUserToTenant`), ULID-shaped message id, literal case-sensitive tenant/user id preservation, role serialized by name, returned correlation-id capture, validation blocking on empty user id and `TenantRole.Unknown` before submit, and safe rejection mapping for `UserAlreadyInTenantRejection`, `RoleEscalationRejection`, `InsufficientPermissionsRejection`, `TenantDisabledRejection`, and `TenantNotFoundRejection` on submission and status-lookup paths, including `InsufficientPermissionsRejection` status lookup, without leaking raw payloads, tokens, or correlation ids.
- [x] Story 2.3 change-role gateway tests cover `ChangeUserRole` `SubmitCommandRequest` shape (`Tenant=system`, `Domain=tenants`, `AggregateId=tenantId`, `CommandType=ChangeUserRole`), ULID-shaped message id, literal case-sensitive tenant/user id preservation, `NewRole` serialized by name, returned correlation-id capture, validation blocking on empty user id and `TenantRole.Unknown` before submit, and safe rejection/status mapping for `RoleEscalationRejection`, `UserNotInTenantRejection`, `InsufficientPermissionsRejection`, `TenantDisabledRejection`, and `TenantNotFoundRejection` without leaking raw payloads, tokens, or correlation ids.
- [x] Story 2.4 remove-member gateway tests cover `RemoveUserFromTenant` `SubmitCommandRequest` shape (`Tenant=system`, `Domain=tenants`, `AggregateId=tenantId`, `CommandType=RemoveUserFromTenant`), ULID-shaped message id, literal case-sensitive tenant/user id preservation, returned correlation-id capture, validation blocking before submit, safe remove-specific rejection mapping for `UserNotInTenantRejection`, `InsufficientPermissionsRejection`, `TenantDisabledRejection`, and `TenantNotFoundRejection`, and shared status lookup exposing `UserNotInTenant` for remove reconciliation without success copy or raw payload/token/correlation leakage.
- [x] Story 2.5 update-metadata gateway tests cover `UpdateTenant` `SubmitCommandRequest` shape (`Tenant=system`, `Domain=tenants`, `AggregateId=tenantId`, `CommandType=UpdateTenant`), ULID-shaped message id, literal case-sensitive tenant id preservation, name/description payload serialization, returned correlation-id capture, validation blocking before submit, safe update-specific rejection mapping for `InsufficientPermissionsRejection`, `TenantDisabledRejection`, and `TenantNotFoundRejection`, and no raw payload/token/correlation leakage.
- [x] Not applicable for Story 3.1. Lifecycle enable/disable command submission intentionally remains out of scope; tests assert no `EnableTenantAsync`, `DisableTenantAsync`, `SubmitCommand`, form, or submit path is reachable from the lifecycle availability component.

### UI Component and Workflow Tests
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/SupportSafeCopyButtonTests.cs` - 43 component tests for explicit approval/known-kind/accessible-name fail-closed behavior, exact Unicode/reserved/significant-whitespace/long literals, no identifier deny-list or invented length contract, zero markup/interop for unapproved input, input-version race suppression, overlapping-activation exclusion, repeated-outcome announcement publication, cancellation and Clipboard API outcomes, polite atomic feedback, localized safe recovery, and no value/exception disclosure.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs` - tenant list workflow coverage now activates the outer copy control and asserts the exact authorized projection literal while preserving stable selectors, filtering/sorting/paging, responsive columns, and forced-colors behavior.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs` - tenant detail/member workflow coverage activates both outer controls and asserts exact literals; configuration preserves legacy safe display/redaction while asserting no copy affordance until Story 1.6's positive safe model exists; source/CSS checks cover literal-preserving wrapping, storage, logging, telemetry, serialization, backend, fallback, focus, forced colors, and motion-independent behavior.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/TruthStateBadgeTests.cs` - transient freshness badge coverage proving `Refreshing` renders from the badge flag while durable snapshots keep the shared `ReadModelFreshnessState` values.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/MyTenantsSurfaceTests.cs` - My Tenants workflow coverage activates `tenants-my-*` copy and asserts the exact authorized tenant literal while preserving paging/freshness, no mutation controls, and no browser backend/token storage.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/UserMembershipLookupSurfaceTests.cs` - user lookup workflow coverage activates `tenants-user-*` copy and proves `tenant/%2F?x=é` is copied literally rather than decoded or replaced by route/lookup context, with no hidden membership/browser token leakage.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs` and `GlobalAdministratorsPageTests.cs` - outer-page compatibility coverage activates safe audit/global-administrator controls with exact clipboard arguments and proves an unsafe raw audit event reference has no grid copy control.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/CreateTenantFlowTests.cs` - create tenant command-flow coverage for stable selectors, fail-closed unavailable state, required-field validation, literal tenant id submission, no projection-free confirmation, projection-confirmed lifecycle, safe rejection text, status rejection with projection evidence, degraded publish-failed state, unable-to-verify recovery, audit handoff, and live-region politeness.
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantCreateCommandSnapshotTests.cs` - lifecycle reducer coverage for accepted/projection-pending/confirmed non-collapse, SignalR nudge-only behavior, audit pending/unavailable handoff, assertive non-success states, and the success-prohibited invariant for rejected/degraded/unable-to-verify states.
- [x] Story 2.1 review regression tests: accepted-vs-projection-pending non-collapse on evidence-free re-query (AC4), focusable lifecycle region for fail-closed focus recovery (AC6), and `aria-describedby` referencing only a rendered validation element (a11y).
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/AddTenantMemberFlowTests.cs` - add-member flow coverage for stable selectors, assignable-roles-only (no `Unknown`, no invite/email/Users navigation), literal user id submission, no projection-free confirmation, projection-confirmed member-role evidence, `UserAlreadyInTenantRejection` staying rejected without success copy, required user id and explicit-role validation before gateway submission, fail-closed stale/unknown-freshness, unauthorized-surface, tenant-lifecycle, and command-surface reasons, duplicate-submit in-flight blocking, focusable lifecycle region, and live-region politeness with no internal correlation-id disclosure.
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantAddMemberCommandSnapshotTests.cs` - add-member lifecycle reducer coverage for completed-status-requires-member-evidence confirmation, accepted-vs-projection-pending non-collapse on evidence-free re-query, SignalR nudge that cannot confirm member or audit success, and terminal non-success states (rejected/publish-failed/timed-out) that projection evidence cannot convert to confirmed.
- [x] Story 2.2 member-table preservation: `TenantDetailSurfaceTests.cs` extended so composing the add-member flow keeps member table headers, row relationships, change-role/remove-member unavailable action slots, stale/degraded messaging, add-member CSS responsive/forced-colors/focus hooks, and `Tenants.AddMember.*` EN/FR resource parity intact.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/ChangeTenantMemberRoleFlowTests.cs` - change-role flow coverage for stable selectors, assignable roles excluding `Unknown`, current-role `AlreadyApplied`, literal user id submission, projection-confirmed role evidence, projection-free pending state, manual refresh through status lookup and projection re-query, fail-closed unavailable reasons, inline label/ARIA associations, spoofed invalid-role validation before gateway submission, owner-count risk warning without hard-blocking, duplicate-submit blocking, close callback focus recovery, terminal lifecycle non-collapse, safe rejection copy, live-region politeness, and no internal correlation/payload/token disclosure.
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantChangeRoleCommandSnapshotTests.cs` - change-role lifecycle reducer coverage for same-role and zero-event backend NoOp `AlreadyApplied`, accepted-vs-projection-pending non-collapse, SignalR nudge that cannot confirm role or audit success, projection evidence requirements, missing target member unable-to-verify state, and terminal non-success states that projection evidence cannot convert to confirmed.
- [x] Story 2.3 member-table preservation: `TenantDetailSurfaceTests.cs` extended so composing the change-role flow keeps member table caption, headers, row relationships, add-member flow, remove-member unavailable slots, copy buttons, stale/degraded messaging, responsive/forced-colors/focus hooks, and `Tenants.ChangeRole.*` EN/FR resource parity intact.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs` - remove-member destructive flow coverage for stable selectors, complete 10-item consequence preview, explicit confirmation, last-owner friction, global-admin risk separation, projection-confirmed absence before confirmation, no optimistic removal, safe already-applied reconciliation, duplicate prevention, blocked gates, cancel/Escape no-op, live regions, audit handoff, and support-safe copy.
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantRemoveMemberCommandSnapshotTests.cs` - remove-member reducer coverage for preview completeness, last-confirmed projection preservation, projection-confirmed absence, missing-member rejection reconciliation, SignalR nudge-only behavior, duplicate-prevented state, terminal non-success non-collapse, and audit pending/delayed/unavailable/missing-support states.
- [x] Story 2.4 member-table preservation: `TenantDetailSurfaceTests.cs` updated so composing the remove-member flow keeps member table caption, headers, row relationships, add-member flow, change-role flow, copy controls, stale/degraded messaging, responsive/forced-colors/focus hooks, and full `Tenants.RemoveMember.*` EN/FR resource parity intact.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/EditTenantMetadataFlowTests.cs` - metadata-edit flow coverage for stable selectors, confirmed metadata display, contributor/admin permission reflection via component parameter, stale/unknown/disabled/command-surface fail-closed gates, whole-field validation, command submission, gateway submission failure, clear-to-null description behavior, projection-confirmed metadata update, no optimistic overwrite, same-metadata submission without already-applied suppression, terminal lifecycle states, support-safe copy, live-region politeness, cancel/Escape close behavior, forced-colors/focus CSS hooks, audit handoff, and in-flight parent locking callback.
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantUpdateMetadataCommandSnapshotTests.cs` - update-metadata reducer coverage for accepted/projection-pending/confirmed non-collapse, last-confirmed metadata preservation, projection-confirmed name/description evidence, SignalR nudge-only behavior, zero-event status not becoming already-applied, terminal non-success states, and audit pending/delayed/unavailable/missing-support states.
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantLifecycleAvailabilityTests.cs` - Story 3.1 availability model coverage for active/disabled/unknown status, disabled-enable projection truth under governance block, same-state `TenantLifecycleStateAlreadySet`, stale/unknown freshness, aging freshness not bypassing unresolved governance, unsafe detail surface kinds, missing command support, unresolved governance, indeterminate authorization, and narrow/mobile fail-closed behavior.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleActionAvailabilityTests.cs` - Story 3.1 component coverage for stable selectors, disabled enable/disable slots, inline keyboard-reachable unavailable reasons, governance blocked copy, disabled projection truth with no optimistic active transition, same-state safe domain outcome copy, mobile/narrow safety copy, assertive/polite live-region behavior, no success/accepted/confirmed lifecycle text, and no command submission path.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs` - Story 3.1 tenant-detail composition coverage for lifecycle action slot placement beside current status/freshness, responsive/forced-colors/focus CSS hooks, and full `Tenants.Lifecycle.*` EN/FR resource parity.
- [x] `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs` - Story 3.2 lifecycle gateway coverage for enable/disable command submission shape, literal tenant id preservation, `EnableTenant`/`DisableTenant` command names, generated message id, correlation id capture, and safe rejection mapping for `TenantLifecycleStateAlreadySet`, `TenantDisabled`, `TenantNotFound`, and `InsufficientPermissions`.
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantLifecycleCommandSnapshotTests.cs` - Story 3.2 lifecycle reducer coverage for previewed/request-sent/accepted/projection-pending/confirmed states, enable and disable projection evidence requirements, SignalR nudge-only behavior, terminal rejection non-success behavior, blocked/duplicate non-success states, status/audit/live-region distinction across accepted, pending, rejected, failed, degraded, unable-to-verify, audit pending/delayed/unavailable/missing-support, terminal non-success states that cannot become confirmed, and last-confirmed lifecycle preservation.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleActionAvailabilityTests.cs` and `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs` - Story 3.2 component/composition coverage for server-side BFF global-admin authorization reflection, high-impact preview launch, complete 10-item consequence preview, typed tenant-id confirmation, validation blocking before submit, cancel/Escape no-commit behavior, command-surface fail-closed action blocking, one-at-a-time activity lock retention until projection confirmation, focus-loop sentinels, safe rejection display, projection-confirmed lifecycle completion, support-safe markup, stable lifecycle selectors, and no optimistic lifecycle transition.

## Coverage
- Revised Story 1.8 acceptance criteria: all five have current automated evidence or an exact external blocker in `story-1-8-support-safe-identifier-copy-and-read-experience-evidence-2026-07-21.md`. Authenticated browser focus/Clipboard evidence, human NVDA evidence, and configuration copy pending Story 1.6 are explicitly blocked rather than inferred.
- API endpoints: 0 new endpoints; existing read gateways remain covered by the surface tests and no new API adapter was required.
- UI surfaces covered: activated tenant list, tenant detail identity, My Tenants, user membership lookup, member table, audit, and global-administrator controls; read-only configuration preserves safe baseline display while proving fail-closed copy omission pending the positive safe model.
- Critical error cases covered: non-approval, default/unknown/undefined future kind, missing accessible name, empty/whitespace input, identity change across awaited interop, overlapping activation, repeated identical outcome, cancellation, permission denial, insecure browser context, missing Clipboard API, generic JS failure, disconnected Blazor Server circuit, unsafe raw audit reference, alternate/route-encoded literals, and no false copied outcome.
- Story 2.1 acceptance criteria: 8/8 covered across command gateway tests, create-flow component tests, lifecycle state tests, composition/resource tests, and workspace preservation checks.
- Story 2.1 critical error cases covered: duplicate tenant rejection, authorization rejection, missing required fields, unavailable command surface, malformed/unavailable status lookup, publish failure, timeout, no false success before projection evidence, no success after rejected/degraded/unable-to-verify status, SignalR nudge without confirmation, and audit evidence unavailable/pending handoff.
- Story 2.2 acceptance criteria: 10/10 covered across add-member command gateway tests, add-member flow component tests, add-member lifecycle state tests, member-table preservation tests, and EN/FR resource parity tests.
- Story 2.2 critical error cases covered: `UserAlreadyInTenantRejection` (rejected, never NoOp, never optimistic member row), `RoleEscalationRejection`, `InsufficientPermissionsRejection`, `TenantDisabledRejection`, `TenantNotFoundRejection`, `TenantRole.Unknown`/empty-user-id validation blocking before submit, fail-closed stale/unknown-freshness/unauthorized/tenant-lifecycle/unavailable-surface/in-flight admission, no projection-free success, accepted-vs-projection-pending non-collapse, SignalR nudge without confirmation, and audit pending/unavailable/missing-support handoff.
- Story 2.3 acceptance criteria: 10/10 covered across change-role command gateway tests, change-role flow component tests, change-role lifecycle state tests, member-table/detail preservation tests, responsive/accessibility style checks, and EN/FR resource parity tests.
- Story 2.3 critical error cases covered: current-role NoOp as `AlreadyApplied`, zero-event backend NoOp as `AlreadyApplied`, `RoleEscalationRejection`, `UserNotInTenantRejection`, `InsufficientPermissionsRejection`, `TenantDisabledRejection`, `TenantNotFoundRejection`, spoofed `TenantRole.Unknown`/empty-user-id validation blocking before submit, fail-closed stale/unknown-freshness/unauthorized/degraded/tenant-lifecycle/unavailable-surface/in-flight admission, owner-count risk warning without hard-blocking, no projection-free success, missing target member unable-to-verify, accepted-vs-projection-pending non-collapse, manual refresh confirmation only after projection re-query, SignalR nudge without confirmation, and audit pending/unavailable/missing-support handoff.
- Story 2.4 acceptance criteria: 8/8 covered across remove-member command gateway tests, destructive-flow component tests, remove-member lifecycle state tests, member-table/detail preservation tests, accessibility/responsive style checks, and EN/FR resource parity tests.
- Story 2.4 critical error cases covered: fail-closed incomplete preview, stale/unknown freshness, unauthorized/degraded detail, disabled/unknown tenant lifecycle, command-surface unavailable, duplicate in-flight submission, last-owner friction, global-admin risk separation without `RemoveGlobalAdministrator`, no projection-free confirmation, `UserNotInTenantRejection` requiring absent projection evidence before already-applied, SignalR nudge without confirmation, audit pending/delayed/unavailable/missing-support handoff, and no raw payload/token/correlation leakage.
- Story 2.5 acceptance criteria: 8/8 covered across update-metadata command gateway tests, edit-flow component tests, update-metadata lifecycle state tests, tenant-detail composition, one-command-at-a-time locking signal, accessibility/live-region assertions, support-safe copy assertions, and EN/FR resource parity.
- Story 3.1 acceptance criteria: 7/7 covered across lifecycle availability model tests, lifecycle action component tests, tenant-detail composition tests, accessibility/live-region assertions, source checks proving no lifecycle submission path, CSS responsive/forced-colors hooks, and EN/FR resource parity.
- Story 3.2 acceptance criteria: 7/7 covered across lifecycle gateway tests, lifecycle command reducer tests, high-impact lifecycle component tests, tenant-detail composition/command-lock wiring, resource entries, and existing Story 3.1 availability/fail-closed coverage.
- Story 2.5 critical error cases covered: required name validation, clear-to-null description, stale/unknown freshness, unauthorized/degraded detail, disabled/unknown tenant lifecycle, command-surface unavailable, gateway submission failure, in-flight command lock, no projection-free confirmation, accepted-vs-projection-pending non-collapse, same-metadata submission without NoOp/already-applied suppression, SignalR nudge without confirmation, cancel/Escape close without submit, audit pending/delayed/unavailable/missing-support handoff, forced-colors/focus status rendering hooks, and no raw payload/token/correlation leakage.
- Story 3.1 critical error cases covered: unresolved destructive governance blocked by default, indeterminate global-admin authority, missing command-surface support, active enable/disabled disable same-state rejection copy, disabled enable remaining governance-blocked without optimistic active transition, stale/unknown freshness, aging freshness not bypassing unresolved governance, degraded/unauthorized/unavailable/unknown detail surfaces, unknown tenant status, narrow/mobile fail-closed mode, no optimistic lifecycle transition, no lifecycle command receipt/audit/success copy, and no raw payload/token/correlation leakage.
- Story 3.2 critical error cases covered: incomplete preview blocking, missing server-reflected authorization, stale/unknown freshness, missing command surface, command-surface unavailable action admission, narrow/mobile fail-closed safety context, cancel/Escape without commit, typed confirmation mismatch, duplicate in-flight lifecycle submission, one-at-a-time lock retention through projection pending, focus-loop trapping structure, `TenantLifecycleStateAlreadySet`, `TenantDisabled`, `TenantNotFound`, `InsufficientPermissions`, projection-pending without intended lifecycle evidence, terminal non-success states with matching projection evidence, SignalR nudge without confirmation, audit pending/delayed/unavailable/missing-support handoff, and no raw payload/token/correlation leakage.

## Validation

### Current Story 1.8 Validation

- Story 1.8 follow-up review validation (2026-07-21): the Release UI test build passed with 0 warnings / 0 errors; the exact seven-class focused executable passed 241/241; the `SupportSafeCopyButtonTests` class passed 43/43; the full UI executable passed 976/976; the Release solution build passed with 0 warnings / 0 errors; and `git diff --check` passed. Authenticated browser and human NVDA usability evidence remains explicitly blocked in the dated Story 1.8 report.

### Historical Validation Retained for Provenance

- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -warnaserror -m:1 -nr:false` passed with 0 warnings and 0 errors.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 -nr:false` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor` passed: 172 total, 0 errors, 0 failed, 0 skipped (169 dev tests + 3 story-automator-review regression tests).
- Story 2.2 validation: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore -warnaserror -m:1 -nr:false` passed with 0 warnings and 0 errors; `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build -m:1 -nr:false` again hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility, so the xUnit v3 executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` was used and passed: 205 total, 0 errors, 0 failed, 0 skipped.
- `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -warnaserror -m:1 -nr:false` passed with 0 warnings and 0 errors.
- `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -p:BuildInParallel=false -p:RestoreBuildInParallel=false -m:1 -v:m` passed with 0 warnings and 0 errors.
- xUnit v3 executable fallback passed for Tier 1/UI projects: Contracts.Tests 103, Client.Tests 47, Testing.Tests 181, Sample.Tests 31, UI.Tests 156; all 518 passed with 0 failures.
- `tests/Hexalith.Tenants.Server.Tests/bin/Release/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor` was attempted and failed in pre-existing documentation/AppHost checks for missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` and stale deployment-readiness evidence unrelated to Story 2.1.
- Story 2.3 validation: `MSBUILDDISABLENODEREUSE=1 dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- Story 2.3 validation: `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility, so the xUnit v3 executable fallback was used.
- Story 2.3 validation: `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests` passed: 244 total, 0 errors, 0 failed, 0 skipped.
- Story 2.3 QA generation validation: `MSBUILDDISABLENODEREUSE=1 dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- Story 2.3 QA generation validation: `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-build` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility, so the xUnit v3 executable fallback was used.
- Story 2.3 QA generation validation: `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed: 252 total, 0 errors, 0 failed, 0 skipped.
- Story 2.3 broader executable regression signal: Contracts.Tests 103/103, Client.Tests 47/47, and Testing.Tests 181/181 passed. Server.Tests executable was attempted and failed in existing documentation/AppHost checks for missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` and an unrelated deployment-readiness summary expectation.
- Story 2.4 validation: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- Story 2.4 validation: `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Release --no-restore` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility.
- Story 2.4 validation: `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests` passed: 283 total, 0 errors, 0 failed, 0 skipped.
- Story 2.4 broader executable regression signal: Contracts.Tests 103/103, Client.Tests 47/47, Testing.Tests 181/181, and Sample.Tests 31/31 passed. Server.Tests failed in existing documentation/AppHost checks for missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` plus stale deployment-readiness evidence. IntegrationTests reported DAPR prerequisites unavailable for skipped tests and failed 54 tests with DaprException/InternalServerError behavior in this environment.
- Story 2.4 QA generation validation (2026-06-06T06:57:51+02:00): `MSBUILDDISABLENODEREUSE=1 dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- Story 2.4 QA generation validation (2026-06-06T06:57:51+02:00): `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-build` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility.
- Story 2.4 QA generation validation (2026-06-06T06:57:51+02:00): `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed: 285 total, 0 errors, 0 failed, 0 skipped.
- Story 2.4 Senior Developer Review (AI) (2026-06-06T07:08:03+02:00): auto-fixed submission-time `UserNotInTenant` reconciliation in `RemoveTenantMemberFlow` (refresh recovery now reachable without a tracking handle so absent projection evidence yields already-applied per AC4) and added regression test `Submission_time_user_not_in_tenant_rejection_reconciles_to_already_applied_after_absent_projection`. `dotnet build ...Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings/0 errors; `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests` passed: 286 total, 0 errors, 0 failed, 0 skipped.
- Story 2.5 validation (2026-06-06T07:31:00+02:00): `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors. `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build --logger "console;verbosity=minimal"` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility. xUnit v3 executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests` passed: 311 total, 0 errors, 0 failed, 0 skipped. `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Release -m:1 --no-restore` was attempted and failed on NU1900 because restricted network access prevented NuGet vulnerability data retrieval.
- Story 2.5 QA generation validation (2026-06-06T07:39:47+02:00): `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors. `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests` passed: 315 total, 0 errors, 0 failed, 0 skipped.
- Story 2.5 Senior Developer Review (AI) (2026-06-06T08:05:00+02:00): auto-fixed `EditTenantMetadataFlow` confirmed-metadata display to source last-confirmed name/description from the proven `LastConfirmedDetailProjection` instead of the ambient `Detail` parameter (a confirmed clear-to-null description no longer resurfaces the old description, preserving AC2/AC8 no-optimistic-overwrite), and added regression test `Confirmed_clear_to_null_description_shows_empty_state_and_not_the_ambient_detail_description` (verified to fail on the prior derivation). `dotnet build ...Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings/0 errors; `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests` passed: 316 total, 0 errors, 0 failed, 0 skipped.
- Story 3.1 validation (2026-06-06T08:15:08+02:00): `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors. `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build --no-restore` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility. xUnit v3 executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo` passed: 336 total, 0 errors, 0 failed, 0 skipped.
- Story 3.1 QA generation validation (2026-06-06T08:30:20+02:00): `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors. `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build --no-restore -m:1` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility. xUnit v3 executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed: 340 total, 0 errors, 0 failed, 0 skipped.
- Story 3.1 broader regression signal (2026-06-06T08:15:08+02:00): executable tests passed for Contracts.Tests 103/103, Client.Tests 47/47, Testing.Tests 181/181, and Sample.Tests 31/31. Broader non-UI project builds were attempted with `dotnet build <project> -c Release -m:1 --no-restore` and were blocked by NU1900 because restricted network access prevented NuGet vulnerability data retrieval. Server.Tests executable was attempted and failed in existing documentation/AppHost checks for missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` plus stale deployment-readiness evidence unrelated to Story 3.1.
- Story 3.2 validation (2026-06-06T11:27:23+02:00): `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors. `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build --no-restore` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility. xUnit v3 executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed: 353 total, 0 errors, 0 failed, 0 skipped.
- Story 3.2 broader executable regression signal (2026-06-06T11:27:23+02:00): Contracts.Tests 103/103, Client.Tests 47/47, Testing.Tests 181/181, Sample.Tests 31/31, and UI.Tests 353/353 passed via xUnit v3 executable fallback.
- Story 3.2 QA generation validation (2026-06-06T11:35:02+02:00): `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors. xUnit v3 executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed: 369 total, 0 errors, 0 failed, 0 skipped.
- Story 3.2 Senior Developer Review (AI) (2026-06-06T11:57:09+02:00): auto-fixed lifecycle command activity retention so accepted/projection-pending disable/enable commands keep other command surfaces locked until projection truth confirms or a terminal non-pending state releases the lock, added focus-loop sentinels to the high-impact confirmation dialog, and added regression coverage for command-surface lock admission and projection-confirmed lock release. `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors. xUnit v3 executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed: 370 total, 0 errors, 0 failed, 0 skipped.

## Checklist
- [x] API tests generated if applicable.
- [x] UI component and workflow tests generated for the implemented surfaces using the existing xUnit v3/bUnit framework.
- [x] Tests use standard xUnit v3, Shouldly, NSubstitute, bUnit, and existing file-based style/resource assertions.
- [x] Tests cover happy path and critical unsafe/empty/unavailable/failed/disconnected cases.
- [x] Tests use semantic/accessibility-oriented assertions and stable selectors.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and order-free.
- [x] Test summary created with coverage metrics.

## Story 3.3 Evidence Addendum

- [x] `tests/Hexalith.Tenants.UI.Tests/Components/SetTenantConfigurationFlowTests.cs` - Covers set-configuration flow stable selectors, complete inline consequence preview, keyboard form submission through projection-confirmed completion, namespace/prefix evidence fail-closed behavior, explicit authorization and missing-scope admission blocking, required namespace/key/value validation, key/value domain limits, identical-value `AlreadyApplied`, accepted/projection-pending/confirmed/rejected/failed/degraded/unable-to-verify states, zero-event reconciliation, support-safe preview/lifecycle copy, cancel/Escape close with focus-return callback, live-region politeness, mobile/forced-colors/focus CSS hooks, and parent command activity locking.
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantSetConfigurationCommandSnapshotTests.cs` - Covers configuration reducer projection evidence requirements, literal key/value confirmation, zero-event NoOp reconciliation only after projection proof, SignalR nudge-only behavior, blocked recovery focus, projection-refresh recovery focus, terminal non-success non-collapse, audit pending/delayed/unavailable/missing-support states, and last-confirmed configuration preservation.
- [x] `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs` - Covers set-configuration command gateway submission shape, literal tenant id preservation, `SetTenantConfiguration` command name, generated message id, correlation id capture, validation blocking before submit, configuration-specific safe rejection mapping, shared status mapping, and support-unsafe failure redaction.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs` - Covers composition of the focused configuration command flow into the existing configuration read surface while preserving namespace grouping, filtering, table semantics, redaction/copy behavior, resource parity, and tenant-detail command-lock wiring.
- Story 3.3 acceptance criteria: 8/8 covered across set-configuration gateway tests, component workflow tests, lifecycle reducer tests, tenant-detail/configuration composition tests, accessibility/live-region assertions, support-safe preview/rejection assertions, responsive/forced-colors CSS hooks, and EN/FR resource parity.
- Story 3.3 critical error cases: missing namespace/key/value, key length above 256, value length above 1024, missing namespace-scope evidence, explicit authorization denial, stale/unknown freshness, unauthorized/degraded detail, disabled/unknown tenant lifecycle, command-surface unavailable, narrow/mobile safety blocking, duplicate in-flight submission, identical key/value pre-submit NoOp, zero-event backend NoOp requiring projection proof, `ConfigurationLimitExceeded`, `InsufficientPermissions`, `TenantDisabled`, `TenantNotFound`, projection pending without matching literal key/value evidence, SignalR nudge without confirmation, audit pending/delayed/unavailable/missing-support handoff, and no raw value/payload/token/correlation leakage in support surfaces.
- Story 3.3 QA generation validation (2026-06-06T12:25:37+02:00): `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors. `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility. xUnit v3 executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed: 416 total, 0 errors, 0 failed, 0 skipped.
- Story 3.3 broader executable regression signal (2026-06-06T12:25:37+02:00): UI.Tests 416/416 passed via xUnit v3 executable fallback. Existing broader Story 3.3 signal remains unchanged: Contracts.Tests 103/103, Client.Tests 47/47, and Testing.Tests 181/181 previously passed; known Server.Tests documentation/AppHost failures remain unrelated to Story 3.3.
- Story 3.3 Senior Developer Review (AI) (2026-06-06T12:36:59+02:00): auto-fixed degraded projection-state reason honesty, owned in-flight command lock messaging, completed-preview `aria-describedby` wiring, and field-specific validation focus in `SetTenantConfigurationFlow`; added regression coverage in `SetTenantConfigurationFlowTests`. `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors. `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility. xUnit v3 executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests` passed: 417 total, 0 errors, 0 failed, 0 skipped.

## Story 3.4 Evidence Addendum

- [x] `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs` - Covers remove-configuration command gateway submission shape, literal tenant id/key preservation, `RemoveTenantConfiguration` command name, generated message id, correlation id capture, validation blocking before submit, configuration-key-not-found mapping, shared safe rejection mapping, and support-unsafe failure redaction.
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantRemoveConfigurationCommandSnapshotTests.cs` - Covers preview/request/accepted/projection-pending/confirmed/rejected/degraded/unable-to-verify states, projection-confirmed absence, no optimistic configuration deletion, `ConfigurationKeyNotFound` staying rejected, SignalR nudge-only behavior, terminal non-success non-collapse, and audit pending/delayed/unavailable/missing-support states.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantConfigurationFlowTests.cs` - Covers row-launched remove preview selectors, complete consequence preview, exact-key destructive confirmation, focus-loop selectors, namespace/scope and missing-target fail-closed behavior, projection-confirmed removal, projection-pending when the key remains visible with command activity retained, `ConfigurationKeyNotFound` rejected lifecycle, cancel/Escape no-commit behavior, live-region politeness, forced-colors/focus/narrow CSS hooks, and support-safe rendering.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs` - Covers remove-flow composition into the existing configuration read surface while preserving grouping, filtering, table semantics, redaction/copy behavior, the Story 3.3 set flow, exact launcher focus return after cancel, resource parity, and tenant-detail command-lock wiring.
- Story 3.4 acceptance criteria: 8/8 covered across remove-configuration gateway tests, component workflow tests, lifecycle reducer tests, tenant-detail/configuration composition tests, accessibility/live-region assertions, support-safe preview/rejection assertions, responsive/forced-colors CSS hooks, and EN/FR resource parity.
- Story 3.4 validation (2026-06-06T13:00:00+02:00): `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors. `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build --no-restore` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility. xUnit v3 executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed: 444 total, 0 errors, 0 failed, 0 skipped.
- Story 3.4 broader executable regression signal (2026-06-06T13:00:00+02:00): `dotnet build Hexalith.Tenants.slnx -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors. Contracts.Tests 103/103, Client.Tests 47/47, Testing.Tests 181/181, Sample.Tests 31/31, and UI.Tests 444/444 passed via xUnit v3 executable fallback. Server.Tests executable was attempted and failed in existing documentation/AppHost checks for missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` plus stale deployment-readiness evidence unrelated to Story 3.4.
- Story 3.4 QA generation validation (2026-06-06T13:10:25+02:00): Added regression coverage for remove-configuration launcher focus return after cancel and mirrored the existing row-keyed focus restoration pattern in the configuration surface. `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors. `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build --no-restore` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility. xUnit v3 executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed: 445 total, 0 errors, 0 failed, 0 skipped.
- Story 3.4 Senior Developer Review (AI) (2026-06-06T13:21:25+02:00): Auto-fixed exact-key destructive confirmation, focus-loop sentinels/modal semantics, and projection-pending command activity retention in `RemoveTenantConfigurationFlow`; added regression coverage for confirmation blocking and one-at-a-time lock retention. `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors. xUnit v3 executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed: 446 total, 0 errors, 0 failed, 0 skipped.

## Story 4.2 Evidence Addendum

- [x] `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs` - Adds API coverage for `GET /api/global-administrators` missing-auth 401 behavior, fixed `system`/`global-administrators` query dispatch with signed cursor forwarding, invalid cursor rejection, and wrong-scope cursor rejection without routing or leaking cursor data.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs` - Adds bUnit workflow coverage for empty/invalid/unavailable state semantics, no false success or hidden-row disclosure, refresh reusing ETag and previous snapshot, cursor-based next-page loading, stable selectors, and no tenant/user substitute markers.
- Story 4.2 acceptance criteria: 8/8 covered across existing fixed-aggregate contract/handler/gateway/page tests plus this QA addendum for API route authorization/cursors and UI review-surface interactions.
- Story 4.2 critical error cases covered: missing authorization, invalid cursor, wrong cursor scope, empty authorized result, unavailable read surface, stale/degraded freshness with rows preserved, ETag refresh, support-safe literal identifiers, fail-closed grant/remove reasons, no `/api/tenants` or `/api/users` substitute markers, no hidden administrator rows, and no raw token/cursor/success leakage in rendered states.
- Story 4.2 QA generation validation (2026-06-06T14:47:38+02:00): `MSBUILDDISABLENODEREUSE=1 dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors. `MSBUILDDISABLENODEREUSE=1 dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors. xUnit v3 executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed: 470 total, 0 errors, 0 failed, 0 skipped. Focused integration executable fallback for Story 4.2 global-admin route/cursor methods passed: 15 total, 0 errors, 0 failed, 0 skipped.
- Story 4.2 broader integration signal (2026-06-06T14:47:38+02:00): full `TenantsQueryControllerIntegrationTests` class was attempted and remains blocked by existing unrelated DAPR-backed tenant query rows returning `InternalServerError`; the isolated global-administrator route/cursor tests pass.

## Story 4.2 Checklist

- [x] API tests generated where applicable.
- [x] E2E-style workflow tests generated for the implemented UI surface using the existing xUnit v3/bUnit framework.
- [x] Tests use standard xUnit v3, Shouldly, bUnit, and existing integration-test APIs.
- [x] Tests cover happy path and critical empty/invalid/unavailable/unauthorized/cursor cases.
- [x] Tests use stable selectors and accessibility-oriented state assertions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and order-free.
- [x] Test summary created with coverage metrics.

## Story 4.3 Evidence Addendum

- [x] `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` - Adds API coverage for `SetGlobalAdministrator` through `/api/v1/commands`, fixed `system/global-administrators/global-administrators` routing, literal `{ UserId }` payload without tenant membership fields, global-admin caller capture, accepted response correlation, and `GlobalAdministratorAlreadyExistsRejection` ProblemDetails without success or AlreadyApplied copy.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs` - Extends grant-flow workflow coverage for read-surface-unavailable and command-surface-unavailable fail-closed blocking, no command submission, no read projection query when read support is unavailable, missing-support audit copy, assertive live-region behavior, cancel recovery, literal user id clearing, and no optimistic administrator row insertion.
- [x] Existing Story 4.3 tests cover gateway fixed-scope command submission, ULID message id creation, blank and whitespace-only user id rejection, safe rejection mapping, platform-specific gateway-unavailable copy, status lookup copy, projection-confirmed success, no projection-free success, terminal lifecycle states, one-at-a-time locking, SignalR nudge-only behavior, resource parity, forced-colors/focus hooks, and support-safe rendered text.
- Story 4.3 acceptance criteria: 8/8 covered across command API integration tests, UI command gateway tests, global-administrator grant component tests, lifecycle reducer tests, resource/style assertions, and fixed global-administrator query-route tests from Story 4.2.
- Story 4.3 critical error cases covered: blank and whitespace-only user id validation, read support unavailable, command support unavailable, stale/degraded freshness, unauthorized/indeterminate platform authority reflection, duplicate target user rejection, insufficient permissions, publish failure, timeout, status unavailable, completed command without projection evidence, SignalR nudge without confirmation, in-flight command locking, cancel without submit, and no raw payload/token/correlation leakage in rendered grant copy.
- Story 4.3 QA generation validation (2026-06-07T09:44:12+02:00): `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors. `dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors. xUnit v3 executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed: 641 total, 0 errors, 0 failed, 0 skipped. Focused integration executable fallback for the two Story 4.3 command API methods passed: 2 total, 0 errors, 0 failed, 0 skipped.

## Story 4.3 Checklist

- [x] API tests generated where applicable.
- [x] E2E-style workflow tests generated for the implemented UI surface using the existing xUnit v3/bUnit framework.
- [x] Tests use standard xUnit v3, Shouldly, NSubstitute, bUnit, and existing integration-test APIs.
- [x] Tests cover happy path and critical validation/rejection/unavailable/lifecycle cases.
- [x] Tests use stable selectors and accessibility-oriented state assertions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and order-free.
- [x] Test summary created with coverage metrics.

## Story 5.4 Evidence Addendum

- [x] API tests: not applicable. Story 5.4 adds Tenants UI state, localized copy, shared Blazor controls, and command/receipt rendering paths; it adds no backend endpoint, query contract, direct browser API call, token storage, or command dispatch surface.
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantAuditAvailabilityTests.cs` - Covers every `TenantCommandAuditState` audit availability mapping, canonical recovery verbs, non-rendered `NotStarted`, live-region politeness, and no false audit-success state.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/AuditAvailabilityStateTests.cs` - Covers visible state text, icon/shape, accessible label, stable `tenants-audit-availability` selectors, polite/assertive live-region behavior, native keyboard-operable recovery controls, passive wait, refresh/continue/escalate callbacks, inspect-audit fragment reuse, forced-colors/focus/reduced-motion CSS hooks, stable dimensions, and no machine-token or Success leakage.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceReceiptTests.cs` - Covers shared availability rendering from pending/delayed/unavailable/missing-support receipt states, refresh recovery, continue-read-only and inspect-audit focus-return callbacks through `OnClose`, support-safe receipt fields/copy, live-region behavior, and no false Success.
- [x] Existing representative command-flow and reducer tests cover create, add member, change role, remove member, lifecycle, set/remove configuration, and edit metadata audit handoffs while preserving command acceptance, projection confirmation, and audit availability as distinct state tokens.
- Story 5.4 acceptance criteria: 7/7 covered across audit availability state tests, shared component tests, receipt/page tests, representative command-flow tests, resource parity/source-safety checks, CSS accessibility checks, and no false Success assertions.
- Story 5.4 critical error cases covered: audit pending, audit delayed, audit unavailable, missing implementation support, `NotStarted`, rejected/degraded/unable-to-verify non-success states, projection-free command completion, missing loaded receipt reference, partial receipt with no safe copy reference, unavailable receipt support-safe copy, machine-token leakage, browser backend/token storage, forced-colors rendering, keyboard recovery action semantics, focus-return callback routing, and polite/assertive live-region boundaries.
- Story 5.4 QA generation validation (2026-06-06T17:59:48+02:00): `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors. `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build --no-restore` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility. xUnit v3 executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed: 572 total, 0 errors, 0 failed, 0 skipped.

## Story 5.4 Checklist

- [x] API tests generated if applicable.
- [x] E2E-style workflow tests generated for the implemented UI surfaces using the existing xUnit v3/bUnit framework.
- [x] Tests use standard xUnit v3, Shouldly, bUnit, and existing file/resource/style assertions.
- [x] Tests cover happy path and critical pending/delayed/unavailable/missing-support error cases.
- [x] Tests use semantic/accessibility-oriented assertions and stable selectors.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and order-free.
- [x] Test summary created with coverage metrics.

## Story 3.5 Evidence Addendum

- [x] `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs` - Covers the REST-backed tenant query gateway for list, detail, my-tenants, user-tenants, global administrators, and audit paths, including endpoint/path construction, literal tenant/user id preservation, cursor/page-size pass-through, `If-None-Match` forwarding, 304 snapshot preservation, stale/degraded/not-modified handling, and safe error-to-state mapping.
- [x] `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsQueryApiClientTests.cs` - Covers the typed `HttpClient` query client for GET-only transport, strong ETag forwarding, projection-version/served-at metadata capture, 304 no-body handling, and ProblemDetails-to-gateway-exception mapping without requiring user-facing raw payload exposure.
- [x] `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs` - Covers the REST query controller ETag/freshness contract, matching `If-None-Match` -> 304 with no body, mismatched `If-None-Match` -> 200 with the current ETag, RBAC authorization before dispatch, identifier/cursor guards, and typed projection route forwarding through in-process query handlers.
- [x] `tests/Hexalith.Tenants.Server.Tests/Queries/TenantQueryHandlerETagTests.cs` - Covers primary read-model ETag propagation for list, detail, users, user-tenants, and audit query handlers.
- [x] `tests/Hexalith.Tenants.Contracts.Tests/SolutionStructureTests.cs` - Adds a guard that UI and Contracts source no longer reference tenant projection-actor routing symbols.
- Story 3.5 story-owned acceptance coverage: AC1-8 are covered across REST controller tests, handler ETag tests, UI gateway/API-client tests, AppHost/UI DI build validation, source guard assertions, and documentation/evidence updates. AC9 story-owned build/test signal is warning-clean; full Tier 2 caveat is documented below.
- Story 3.5 validation (2026-06-07T10:37:08+02:00): affected builds passed with 0 warnings and 0 errors for UI.Tests, AppHost, Contracts.Tests, IntegrationTests, Server.Tests, Testing.Tests, and Client.Tests using `MSBUILDDISABLENODEREUSE=1 dotnet build ... -c Release -m:1 --no-restore`. xUnit v3 executable fallback passed: UI.Tests 644/644, focused TenantsQueryGateway/TenantsQueryApiClient 69/69, focused handler ETag tests 5/5, full Contracts.Tests 106/106, full Client.Tests 47/47, full Testing.Tests 181/181, and full `TenantsQueryControllerIntegrationTests` 95/95.
- Broader regression signal: full Server.Tests was attempted and remains blocked by existing documentation/AppHost evidence checks for missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` plus stale deployment-readiness summary expectations. Full IntegrationTests was attempted and is down to existing unrelated health-readiness contract drift (`dapr-statestore` vs current `dapr-statestore-tenants` registration and development JSON exception-sanitization behavior); DAPR-dependent rows skip when local DAPR prerequisites are unavailable.

## Story 3.5 Checklist Result

- API tests generated for the five Tenants REST query endpoints and conditional-read behavior.
- E2E-style gateway workflow tests generated through existing xUnit v3 service-level UI gateway coverage.
- Tests use standard xUnit v3, Shouldly, NSubstitute/test-local handlers, WebApplicationFactory, and existing query doubles.
- Tests cover happy path, stale/degraded/not-modified, unauthorized/forbidden/not-found/unavailable, invalid cursor/identifier, and support-safety boundaries.
- Tests use stable semantic assertions and no hardcoded waits or sleeps.
- Summary includes coverage metrics and validation evidence.

## Story 3.5 QA E2E Generation Addendum (2026-06-07)

Gap identified during `/bmad-qa-generate-e2e-tests 3.5`: AC7's fail-closed fallback
(`UnavailableTenantQueryGateway`, bound when no tenant query route is configured) had only DI
build validation and no behavioral coverage. Added a dedicated fixture to close it.

- [x] `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/UnavailableTenantQueryGatewayTests.cs` (8 tests) -
  Covers the AC7 fail-closed fallback across all six read surfaces: `ListTenantsAsync` -> `Error`;
  `GetTenantAsync` -> `Unavailable`; `GetMyTenantsAsync` / `GetUserTenantsAsync` /
  `GetGlobalAdministratorsAsync` / `GetTenantAuditAsync` -> `Unavailable` with the
  `GatewayUnavailable` reason. Asserts AC5 never-fabricated freshness (every surface stays
  `ReadModelFreshnessState.Unknown`, never `Current`), `ArgumentNullException` guards on the two
  scope-bearing reads, preservation of `TargetUserId` and the audit request scope
  (tenant/from/to/category) in the fail-closed snapshot, and AC8 support-safety: a previously-good
  `previous` snapshot is never served as current and the caller-supplied ETag/cursor is never
  echoed into any user-facing field.

- Coverage delta: Story 3.5 AC7 fail-closed behavior moves from build-only to behavioral, and
  AC5/AC8 gain explicit assertions on the unconfigured-gateway path. Story 3.5 acceptance criteria
  remain fully addressed (9/9), with AC9 = warning-clean build + green suite below.
- Validation (2026-06-07): `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj`
  passed with 0 warnings / 0 errors. xUnit v3 executable fallback
  `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll` passed:
  652 total (644 prior + 8 new), 0 errors, 0 failed, 0 skipped.

## CC 2026-06-19 Tenant Query Freshness/ETag/Coverage Hardening

- [x] `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs` - Adds coverage that `ServedAt` alone is not freshness evidence and that live populated ProblemDetails (`correlationId`, `reasonCode`, raw payload text, stack traces, tokens, cursors, ETags) maps to support-safe user copy.
- [x] `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsQueryApiClientTests.cs` - Adds null/no-ETag metadata coverage plus client `If-None-Match` normalization for weak tags, `*`, unsupported multi-tag input, whitespace/control input, and escaped strong tags.
- [x] `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs` - Adds REST controller coverage for no-ETag 200/no-304 behavior, unsupported `If-None-Match` safe 200 handling, escaped strong ETag 304 comparison, and REST/handler persisted read-model reconstruction after app-factory recreation.
- [x] `tests/Hexalith.Tenants.Server.Tests/Queries/TenantQueryHandlerETagTests.cs` - Updates the handler metadata assertion so query results expose ETag/projection-version without fabricating `ServedAt`.
- Coverage: story AC1, AC3-AC8 covered; AC2's `aging`/`stale` threshold portion is deferred to the EventStore read-model freshness handoff (`eventstore-2026-06-19-read-model-freshness-metadata`). The direct-read D6 model is ETag/projection-version `current`; unmarked responses are `unknown`; generic persisted projection age/version remains an EventStore owner handoff before threshold-based `aging` can be computed.
- Validation (2026-06-19T14:18:04+02:00): focused `Server.Tests --filter Query` passed 90/90; focused `UI.Tests --filter TenantQuery` passed 76/76; full `TenantsQueryControllerIntegrationTests` passed 101/101; `StatelessRestart` filter skipped its 1 DAPR-dependent test.
- Regression (2026-06-19T14:18:04+02:00): Contracts.Tests 106/106, Client.Tests 47/47, Testing.Tests 181/181, Sample.Tests 32/32, UI.Tests 695/695, IntegrationTests 204 passed / 33 skipped. Full Server.Tests remains blocked by 3 unrelated DAPR dead-letter metadata expectation tests (`enableDeadLetter` / `deadLetterTopic` absent).
  - Correction (2026-06-21): the Server.Tests blocker noted above was resolved on 2026-06-20 by `cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup` — the misleading `enableDeadLetter` / `deadLetterTopic` keys were removed from the local and production pub/sub components and full Server.Tests now passes 700/700. The 2026-06-19 line is retained as historical evidence; treat this correction as the current state.

## CC 2026-06-25 Tenant Read-Model Freshness Adoption

- [x] `tests/Hexalith.Tenants.Server.Tests/Projections/ReadModelFreshnessTests.cs` - Proves the four persisted read models implement `IReadModelFreshness`, start with unknown freshness metadata, and keep projection `ProjectedAt` distinct from tenant `CreatedAt` and audit entry timestamps.
- [x] `tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionHandlerTests.cs` and `tests/Hexalith.Tenants.Server.Tests/Projections/GlobalAdministratorProjectionHandlerTests.cs` - Add deterministic `TimeProvider` coverage for stamping tenant detail, audit, index, global-administrator, and system audit projection writes.
- [x] `tests/Hexalith.Tenants.Server.Tests/Queries/TenantQueryFreshnessTests.cs` - Covers server-side `ToQueryResponseMetadata` classification for current, aging-collapsed-to-wire-current, stale, unknown, and all six primary query-handler read-model sources.
- [x] `tests/Hexalith.Tenants.Server.Tests/Configuration/SnapshotConfigurationTests.cs` and `tests/Hexalith.Tenants.Server.Tests/Queries/TenantQueryHandlerETagTests.cs` - Cover conservative threshold binding/validation and preserve ETag fail-closed behavior with deterministic served time.
- [x] `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`, `UnavailableTenantQueryGatewayTests.cs`, and UI state/component tests - Migrated durable UI state to `ReadModelFreshnessState`; `Refreshing` is covered only through `TruthStateBadge.IsRefreshing`.
- Coverage: story AC1-6 covered. Server metadata now uses persisted `ProjectedAt` and configured thresholds; `ServedAt` is response metadata only. The UI consumes shared freshness state, keeps stale/unknown gates fail-closed, and EN/FR freshness resource key parity remains 55/55.
- Validation (2026-06-25): focused `Server.Tests --filter "Freshness|Query"` passed 123/123; focused `UI.Tests --filter "Freshness|TenantQuery|Badge"` passed 95/95; Release solution build with `-warnaserror --no-restore` passed 0 warnings / 0 errors.
- Regression (2026-06-25): Contracts.Tests 106/106, Client.Tests 48/48, Testing.Tests 181/181, Sample.Tests 39/39, Server.Tests 735/735, UI.Tests 761/761, IntegrationTests 223 passed / 1 skipped. The broad Server pass also reconciled existing Memories local AppHost DAPR-scope evidence from `memories-server` to the current `memories` app id.

## Story 1.6 Corrective Evidence Addendum (2026-07-22)

- [x] `TenantConfigurationReadPolicyTests` covers runtime typed binding, valid-empty versus unavailable policy, authenticated-subject corroboration, ordinary and global-administrator evidence, ordinal longest-prefix authorization, exact positive display approval, malformed/scalar/duplicate policy rejection, Unicode/case/boundary behavior, defensive copying, and no startup-fatal validation.
- [x] `TenantQueryGatewayTests` covers pre-snapshot raw filtering, initial unavailable versus same-tenant degraded retention, unconditional `304` recovery, wrong-tenant rejection, hidden-state absence, and set/remove proof-only current projection outcomes for matching, nonmatching, missing, not-modified, stale, degraded, unknown, unauthenticated, wrong-tenant, and exception paths.
- [x] `TenantDetailSurfaceTests` and `TenantConfigurationEndToEndTests` cover strict `tenants-config-read-*` inspection markup, safe summaries/filtering/grouping, valid-empty/unavailable/filtered-empty truth, literal Unicode/bidi/markup-like values, accessible literal value text, responsive overflow, sibling management composition, mutually exclusive management states, safe removal targets, target-specific accessible names, and the authenticated SSR principal → raw query → BFF policy → sanitized snapshot → DOM boundary.
- [x] Set/remove component and reducer suites preserve preview, validation, locking, focus return, cancellation, safe lifecycle/audit/recovery states, proof-only projection confirmation, exact full-key set semantics (including exact grant key `P`), safe-row-only removal, and submission-time policy reauthorization/revocation blocking before gateway dispatch.
- [x] EN/FR `Tenants.Configuration.*` resource parity, Fluent component/style governance, forced-colors, visible focus, responsive hooks, stable data-independent selectors, and source-level no-copy/no-reveal/no-read-mutation guards pass. `CFG-1.6-SAFE-MODEL` is closed; configuration clipboard activation/certification remains intentionally absent.
- Validation: `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore --logger "console;verbosity=minimal"` passed 1031/1031. `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -m:1 --nologo` passed with 0 warnings and 0 errors. `dotnet test Hexalith.Tenants.slnx -c Release --no-build --no-restore --logger "console;verbosity=minimal"` passed Client 50/50, Contracts 112/112, Sample 39/39, Testing 181/181, Server 738/738, UI 1031/1031, and Integration 166 passed / 1 skipped (2317 passed, 1 skipped overall).

### Story 1.6 Addendum — Corrections from the 2026-07-27 adversarial code review

The five checked claims above overstated coverage in four places. Corrected, with the gaps now closed:

- **"hidden-state absence"** rested on six `ToString()` assertions that could not fail: the change converted
  `TenantDetailSnapshot` from a `record` to a `class`, deleting the compiler-generated `ToString`, and
  `TenantConfigurationProjectionProof` never had one — so both rendered as their bare type name. Fixed on `main` by
  `de2ded0`, which added support-safe overrides to both types. Real absence evidence is
  `TenantConfigurationEndToEndTests`, which asserts hidden keys and values are absent from rendered markup (covering DOM,
  `aria-label` accessible names, and the announcer in one pass).
- **"Unicode/case/boundary behavior"** was proven only for `IsPrefixMatch`. Grant tenant/subject matching and the
  `DisplaySafeKeys` set had no case-sensitivity coverage, and the story's required leading-empty-segment and visually
  confusable prefix cases were absent entirely. Added: `Grants_apply_only_to_their_exact_ordinal_tenant_and_subject`,
  `Display_approval_is_exact_and_ordinal_so_a_case_variant_key_is_not_approved`,
  `Empty_segment_and_confusable_keys_cannot_broaden_an_ordinal_prefix_grant`, and
  `A_consecutive_empty_segment_stays_inside_the_granted_namespace`.
- **"defensive copying"** was asserted by a test composing under an empty `DisplaySafe` list, so the rows were empty with
  or without a copy. Replaced by `Composed_rows_do_not_track_later_mutation_of_the_caller_owned_dictionary`, which
  composes a real row and then both adds to and removes from the source dictionary.
- **"submission-time policy reauthorization"** was proven only at the component guard, against an injected lambda; the
  production seam `TenantsBffComposition.ReauthorizeConfigurationManagementAsync` had zero test references. Added
  `TenantsBffCompositionTests` (9 tests) covering unchanged scope, revoked grant, cross-tenant fail-closed, and the
  ordinal key-authorization matrix.
- Additionally, the configuration support-safety redaction test (10 assertions on correlation ids, JWT-shaped strings,
  exception type names, stack-trace text and PII) was **deleted** by this story and never replaced, in the same change
  that added new exception paths. Restored as
  `Configuration_view_never_renders_error_metadata_correlation_ids_tokens_stack_traces_or_pii`.
- Also corrected: on logging and telemetry specifically, absence held only by construction — there was no logging surface
  in the configuration path and no test would have flagged a new one. The provider now emits a failure *category* only
  (`TenantConfigurationPolicyFailure`), which carries no tenant, subject, prefix, key, or value.

**Re-verified (2026-07-27), using the commands the story and kernel prescribe** — the earlier record used aggregate
`dotnet test` runs and a build without `-warnaserror`, and credited a `Sample.Tests` project that does not exist under
`tests/`:

- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore -warnaserror -m:1 -nr:false` — 0 warnings / 0 errors.
- Focused Release classes: `TenantConfigurationReadPolicyTests` 36/36, `TenantsBffCompositionTests` 9/9,
  `TenantQueryGatewayTests` 284/284, `TenantDetailSurfaceTests` 61/61, `SetTenantConfigurationFlowTests` 28/28,
  `RemoveTenantConfigurationFlowTests` 12/12, `DomainUiFluentConformanceTests` 51/51,
  `TenantConfigurationEndToEndTests` 1/1.
- Full UI suite 1312/1312; Contracts 116/116, Client 50/50, Testing 181/181, Server 738/738.
- `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -warnaserror -m:1 -nr:false` — 0 warnings / 0 errors.
- `Hexalith.Tenants.IntegrationTests` (Tier 3, non-blocking) was not run in this pass.

### Story 1.6 closure pass — the last open review item (2026-07-27)

The review left one patch open, classified "not implementable as specified": `"DisplaySafe": ""` binds to an empty
allow-list instead of failing closed. Re-probed against the pinned .NET 10 configuration stack before deciding.

- **The blocking claim is correct.** `"DisplaySafe": []`, `"DisplaySafe": ""` and an emptied
  `Tenants__ConfigurationReadPolicy__DisplaySafe` override all present as `Value == ""` with zero element children. No
  code-only fix can separate them, and re-applying the "treat empty value as scalar" fix still takes the shipped
  valid-empty `appsettings.json` default dark.
- **The blast radius was never established, and it is what closes the item.** The environment cannot reach this state:
  an emptied override does *not* shorten an already-declared list (`["a","b"]` still binds to two entries, because the
  declaring provider's element children win), and the one override shape that does reach the bound list
  (`…__DisplaySafe__0=`) arrives as a blank element that `TryValidate` already rejects. The residual is a hand-authored
  JSON scalar typo whose only effect is zero approved keys — it can withhold approval, never grant it.
- Closed as an accepted, test-pinned limitation rather than by a spec change. A required declared cardinality
  (`DisplaySafeCount`) was considered and rejected: it would catch "intended 2, got 0", but since the environment cannot
  produce that state it buys an operator sync footgun for near-zero benefit.
- Pinned by three tests, each verified by mutation rather than assumed:
  `An_emptied_environment_override_cannot_clear_a_declared_display_safe_list`,
  `An_emptied_display_safe_element_is_rejected_rather_than_silently_dropped`, and
  `An_empty_display_safe_scalar_approves_nothing_rather_than_widening_approval`. Mutating
  `HasScalarCollection` to `child.Value is not null` fails 5 tests (2 of them new); dropping the blank-key guard from
  `TryValidate` fails exactly the third.

Re-verified with the prescribed commands: UI test project build 0 warnings / 0 errors;
`TenantConfigurationReadPolicyTests` **39/39** (was 36), `TenantsBffCompositionTests` 9/9, `TenantQueryGatewayTests`
284/284, `TenantDetailSurfaceTests` 61/61, `SetTenantConfigurationFlowTests` 28/28, `RemoveTenantConfigurationFlowTests`
12/12, `DomainUiFluentConformanceTests` 51/51, `TenantConfigurationEndToEndTests` 1/1; full UI suite **1315/1315**;
Contracts 116/116, Client 50/50, Testing 181/181, Server 738/738; `dotnet build Hexalith.Tenants.slnx -c Release
--no-restore -warnaserror -m:1 -nr:false` 0 warnings / 0 errors. Tier 3 `IntegrationTests` not run in this pass.

### Story 1.6 trust-boundary re-review patch evidence (2026-07-28)

- [x] Literal principal evidence rejects a padded `sub` even when `IUserContextAccessor` exposes its normalized value;
  conflicting or whitespace-bearing tenant scopes resolve indeterminate before any global-administrator wildcard grant.
- [x] Policy-cache reads and reload invalidation share one synchronization boundary. Mutable-configuration coverage proves
  a cached subject grant is revoked on the next resolution after reload.
- [x] Policy diagnostics use source-generated `LoggerMessage` events (2100 Debug for indeterminate principal, 2101
  Warning for invalid deployment policy); captured messages expose only the failure category and the warning occurs once
  per invalid configuration load.
- [x] Embedding-host DI root precedence and global-administrator namespace authorization without explicit grants are
  pinned at the production composition seams.
- [x] Projection-proof policy exceptions return Unavailable without querying raw detail. Retained-detail authorization
  failures return a sanitized degraded snapshot, while cancellation propagates through initial composition and retained
  reauthorization.
- [x] Ten net-new cases moved the UI suite **1315 → 1325**. Package-mode Debug `dotnet test` passed 1325/1325; the
  Release UI build passed with 0 warnings / 0 errors; the Release xUnit v3 executable passed 1325/1325; and the Release
  `Hexalith.Tenants.slnx` build passed with 0 warnings / 0 errors. `git diff --check` passed.
- The story remains in `review` because this was the agreed narrowed trust-boundary chunk; UI
  composition/accessibility and broader test/evidence review groups remain follow-up work. The two Story 1.9 search
  findings remain tracked in `_bmad-output/implementation-artifacts/deferred-work.md`.

## Story 1.9 Authoritative Memories Search Evidence Addendum (2026-07-26)

Re-derived from the amended Story 1.9 spec after commit `a6f5801` rolled back the previous
review-repair delta.

- [x] `TenantQueryGatewayTests` and `TenantSearchCursorTests` cover exact Memories requests and
  response invariants; candidate parsing/deduplication; raw-hit accounting without backfill;
  authorization hydration; status recheck; every sort direction; pending/freshness truth; bounded
  cancellation-aware concurrency asserted against the production limit constant; cross-user,
  wrong-scope, invalid, index-shrink (including the equality boundary), and every-codec-exception
  recovery on both decode and encode; search failure families; ordinary fallback; cursor
  invalidation surviving a combined outage; and reason-code-only support-safe diagnostics.
- [x] The raw-page count invariant is an **upper bound only**: a short non-final index page stays
  authoritative and advances by the requested window bounded to the reported total, so consecutive
  pages neither repeat nor skip a candidate; only over-full or total-overflowing pages are rejected.
  Positive short-page, short-page-sequence, and short-final-page coverage is observed passing.
- [x] Codec-failure containment is two disjoint enumerated sets. The surfacing set
  (`OutOfMemoryException`, `NullReferenceException`, `ObjectDisposedException`,
  `ArgumentNullException`) is excluded before any base-type match, because `ObjectDisposedException`
  derives from `InvalidOperationException` and `ArgumentNullException` from `ArgumentException`.
  Seven contained types and four surfacing types are each covered on both the decode and encode
  paths.
- [x] The support-safe degradation signal is emitted only on a load that actually resolved to the
  ordinary list, never from the decode catch whose forced page-zero retry then succeeds
  authoritatively, and never twice for one load: every failure path records a reason code and funnels
  through a single fallback call.
- [ ] ~~Cursor invalidation landing on a terminal error/unauthorized surface withholds the clearing
  together with its notice and delivers both on the next renderable load.~~ **False — retracted.** The
  shipped code clears on the load that reports it, which is the opposite. The correction is stated in
  the Evidence Correction section below; this line is left in place, unchecked and struck through, so the
  contradiction cannot be read as a live claim.
- [x] A malformed member collection raises the identical `IsDegraded` / `RowEnrichmentUnavailable`
  signal on the search surface and the ordinary list, carried by a distinct enrichment-degraded flag
  that cannot trigger the ordinary-list fallback.
- [x] `TenantListSurfaceTests`, `TenantDetailSurfaceTests`, `TenantWorkspaceStateTests`,
  `TenantsWorkspaceTests`, `TenantsUiCompositionTests`, `LocalizerDoubleParityTests`, and
  `DomainUiFluentConformanceTests` cover authoritative and fallback Next/Previous,
  authoritative/fallback boundary reconciliation resolved before the outgoing request is built,
  crossing detection after a tenant-detail return recreates the component, the active paging mode
  held in the circuit-scoped paging service, server-only protected state, page-two identity reset,
  detail-return continuity and missing-retention recovery, prerender suppression of retained-paging
  restoration, pending recovery notices surviving a superseding same-scope load, rapid-load
  cancellation, sparse/partial/empty/fallback states, the rendered `SearchPageEmpty` surface with its
  stable test id and both its non-final and final messages, both notice bars refusing unmapped
  reasons, a shared polite live region that pre-exists its content, exact EN/FR copy for every new
  key in an explicitly resolved `fr` culture plus a gate (with a self-test proving it can fail) that
  every stubbed localizer value equals the shipped `TenantsResources.resx` value, stable selectors,
  Fluent controls, accessibility hooks confined to rendered markup, responsive rules,
  provider-resolved host-purpose isolation, and control-client-backed proof that default Memories
  HTTP logging is suppressed in both host compositions.
- [x] Support-safety evidence is placed where disclosure is possible — rendered markup, the canonical
  URL, JS-interop invocations, and the log sink — each with a control case in which the material
  genuinely appears. Diagnostic surfaces are pinned by equality; no `ToString()` substring check is
  offered as support-safety evidence.
- Story 1.9 acceptance criteria: all automated portions are covered. Authenticated AppHost/Memories
  runtime, responsive browser, and human NVDA evidence remain open with owner, consequence, and
  reopen trigger in the dated Story 1.9 evidence report.
- Validation **as of the 2026-07-26 pass-2 record, superseded** — retained as history only: Release UI
  test-project build passed with 0 warnings / 0 errors; the exact seven-class focused executable passed
  396/396; `TenantDetailSurfaceTests` passed 56/56; `LocalizerDoubleParityTests` passed 2/2; the full UI
  executable passed 1145/1145; `MemoriesSearchIndexEventPublisherTests` passed 7/7 after a warning-clean
  sample-test build; the Release solution build passed with 0 warnings / 0 errors; and `git diff --check`
  passed. For the current totals see the 2026-07-27 pass-3 entry at the end of this section.

## Story 1.9 Evidence Correction (2026-07-27)

The 2026-07-26 addendum above overstated three things and is corrected here. It claimed cursor
invalidation on a terminal surface "withholds the clearing together with its notice and delivers both
on the next renderable load"; the shipped code clears on the load that reports it and says so
explicitly, and no deferral mechanism exists. It claimed no `ToString()` substring check was offered
as support-safety evidence; seven such checks were live in the UI test project, four of them against
classes with no `ToString` override, and all are now pinned by equality behind a scanner that matches
same-statement and stringified-local spellings. It described a `SearchPageEmpty` copy that splits on
`HasMore`; that split is gone, because an authoritative window yielding no authorized row now ends
paging and both causes must render identically.

- Validation at `d59dd59` (**superseded** — four commits behind the branch tip when written): UI
  1,222/1,222; focused seven-class lane 452/452; Contracts 114/114; Sample index-handoff 7/7;
  `Hexalith.Tenants.slnx` Release build 0 warnings / 0 errors.

## Story 1.9 Code Review Pass 3 (2026-07-27)

A third four-layer review over the pass-2 repair delta produced 32 merged findings: two decisions
resolved by the story owner, 26 patches applied, one deferred, five dismissed. The behavioural changes
are the window-collapse rule (paging now ends only when every hydrated candidate was hidden or absent,
not on any empty window), a distinct `SearchAndListUnavailable` notice for terminal fallback surfaces, a
reported rather than silently dropped over-length search term, Next enablement tied to the paging cursor,
and a history cap that keeps page one reachable. The reset control on the empty-search surface was wired
— it had been rendered with an unset `EventCallback` and did nothing.

Six new guards were mutation-verified rather than assumed: the wired reset button, the mid-load pager
guard, Next enablement, the window-collapse rule, the pending recovery notice surviving disposal, and the
prerender guard on Previous. Two existing guards were rebuilt because they could not fail: the
support-safety scanner's stringified-local rule had no planted-failure case and was evaded by five
spellings, and the "components never call Memories" scan was blind to the `IServiceProvider` resolution
the audited component itself uses.

- Validation on the pass-3 working tree (on top of `5fdbc80`): UI **1,256/1,256**; focused seven-class
  lane **479/479**; Contracts **114/114**; Sample **39/39**; `Hexalith.Tenants.slnx` Release build
  **0 warnings / 0 errors**; `git diff --check` clean.
- Earlier figures in this file, in `spec-1-9-…-paging.md`, and in the withdrawn 2026-07-26 evidence
  report are historical records of earlier revisions and are marked as superseded where they appear.
  Superseded in turn by the backlog closure below.

## Story 1.9 Backlog Closure (2026-07-27)

The seven pass-2 patch findings that had been left unchecked are applied, so Story 1.9 has no open
review item. All seven were test-efficacy rather than behaviour: assertions that could not fail, guards
that were never reached, and a gate that silently skipped what it could not construct.

Each fix is mutation-verified — a defect was planted in the code the assertion claims to guard, the
strengthened assertion was shown to fail on it, and the plant was reverted. In four cases the prior
assertion was additionally shown to pass over the same plant, which is the sense in which it certified
nothing: the crossing test's cursor check, the secondary notice bar's own guards, the pending-recovery
scope binding, and the standalone-host codec assertions.

The one production change is `src/Hexalith.Tenants.UI/Program.cs`, which now composes
`AddHexalithTenantsUiModule` instead of hand-duplicating its registrations. The duplication is what let
the standalone host's search-cursor purpose, circuit-scoped paging state and Memories log suppression
drift from the module copy that was under test. The registrations were line-for-line identical, so
composition is unchanged.

Pinning the localizer gate's controls surfaced two further defects, fixed in the same session: the
control double meant to prove the French-parity rule was in fact being rejected by the neutral-bundle
rule (a key absent from the neutral bundle short-circuited the French check), and another control read
its "shipped" value back from the same `ResourceManager` the gate compares it against.

- Validation: UI **1,266/1,266**; focused seven-class lane **486/486**; Sample index-handoff
  **7/7**; UI Release build **0 warnings / 0 errors**; `Hexalith.Tenants.slnx` Release build
  **0 warnings / 0 errors**; `git diff --check` clean. These are the authoritative totals for Story 1.9.
- Counts rose 1,256 → 1,266 and 479 → 486 through the new scope-binding discrimination test, the
  reachable secondary-notice-bar theory, the localizer discovery test, and additional theory rows.
- **Gates were run with `-p:HexalithEventStoreVersion=3.82.0`.** `references/Hexalith.Builds` at gitlink
  `0e464b5` pins `HexalithEventStoreVersion = 999.1.20-proof.fa2d1c9910f8`, which is unpublished, so
  package-mode restore fails `NU1102` repository-wide; Release with source references is blocked
  independently by `references/Hexalith.Memories/Directory.Build.props:95`. The pin arrived with the
  `fix/release-stale-source-guard` merge (`8e84bf1`) and is not owned by Story 1.9 — a bare
  `dotnet restore` of any project fails identically. Recorded as `BUILDS-EVENTSTORE-PIN` in the dated
  Story 1.9 evidence report.

## Story 1.10 — Direct Tenants Reads and Authoritative Freshness (2026-07-28)

The Tenants UI query side now uses six typed server-side REST GETs and contains no generic EventStore
query submission/router symbols. `Tenants:BaseAddress` is independent from the unchanged EventStore
command/status dependency. Direct-client and gateway coverage pins exact paths/query fields, URI and
dot-only escaping, real enabled/disabled bearer relay, 200-only payload handling, exact-validator 304,
metadata contradictions, empty/auth/not-found/transport states, bounded ETags, paging shape, body/header
exceptions, cancellation, first-load truth, matching retained refresh data, and unchanged Memories
hydration.

Tenant member rows and paging are backed only by the dedicated tenant-users snapshot. Component tests
cover disjoint detail/member data, projection-version action gating, visible-page labeling, Next/Previous
history, page-one recovery, duplicate suppression, route and overlapping-load generations, retained rows
during notification refresh, exact producer/subscriber notification pairs, lease reference counting and
late disposal, and no unauthorized global-administrator subscription. EN/FR, accessibility, safe DOM,
diagnostic, and command/status regressions are included in the full UI gate.

The full `IntegrationTests` lane — never run by the earlier record, which filtered to the generated
controller class — caught two real breaks in the hosted global-administrators route, both now fixed. The
route answered HTTP 500 because a story-added `[Authorize]` attribute was the module's only endpoint
authorization metadata, and the host registers an authentication scheme only when OIDC is configured;
platform authority is now the page's rendered fail-closed state, guarded in the fast lane by
`Routable_components_fail_closed_in_page_without_endpoint_authorization_metadata`. The restricted branch
had also dropped the `tenants-global-admins-area` container and the live region it published before this
story, and now nests inside both. A superseded `AspireTopologyTests` audit assertion was moved to the
first-load error truth that review repair loop 1 introduced.

Every result below was produced after restoring the four `references/` gitlinks that had drifted inside
the story range back to `baseline_commit`.

- UI: `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore` —
  **1,416/1,416 passed** after a serialized project restore.
- PLAT-FRESH-1: `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~TenantsApiGeneratedControllerTests`
  — **26/26 passed** after a serialized project restore.
- Integration (Tier 3, `Category!=Performance`): **167/167 passed**, including the hosted UI route smoke
  and Aspire topology suites.
- Regression lanes: Contracts **120/120**, Client **50/50**, Testing **181/181**, Server **738/738**,
  Sample **39/39**. Each needs its own serialized restore; the `IntegrationTests` graph and the
  package-mode graph cannot share `obj` state (`SOLUTION-GRAPH-1` below).
- Static: generic-query scan had no matches; invented `tenant-index:system` scan had no matches; the
  exact story gitlink validator exits 0 against the final tree; staged and unstaged `git diff --check`
  passed.
- Solution: `dotnet test Hexalith.Tenants.slnx --no-restore` — **2,711 passed, 1 skipped, 0 failed, 0
  warnings** (the skip is the `Category=Performance` test that only runs on the nightly schedule).
  `SOLUTION-GRAPH-1` did **not** reproduce and is no longer claimed as a blocker. It is an `obj`-state
  effect, not a property of the solution: the missing-EventStore-symbol build failure appears when a
  source-reference restore (anything pulling the AppHost graph, including `dotnet run` on a UI project)
  has rewritten `src/*/obj/project.assets.json` while evaluation expects package assets. Forcing a
  package-mode restore per project first (`dotnet restore <project> -m:1 -nr:false --force`) makes the
  subsequent solution restore a no-op and the solution lane passes.
- `HOST-REF-1` remains open: the unchanged transitional AppHost does not provide the Tenants service
  reference/`Tenants:BaseAddress`, so no authenticated live-host REST proof is claimed. See the dated
  Story 1.10 evidence report for exact producer-pair, route, metadata, host, and build-graph evidence.
