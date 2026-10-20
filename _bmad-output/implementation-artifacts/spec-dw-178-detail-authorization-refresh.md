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
