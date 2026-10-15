---
title: 'DW-105/DW-108: Shared DOM-safe tenant identity'
type: 'bugfix'
created: '2026-10-14'
status: 'done'
route: 'oneshot'
baseline_commit: '51f404513f2657e98f949f58f0df43b99b68a7d0'
review_loop_iteration: 0
---

<frozen-after-approval reason="user authorized this bounded implementation and preservation scope">

## Intent

Both grids interpolate literal tenant identities into DOM IDs and return-focus
anchors. Shared detail navigation can throw while rendering blank identifiers.
Give rendered rows, detail links and generated navigation one deterministic,
injective DOM representation, retaining exact logical identity and existing
authorization, route and command rules. Close only DW-105/DW-108 after rendering,
navigation, browser focus and independent review pass.

The owning architecture defines TenantId as an ordinal caller-supplied string.
Story 1.8 requires exact visible/copy literals, including significant whitespace,
Unicode and reserved characters. These display permissions do not expand route
permissions: TenantAuditNavigationSafety.IsSafeTenantId and TenantDetailPage
already restrict tenant routes to non-credential ASCII slugs of at most 256
characters. Unsupported route identities remain visible/copyable under existing
read approval, with no detail/audit link. Blank input never tears down a grid.
No normalization, sanitization, hash, GUID/ULID conversion or identifier limit is
introduced into display identity.

Encode every nonblank, well-formed identifier as strict UTF-8, unpadded Base64url
with an `id-` marker. Prefixes remain `tenant-row-`, `tenants-my-row-`,
`tenants-my-detail-` and `tenants-user-row-`; MyTenantsDataGrid uses the same token
for its row and detail IDs. Case, whitespace, composition and supplementary
Unicode stay distinct. Null/blank/malformed UTF-16 yields no DOM anchor. Fixed
surface prefixes are supplied by the owning components. This tenant-specific
helper does not replace shared platform infrastructure; inspection found no
equivalent stable tenant encoding helper in FrontComposer/Commons.

Keep the existing 512-character focus bound and legacy safe-focus compatibility.
Recognize canonical encoded tenant tokens so valid 256-character ASCII tenant
slugs still restore focus despite encoding expansion. Never decode a token into
a route, authorization identity or command. Navigation routes and selected IDs
use the original literal; query parsing must not trim selected tenant identity.
All-tenants detail return retains the approved first-page/heading behavior;
My Tenants detail returns to its detail link; audit returns to the launcher in
the originating row. Browser checks assert the actual active element.

Given exotic or blank authorized read rows, rendering must remain stable and
preserve exact text/copy behavior without unsupported navigation. Given distinct
identifiers (spaces, punctuation, Unicode, case, composition and encoded-looking
literals), rendering and rerendering must yield distinct stable IDs across both
grids and both membership prefixes. Given an admitted tenant slug, generated
detail/audit return anchors must match rendered elements, survive existing return
validation, and focus the correct launcher through the shipped JS module.

Preserve B1–B8, DW-517, all historical findings, unresolved decisions, archive,
sprint tracking and inherited working-tree changes. Record complete Git state,
exact validation commands, initial failures/skips and historical gitlink
provenance blockers. Keep gates/exclusions/dependency versions and pointers
unchanged. No staging, commits, push, deployment, replay, production data mutation
or nested-submodule initialization is authorized.

</frozen-after-approval>

## Implementation Notes

Pre-implementation investigation verified both findings in current source:
TenantListNavigationContext raw row anchors and throwing string overload;
TenantDataGrid raw identity/ReturnFocus; MyTenantsDataGrid raw row/detail IDs and
ReturnFocus; MyTenantsPanel raw standalone/workspace detail-return anchors;
TenantsWorkspace raw selection/anchor comparison. tenantsFocus.js uses
getElementById with origin containment and actual focus checks, so its behavior
can be reused. TenantAuditNavigationSafety owns safe focus, route and return URL
validation. TenantWorkspaceState owns bounded selection/query state. Existing
Story 1.4 review accepted Role-augmented grid and first-page/heading parity;
that decision is unchanged. Story 1.8 review explicitly deferred cross-grid
anchor safety, not identity normalization. No unresolved policy choice remains.

Entry capture: dw-105-108-entry-state.json records all root dependencies, full
HEADs, branches, remotes, history, index/working state, recorded/checked-out
pointers and preservation hashes. Tenants entry is the full baseline above;
EventStore checkout 241652627a52e47c189b2b052db407cec172f813 differs from recorded
24d719292b06d8a9d09771d837f8dd4d8d9b8997. Its DW-517 source/evidence changes and
untracked files are inherited. The dirty-tree skill checkpoint is already
authorized by the user's explicit preservation instructions. No attribution of
that work or pointer movement to this bundle is permitted.


### Implementation and review corrections

One tenant-specific helper now produces strict UTF-8 Base64url DOM tokens.
Both grids and their detail/audit navigation use it; workspace return-context
comparison uses the same anchor. Display/copy literals are untouched. Invalid
route rows render plain identity text without navigation; blank/malformed input
has no DOM identity. Selected tenant query values retain significant whitespace,
while existing control/length rejection remains. The unchanged JS module owns
origin containment, cancellation, correct launcher lookup and focus confirmation.

Initial test builds failed on a retired bUnit fragment interface, omitted test
imports and nullable assertions. Fixture execution then exposed premature service
provider initialization, bUnit wrapper reference comparisons and wrong interop
origin/argument expectations. These were corrected in test setup, preserving the
intended assertions. Initial six-class run:519 total/14 failed/0 skipped from old
anchor fixtures and unsupported-link expectations; corrected519/519 passed.
Initial full UI:4968 total/4 failed/0 skipped. Three generated anchor expectations
and the existing wedged-browser scenario expectation were corrected; the shipped
browser scenario again runs first. Intermediate full4968/4968 passed.

## Review Triage Log

Independent context-free Blind Hunter computed7 findings from38.121kB, and ran
80 focused tests with zero failures/skips. Every finding is retained below:

1. **Medium, patch:** Plain all-tenants identity lost link-owned whitespace and
   wrapping styling. Added scoped strong-literal rules; browser now loads actual
   scoped styles and checks computed break-spaces/anywhere for all33 exotic rows.
2. **Medium, patch:** Users fixture incorrectly supplied DetailHref, unlike the
   production lookup. Fixture now supplies none and asserts no detail links.
3. **Medium, patch:** Lookup fixture omitted TargetUserId/expectedUserId. Added
   exact subject to fixture and component interop proof; browser uses the real
   subject, checks targetUserId, and rejects focus after a subject change.
4. **Medium, patch:** All-tenants ordinary-detail heading return lacked browser
   proof. Added actual workspace render, exact captured heading interop, original
   selection/first-page assertions, rendered export and browser active heading.
5. **Medium, patch:** Browser My Tenants detail fixture bypassed MyTenantsPanel's
   standalone/workspace branches. Both actual panel renders now export and both
   branches restore focus to their exact rendered links in Chromium.
6. **Medium, patch:** Set-membership-only text checks could miss Unicode
   normalization into another supplied identity. Added complete ordinal equality
   of expected/rendered collections, preserving multiplicity and composition.
7. **Low, patch:** Encoded validation lacked malformed/noncanonical/boundary
   vectors. Added long tokens that force the decoder branch, invalid UTF-8,
   noncanonical padding bits, invalid Base64 length, forbidden padding, blank
   payload and exact512/513-character bounds. No gate or route rule is relaxed.

No finding is dropped or deferred. User instructions prohibit the skill's commit
step; no Git mutation is performed. Final independent correction recheck and
qualification results will be recorded in the bundle evidence.


Final gate audit tightened the extended encoded-token branch to reuse the owning
IsSafeIdentifier rule on the decoded literal, preserving its existing256-character
logical focus bound and safety checks as well as the512-character DOM bound.
DOM encoding itself remains unlimited/literal-preserving. The exact512/513 vectors
now use256-character BMP Unicode identifiers with373/374 UTF-8 bytes; a257-character
ASCII identity is rejected. This preserves existing gates rather than treating
encoding as permission for an unsupported focus identity. Final rechecks qualify
this revision; earlier passes remain intermediate evidence.


### Final qualification and closure

Final independent guard recheck passes83/83. Final Debug/source and maintained
Release/package-default builds exit0 with zero compiler errors and preserved
NU1903 warnings. Both final unfiltered UI runners pass4971/4971 with no errors,
failures, skips or not-run cases. Both final Chromium lanes pass all rendered
identity/navigation/focus, existing safety and negative controls. Complete
commands, retained initial failures, XML counters and source/artifact preservation
hashes are in dw-105-108-validation-evidence.md and its linked JSON records.

Only DW-105/DW-108 close; all643 IDs remain,321 done/322 open,287 exact-covering
index rows;117 reconciled done/234 actionable. Unrelated records, historical
B1–B8/DW-517/readiness/archive/sprint,1371 inherited artifacts, all branch/HEAD/
index/dependency states and gates are preserved. Broad NU1903, strict package-only
API/provenance and existing release/qualification/decision holds remain. Next
selected item is DW-112; release remains not ready. No Git or production mutation
ran. Isolated local AppHost was stopped through the exact target.
