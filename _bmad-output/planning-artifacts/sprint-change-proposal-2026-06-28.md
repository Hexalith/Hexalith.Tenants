# Sprint Change Proposal - 2026-06-28

Approval: approved by Administrator on 2026-06-28 (Correct Course — "Apply pre-approved syncs now").

Scope classification: **Minor** (build/test verification + documentation sync of an already-approved change). No code, contract, or behavior change.

## 1. Issue Summary

Trigger: direct user request via Correct Course — *"projects must be built if any changes have been done to their files. check projects, solutions and documentation."*

The repository working tree and all `references/` submodules are clean and pushed; the "changes" are the recently committed work (commits `ad7312b`…`a789a42`), dominated by the **cc-2026-06-27 Tenants tabbed-workspace + single-nav-entry** correction.

Two problems were investigated:

1. **Build/test integrity** of the committed state had not been re-verified in this session.
2. **Documentation consistency** after cc-2026-06-27 was unverified.

Finding: build and all test tiers are green, but the cc-2026-06-27 change shipped in **code** (and is marked `done` in `sprint-status.yaml`) while several planning/UX artifacts the *approved cc-2026-06-27 proposal explicitly required updating* still described the **superseded** "three primary nav areas / Users contextual" model. The 2026-06-27 doc commit (`cdeb36b`) only touched FC-TBL/Memories-search wording — it never updated the information-architecture/navigation sections.

## 2. Verification Evidence

### Build

- `dotnet restore Hexalith.Tenants.slnx` — up-to-date.
- `dotnet build Hexalith.Tenants.slnx -c Release -warnaserror` (full solution incl. submodule deps, 5 publishable packages, host, AppHost, UI, tests) — **Build succeeded, 0 Warning(s), 0 Error(s)**.

### Tests (all tiers green — 2115 passing, 0 failing)

| Project | Tier | Result |
|---|---|---|
| Hexalith.Tenants.Contracts.Tests | 1 | 106 ✅ |
| Hexalith.Tenants.Client.Tests | 1 | 48 ✅ |
| Hexalith.Tenants.Testing.Tests | 1 | 181 ✅ |
| Hexalith.Tenants.Sample.Tests | 1 | 39 ✅ |
| Hexalith.Tenants.UI.Tests | 1 (bUnit) | 777 ✅ |
| Hexalith.Tenants.Server.Tests | 2 (dapr) | 738 ✅ |
| Hexalith.Tenants.IntegrationTests (`Category!=Performance`) | 3 (Aspire + Testcontainers) | 226 ✅ |
| **Total** | | **2115 ✅ / 0 ❌** |

(`Category=Performance` is nightly-only and out of scope for this verification.)

### Code-vs-spec spot checks

- `TenantsFrontComposerRegistration.RegisterDomain` contributes exactly **one** `AddNavEntry` → `/tenants` (matches cc-2026-06-27 AC "one Tenants module entry").
- `TenantsWorkspace.razor` hosts page-local tabs; `MyTenantsPanel` / `UserMembershipLookupPanel` provide the tab bodies (an implementation-naming refinement of the proposal's sketch `UserMembershipWorkspaceTab`; the proposal expressly left exact composition to existing FrontComposer/Fluent patterns — no action needed).

## 3. Impact Analysis

- **Epic Impact:** Epic 1 (read surfaces) navigation reconciliation; Epic 4 (Global Administrators) and Epic 5 (Audit) at the navigation layer only — their surfaces stay reachable through module-internal/contextual paths, not separate Tenants left-menu entries.
- **Artifact Conflicts (documentation drift, now fixed):**
  - PRD `prd-tenants-2026-06-02/prd.md` — §5.1 Operations Shell + the Operations Shell glossary entry.
  - UX `EXPERIENCE.md` — Information Architecture prose (UX-DR20 equivalent) + IA surface table "Reached from" cells.
  - `epics.md` — UX-DR20 statement + Story 1.5, Epic 4, and Epic 5 navigation acceptance criteria.
  - `docs/tenants-ui-operations-shell-spec.md` — supersession note + two user-lookup reachability bullets.
- **Technical Impact:** none — documentation only; no code, contract, package, or test change.

## 4. Recommended Approach

**Direct Adjustment** — apply the documentation sync the approved cc-2026-06-27 proposal already specified, bringing the artifacts in line with the shipped + tested code. No new product decision: Users stays **lookup/search-backed (Option A)**; an exhaustive all-users inventory remains a separate, deferred backend/Product decision.

## 5. Detailed Change Proposals (applied this session)

All edits change documentation only; the verbatim old→new IA text follows the approved cc-2026-06-27 proposal, retaining still-valid invariants (command lifecycle never navigation; context preservation across tabs/surfaces).

1. **PRD §5.1 Operations Shell** — replaced "three primary nav areas / Users contextual" with the one-entry-per-module tabbed-workspace model (Tenants tab + lookup-backed Users tab; Global Administrators/Audit module-internal/contextual). Dated supersession note added.
2. **PRD glossary — Operations Shell** — rewritten to the one-entry-per-module + tabbed-workspace definition.
3. **EXPERIENCE.md — Information Architecture** prose rewritten; surface-table "Reached from" cells updated for Tenant list, My Tenants, User lookup, Global Administrators, Audit.
4. **epics.md** — UX-DR20 rewritten; Story 1.5 (`:604`), Epic 4 (`:1204-1205`), and Epic 5 (`:1449`) nav acceptance criteria reconciled to the tab model with the Users-stays-lookup-backed honesty constraint.
5. **docs/tenants-ui-operations-shell-spec.md** — 2026-06-06 supersession block extended with a 2026-06-27 note; two user-lookup reachability bullets updated.

## 6. Implementation Handoff

Scope: **Minor** — implemented directly during this Correct Course run.

Remaining handoff: a Developer agent commits the four changed documentation files on a `docs/…` branch (Conventional Commits `docs:` → no version bump / no NuGet publish), e.g. `docs(planning): sync IA artifacts with shipped cc-2026-06-27 tabbed workspace`.

Deferred (unchanged): Option B — a true exhaustive all-users membership list — still requires a new authorization-scoped backend read query before any UI presents it as complete.

## 7. Approval State

Proposal status: approved and applied. Approved by: Administrator. Approved on: 2026-06-28.
