# Story 1.8 — Support-Safe Identifier Copy and Read-Experience Evidence (2026-07-21)

Reverification and hardening of support-safe copy on the revised Stories 1.2–1.8 read experience.
This report supersedes the completion claims and test counts in the preserved historical artifact
`1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md`; it does not rewrite that history.

## Environment and immutable source pins

| Item | Value |
|---|---|
| Root baseline revision | `c0451deaa02a67c852a5222b75a3564795460d0b` (`main`) |
| .NET SDK | `10.0.302`, `rollForward=latestPatch`, target `net10.0` |
| Fluent UI Blazor | `5.0.0-rc.4-26180.1` |
| FrontComposer | package baseline `4.0.1`; source submodule `7870526090a8596082e3df034ecacf4c07881a04` |
| Hexalith.Builds | `513b9bd66f8a16a109a0459ccad0d1424d2b1edd` |
| Hexalith.EventStore | `9b1fd9584362acf5d31c22375a7998ad06b0524f` |
| Hexalith.Memories | `2411c03c497133f48ec4ad42be9b333f8fc157c4` |
| AI.Tools / Commons / PolymorphicSerializations | `991e8ea1b39bfb8170aea9a6857c25c7c69176c1` / `ea1fc4551dcaf8ee63fd562d77dfe0f18c57a94c` / `a5dd24f5e66324d18241de7d5521ee124eab4877` |

No dependency, package, submodule, backend/public contract, AppHost, DAPR, or `references/` change was
made. At implementation start the only worktree entry was the untracked controlling Story 1.8 spec.

## Implemented contract

- `SupportSafeCopyValueKind.Unknown = 0` and `SupportSafeCopyEligibility.Unsafe = 0` make defaults
  fail closed; the public enums now live in their own documented files.
- Eligibility requires a non-whitespace literal, one of the five explicitly known non-default kinds,
  explicit outer-surface approval, and a non-whitespace accessible name. Undefined/future enum values
  fail closed. Approved literals are never parsed, trimmed, case-normalized, decoded, reformatted, or
  rejected by content or length deny-lists.
- `SupportSafeCopyButton` renders no control, feedback channel, or clipboard path for unapproved,
  empty, or unknown-kind inputs. Approved outcomes share one `role=status`, `aria-live=polite`,
  `aria-atomic=true` result channel. Each activation captures an input version before awaiting,
  aborts before write when identity/approval changes, suppresses stale post-write feedback, clears and
  republishes identical outcomes, rejects overlapping activation, and maps cancellation to localized
  non-success recovery. Only resolved clipboard writes announce `Copied.`; every failure uses
  localized safe manual-copy recovery without values or exception details.
- Authorized tenant list, tenant detail, My Tenants, user membership lookup, and member projections
  pass explicit approval. Outer audit and global-administrator page tests now activate safe rendered
  controls and assert exact clipboard arguments; audit grid tests also prove an unsafe raw event
  reference has no copy control.
- Configuration copy controls are absent because the revised Story 1.6 positive BFF safe model is
  not implemented at this baseline. The isolated legacy display sanitizer continues to show safe
  baseline values and redact unsafe values in list/set/remove previews, but it never grants clipboard
  approval. No raw configuration dictionary entry is approved for copy.

## Exact automated verification

All commands ran from the repository root. The xUnit v3 in-process executable is the repository's
documented .NET 10 Microsoft.Testing.Platform fallback.

| Command | Result |
|---|---|
| `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -warnaserror -m:1 -nr:false` | Passed — **0 warnings, 0 errors** |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Components.SupportSafeCopyButtonTests -class Hexalith.Tenants.UI.Tests.Components.TenantListSurfaceTests -class Hexalith.Tenants.UI.Tests.Components.TenantDetailSurfaceTests -class Hexalith.Tenants.UI.Tests.Components.MyTenantsSurfaceTests -class Hexalith.Tenants.UI.Tests.Components.UserMembershipLookupSurfaceTests -class Hexalith.Tenants.UI.Tests.DomainUiFluentConformanceTests -class Hexalith.Tenants.UI.Tests.TenantsUiCompositionTests` | Passed — **241 total, 0 failed, 0 skipped** |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` | Passed — **976 total, 0 failed, 0 skipped** |
| `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -warnaserror -m:1 -nr:false` | Passed — **0 warnings, 0 errors** |
| `git diff --check` | Passed — no whitespace errors |

The first red-phase build failed as intended because `SupportSafeCopyValueKind.Unknown` did not yet
exist. After review hardening, the component class passed 43 tests; the final totals above are the
authoritative current-source results.

## Acceptance-criterion verdict

| Criterion | Verdict | Current evidence |
|---|---|---|
| Approved literals copy exactly and focus stays on the trigger | **automated verified; browser focus blocked** | Component tests write exact GUID-/ULID-shaped, Unicode, confusable, reserved-character, significant-whitespace, deny-list-word, and 4,096-character literals. Outer list/detail/My Tenants/user/member tests activate their rendered controls and assert the exact JS argument. The single Fluent trigger and non-interactive status region preserve DOM focus semantics; real browser focus proof is blocked below. |
| Unapproved values create no affordance/state/announcement/interop/log/telemetry/serialization | **verified for current sources; configuration dependency blocked** | Direct component tests render empty markup and make zero JS calls for empty/unapproved values. Composition scans require `IsApproved=true` at every shared caller and prove configuration has no copy component. Source scans exclude logger/console, browser storage, serialization, backend calls, token access, legacy clipboard fallback, and browser telemetry APIs. Story 1.6 remains blocked below. |
| EN/FR true outcome, polite atomic feedback, safe recovery | **verified** | Component tests cover success, cancellation, insecure/missing API, permission denial, generic JS failure, and disconnected circuit, including repeat success/failure announcement publication. ResourceManager tests assert exact English/French strings for every outcome; failures contain neither the copied literal nor exception details. |
| Six read surfaces remain usable across widths and accessibility modes | **partial; completion blocked** | Automated component/source checks cover stable selectors, Fluent buttons, keyboard semantics, literal-preserving wrapping, focus-visible, forced-colors, and motion-independent behavior. Five surfaces have clipboard activation coverage and configuration correctly has no copy affordance pending Story 1.6. This criterion is not verified until authenticated responsive-browser and human NVDA evidence exists. |
| Every revised criterion and surface/channel has current evidence or an exact blocker | **verified by this report** | This dated matrix uses the current baseline and 976-test result; the June 144-test result and obsolete Server/AppHost failures are referenced only as historical claims in the superseded artifact. |

## Surface and channel matrix

| Surface | Authorized visible literal | Clipboard activation | Announcement/focus | Responsive/a11y | Support-safety channels |
|---|---|---|---|---|---|
| Tenant list (Stories 1.2/1.8) | `verified` — server-authorized `TenantListRow.TenantId`; `tenants-list-*` | `verified` — exact `tenant.alpha` JS argument | `component verified`; browser focus `blocked` | CSS/conformance `verified`; browser/AT `blocked` | DOM/source/storage/log/telemetry/serialization scans `verified` |
| Tenant detail (Stories 1.3/1.8) | `verified` — authorized detail projection; `tenants-detail-*` | `verified` — exact visible tenant literal | `component verified`; browser focus `blocked` | CSS/conformance `verified`; browser/AT `blocked` | DOM/source scans `verified` |
| My Tenants (Stories 1.4/1.8) | `verified` — authenticated self-membership projection; `tenants-my-*` | `verified` — exact row tenant literal | `component verified`; browser focus `blocked` | CSS/conformance `verified`; browser/AT `blocked` | No browser target id/token/backend path; source scans `verified` |
| User lookup (Stories 1.5/1.8) | `verified` — authorization-scoped result projection; `tenants-user-*` | `verified` — exact `tenant/%2F?x=é`, not decoded/alternate route text | `component verified`; browser focus `blocked` | CSS/conformance `verified`; browser/AT `blocked` | Hidden membership and browser client/storage guards `verified` |
| Configuration (Stories 1.6/1.8) | Story 1.6 positive model `blocked` | `blocked` by design — no copy affordance | No copy announcement/state `verified` | Existing CSS/conformance `verified`; browser/AT `blocked` | Raw dictionary is never approved for copy; dependency blocker below |
| Member review (Stories 1.7/1.8) | `verified` — authorized member projection; `tenants-member-*` | `verified` — exact `owner-user` JS argument | `component verified`; browser focus `blocked` | Row/reason/CSS conformance `verified`; browser/AT `blocked` | Existing fail-closed action reasons and no mutation success regression `verified` |

## External evidence blockers

- **CFG-1.6-SAFE-MODEL — owner: Tenant UI/BFF configuration-policy implementer — OPEN.** The
  revised Story 1.6 typed policy registry and positively approved component-facing safe model do not
  exist at baseline `c0451de`. Consequence: configuration key/value copy cannot be offered or certified;
  this story fails closed by omitting it. Reopen trigger: Story 1.6 is implemented and its focused
  `TenantDetailSurfaceTests` plus `TenantQueryGatewayTests` pass with an exact-key `DisplaySafe` model;
  then add configuration outer-surface clipboard activation to this matrix.
- **BROWSER-COPY-1.8 — owner: Tenant UI QA — OPEN.** No dated authenticated EN/FR browser session was
  available for this run. Consequence: real Clipboard API success/failure, focus before/after, clean
  browser console/logs, and layout at 320/375/430/768/1024/1366/1440 px are not claimed. Reopen trigger:
  run the repository AppHost with authenticated authorized fixtures, exercise all available copy
  controls plus denied/insecure outcomes in EN and FR, and retain dated screenshots/traces/console logs
  that record the exact viewport and focus locator before and after each action.
- **AT-NVDA-1.8 — owner: Accessibility QA — OPEN.** No dated human NVDA plus supported-browser session
  exists in the repository. Consequence: bUnit ARIA/source checks cannot certify spoken announcement
  order or real screen-reader focus retention. Reopen trigger: a human tester records browser/NVDA
  versions, keyboard steps, spoken success/failure feedback, trigger focus before/after, and results for
  list, detail, My Tenants, user lookup, configuration availability, and member review.

No unavailable proof was inferred from historical evidence, bUnit, CSS source, or a different story's
browser artifacts.
