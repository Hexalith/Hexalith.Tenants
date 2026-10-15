# DW-105/DW-108 validation evidence — 2026-10-14

**Only DW-105/DW-108 close in the validated working tree. Release remains not ready.**

## Ownership, entry and contract

The owning repository is Hexalith.Tenants, on `main` at full entry/current HEAD
`51f404513f2657e98f949f58f0df43b99b68a7d0`. EventStore remains on `main` at external
checkout `241652627a52e47c189b2b052db407cec172f813`, with recorded Tenants pointer
`24d719292b06d8a9d09771d837f8dd4d8d9b8997`. These inherited revisions and DW-517
source/evidence changes belong to their original work. No dependency movement is
attributed to this bundle. [Entry capture](dw-105-108-entry-state.json) records
all root dependency branches, full HEADs, remotes/history, indexes, working state,
recorded/checked-out pointers and preservation hashes. No nested submodule was
initialized; all remain in their captured state.

AGENTS.md, the permitted root-declared AI.Tools baseline and its UX/state/Git
instructions, tracked configuration, owning architecture, Story 1.4 and its
review/evidence, Story 1.8 specification/review/evidence and current helpers were
read. The allowed repository-local bmad-build renderer ran exactly once. Its
small-change route and independent Blind Hunter review were followed; the user's
explicit preservation/no-commit instructions override dirty-tree/commit steps.
No skill was discovered or loaded from references/.

The [pre-implementation contract and complete triage](spec-dw-105-108-dom-safe-tenant-identity.md)
distinguish exact ordinal display identity from existing supported tenant routes.
Story 1.8 permits exact authorized display/copy literals, including whitespace and
Unicode. Existing IsSafeTenantId/detail/audit rules admit bounded tenant slugs;
encoding grants no new route, authority, command or persistence permission.
Unsupported route rows remain visible/copyable under existing approval without
detail/audit links. Blank/malformed inputs render safely without an anchor.

## Change and regression proof

Both findings were reproduced in inspected current source before implementation:
raw DOM IDs and anchors in both grids/navigation, plus throwing blank detail
navigation. TenantDomIdentity now encodes exact well-formed nonblank literals as
strict UTF-8, unpadded Base64url with an id- marker. It never trims, recases,
normalizes, hashes or truncates an identifier. Both grid prefixes, membership
lookup, detail links, audit anchors, MyTenantsPanel's two return branches and
workspace anchor comparison use that representation. ItemKey, route identity,
authorization and commands retain the original logical tenant ID.

Focus validation retains the existing512-character DOM bound, logical256-character
identifier/safety rule, origin-prefix checks and legacy safe-focus compatibility.
Long encoded tokens must decode to an owning-rule-approved identifier and
reencode canonically. Display encoding itself has no new length/format policy.
Selected tenant query state preserves significant whitespace while keeping its
existing control/length rejection. All-tenants ordinary detail retains the
approved first-page/heading behavior; My Tenants detail restores its link and
audit restores the launcher inside the correct originating row.

32 new cases exercise real Fluent grid/panel/page rendering, stable rerendering,
distinct punctuation/space/case/Unicode-composition/encoded-looking identities,
both grids and tenants-my/tenants-user prefixes, blank/null/malformed Unicode,
route denials, exact selections, rendered navigation anchors, actual page focus
interop, lookup subject binding and encoded payload/boundary rejection. They are
included in final focused/full runs. Existing literal clipboard checks remain.

Independent review produced seven findings, all individually triaged and patched:
plain-identity whitespace styling, lookup fixture parameters, subject-bound focus,
all-tenants heading proof, both actual MyTenantsPanel exports, exact ordinal text
collections and malformed/boundary vectors. Subsequent independent83-case
correction and final decoded-rule rechecks pass, with no unresolved findings.
Initial independent80-case result and safe exact stdout are retained in
[command evidence](dw-105-108-command-results.json).

## Executed validation and initial failures

[Command evidence](dw-105-108-command-results.json) contains exact argv, cwd,
exit, duration and safe output; [full UI counters](dw-105-108-full-ui-summary.json)
retain XML-derived failures/skips. Final source/artifact hashes and preservation
checks are recorded in [preservation evidence](dw-105-108-preservation.json).

| Lane | Result |
| --- | --- |
| Debug UI build, source references, -warnaserror -m:1 -nr:false | Exit0; zero compiler errors; existing NU1903 audit warnings retained |
| Maintained Release UI build, package defaults plus existing FrontComposer source exception | Exit0; zero compiler errors; existing NU1903 audit warnings retained |
| Six existing UI/navigation classes |519/519 passed; zero failures/skips |
| Final independent identity/navigation/query selector |83/83 passed; zero failures/skips |
| Final unfiltered Debug UI |4971/4971 passed; zero errors/failures/skips/not-run |
| Final unfiltered maintained Release UI |4971/4971 passed; zero errors/failures/skips/not-run |
| Maintained Chromium runner, Debug and Release |Exit0 in both; Google Chrome154.0.8037.57 |
| Shell syntax and git diff --check |Exit0; expected CRLF normalization notices retained |

Chromium loads the actual exported component markup, restored Fluent module,
shipped unchanged tenantsFocus.js and actual scoped grid styles. It checks33
distinct exotic anchors and whitespace/wrapping,12 audit returns, four detail
returns through actual standalone/workspace MyTenantsPanel branches, actual
all-tenants heading focus and lookup cancellation after a subject change. Every
successful restoration checks document.activeElement against the exact element.
The wrong-row focus control fails as required. Existing focus/style/forced-colors,
localized correction/removal and mutation controls remain enabled and pass.

Initial failures remain visible: three new test builds failed on bUnit interface,
imports/nullability and context-accessor setup; initial29-case runs failed7 and4
cases on fixture initialization/wrappers/interop expectations. Initial519-case
run failed14 stale anchor/unsupported-link expectations. Initial4968-case full
run failed three generated anchor expectations and the existing wedged-browser
scenario expectation after new work ran first. Fixtures were corrected and the
original shipped browser scenario remains first; no gate, assertion purpose,
selection, exclusion, floor or timeout was weakened. CRLF normalization preserves
text-normalized contents; final builds/full runs qualify the final source.

UI runs have no skipped cases. The preserved DW-517 Server25 named DW1 ATDD skips
were not rerun or reclassified by this UI bundle. No other test lane is described
as current execution without a recorded command.

## Remaining release blockers

The exact broad command `dotnet build Hexalith.Tenants.slnx --configuration Release
-warnaserror -m:1 -nr:false` exits1 on two NU1903 errors for the pinned
Microsoft.AspNetCore.SignalR.Common10.0.12 advisory. The diagnostic strict
package-only UI build, adding -p:HexalithFrontComposerFromSource=false, exits1
with seven existing projection-scope API errors in unchanged
TenantSummaryProjectionPage. Neither dependency versions nor audit/build policy
changed. Maintained UI source-exception success does not turn either result green.

B6/B8 story-gitlink commands still fail on five inherited movements. DW-517 and
this bundle's gitlink check fail on the inherited EventStore checkout movement.
All full identifiers/pointers are preserved, with no reset, baseline narrowing
or false ownership declaration. Existing raw Gitleaks/CI wiring, seven
authority/recovery/governance decisions, nested-layout validation, NFR10/performance,
hosted TLS/browser and production authority/revocation/provider qualification
holds remain. The isolated local Aspire start/describe observed running healthy
resources and the exact-target stop succeeded; this grants no provider acceptance.

Browser evidence uses rendered-component fixtures with substituted gateway and
composition services plus direct history/focus calls. It proves the owning DOM,
generated navigation and focus contracts. Authenticated deployed topology,
clipboard denial and human assistive-technology acceptance remain separate holds.

## Closure, preservation and next bundle

Only DW-105/DW-108 change disposition; their original findings, legacy text and
pre-bundle verification/actions remain. All643 IDs remain:321 done/322 open, with
287 index rows covering each open ID exactly once. Reconciled counts are117 done,
234 actionable,15 blocked,26 awaiting-decision,nine accepted limitations and38
uncertain. Unrelated ledger records, B1–B8, DW-517, archive, sprint and1371 inherited
historical artifacts are preserved. Indexes, repository/dependency HEADs, pointers,
configuration, gates and exclusions remain unchanged. No staging, commit, push,
branch, dependency update, deployment, production replay or data migration ran.

**Next selected bundle: DW-112**, align the authoritative-search status filter's
visible label and accessible name. Current TenantsWorkspace still uses
StatusFilterLabelKey for Label and Tenants.List.StatusFilterLabel for aria-label;
its ledger entry is open/actionable with no dependency or policy decision.
This selection and the two closures do not establish full release readiness.
