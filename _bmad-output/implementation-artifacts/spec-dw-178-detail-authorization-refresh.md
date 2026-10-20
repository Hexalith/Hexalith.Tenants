---
title: 'DW-178: Explain and qualify same-tenant authorization refresh'
type: 'refactor'
created: '2026-10-20'
status: 'in-progress'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '54bfac8da66b60aa8aa19a77d23676a7662113b4'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-178 questions repeated lifecycle authorization resolution when the tenant route stays the same. The detail page reloads its projection evidence on every parameter update, and an unchanged tenant or caller identifier does not establish that the BFF authorization decision is unchanged.

**Approach:** Take the ledger's documented-rationale option: explain why each eligible parameter load obtains fresh authority, preserving pending fail-closed behavior, generation/cancellation protection and independent submit-time reauthorization. Add rendered regressions for same-tenant repeated/query-only updates, changed authorization, pending decisions and supersession. Qualify only this recommendation; retain unrelated work and dependency ownership. Commit and push the completed change on main as requested.

</frozen-after-approval>

## Implementation Notes

- The worktree was clean on main at the full baseline above. This freeform maintenance has no sprint story key. The change is small: a source comment, a partial test file, the existing test class declaration, and maintenance evidence. No unresolved intent choice, new public API, migration or unrequested external action is needed; use the oneshot route.
- Read baseline/Git/UX guidance, editor/build/test configuration, architecture AD-7/8/11, the ledger and maintenance index. Independent investigation confirms unconditional parameter-load resolution and separate authentication/command-time resolution. Preserve current behavior rather than add a cache whose validity cannot be established from tenant/caller identity alone.
- Code map: `TenantDetailPage.razor` OnParametersSetAsync reloads all data and calls StartLifecycleAuthorizationResolution; that method sets Indeterminate and replaces the generation/CTS. ResolveLifecycleAuthorizationAsync rejects late generations. `TenantDetailSurfaceTests.cs` supplies RegisterServices, ready caller-bound snapshots and StubTenantsBffComposition with controllable resolution gates; reuse them from a partial file.
- Validate rendered lifecycle button availability while held authority is pending, after revoked/indeterminate/granted completion, and after an obsolete same-tenant result completes. Show that changing return context still refreshes authority. Avoid private-field assertions and arbitrary sleeps in new cases.
- Initial ordinary Debug source build is blocked by existing CS0246 NativeSourceProof at TenantReadAuthorityResult.cs:12. The required isolated Aspire baseline starts and eventually reports all 23 resources Running/Healthy; stop the started app after inspection. Preserve this initial failure and any later environment changes explicitly.

- The initial extra same-tenant late-completion test did not reject a generation-removal control. A direct-substitute experiment also lacked required high-impact composition, producing a fixture null fault. Remove that unproven extra test and its experimental fixture hook; preserve the existing pending/superseded/authentication/submit-time regressions. The maintained new lane has six repeated/query-only authority cases. Do not claim new deterministic late-completion proof.

- Concurrent external commit `a83021d9efa259c1afa014078d2c5fcf61c4d42c` captured the initial DW-178 draft files and six dependency pointer advances during qualification. Preserve that commit; this session owns only the completion corrections. The full source build now uses those committed updated source pins. Record all inherited pointer moves below so the true entry baseline remains auditable; no dependency update command was run by this session.

## File List

- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` — document fresh per-load authorization rationale.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs` — make the existing fixture partial; no experimental hook remains.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.AuthorizationRefresh.cs` — six rendered repeated/query-only refresh cases.
- `_bmad-output/implementation-artifacts/spec-dw-178-detail-authorization-refresh.md` — workflow state, scope and inherited pointer attribution.
- `_bmad-output/implementation-artifacts/dw-178-validation-evidence.md` — commands, failed experiments and accepted evidence.
- `_bmad-output/implementation-artifacts/deferred-work.md` — preserve original DW-178 history and reconcile only this recommendation after acceptance.
- `docs/maintenance-backlog.md` — reconcile the matching remaining-work row after acceptance.
- `references/Hexalith.Builds`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.McpCli`
- `references/Hexalith.Memories`
- `references/Hexalith.Platform`

## Completion Notes List

The concurrent external sync commit above included the in-progress story files and these dependency moves. This table records their actual targets for range validation; it does not attribute the updates to this session or establish broader dependency/release qualification.

| Path | Entry pointer | Inherited target |
| --- | --- | --- |
| `references/Hexalith.Builds` | `5d03f1db9c38db34ecac7fe6048937478ffb82cc` | `fff4a212180d8cd1c5c0759655a9bc4fb6963c45` |
| `references/Hexalith.EventStore` | `f6b545cd02632f662136d0b4b8e6952baa7afec3` | `a07265f00994a3ad093184cec0f329d11184c3e9` |
| `references/Hexalith.FrontComposer` | `a706a8ae261268f839feb743e930e5311bb2a95b` | `1a1cd1e89f59b05ed2621a31f157f9d940356ef8` |
| `references/Hexalith.McpCli` | `040fd406d71723772fb14cb3a3bac9434003b6a4` | `9354dc08176744c75c2ddb61d924c0948b373b98` |
| `references/Hexalith.Memories` | `c082f8151328b1ac91939cda8733ab78ae68e1c4` | `35df7e2080c369003cacac12f5b44117d7f3ed35` |
| `references/Hexalith.Platform` | `11893ca9fd85644cab8a0414b2076bf346f108f6` | `642b5bdbdf33ec1588eeef3b9a7e9f1a02aebc40` |
