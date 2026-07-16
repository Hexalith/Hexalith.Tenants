# Sprint Change Proposal — CI Build Mutualization (Phase 1)

- **Date:** 2026-06-30
- **Author:** Administrator
- **Trigger type:** Technical limitation / maintainability (CI gap analysis)
- **Scope classification:** Moderate (cross-repo: edits the shared `Hexalith.Builds` submodule)
- **Change mode:** Batch
- **Path forward:** Option 1 — Direct Adjustment (low effort, low risk)

## 1. Issue Summary

A gap analysis compared the CI build of `Hexalith.Builds` (the shared build template,
exposing reusable GitHub *composite actions* under `Github/`) with the CI build of this
repository (`Hexalith.Tenants`).

`Hexalith.Tenants` implements its CI entirely hand-rolled in two monolithic workflows
(`.github/workflows/ci.yml` and `release.yml`) and consumes **none** of the
`Hexalith.Builds` composite actions. This produces:

- **Duplication**: the Dapr install + `dapr init` block is copy-pasted **4 times**
  (`ci.yml` ×3 across the `build-and-test`, `aspire-tests`, `performance-tests` jobs;
  `release.yml` ×1).
- **No reuse** of the shared `Hexalith.Builds` tooling, contradicting the repository
  policy in `CLAUDE.md` ("Factor technical concerns into the relevant shared Hexalith
  modules… Hexalith.Builds").

Phase 1 addresses the clearest, lowest-risk duplication (the Dapr block) by factoring it
into a new shared composite action, and prepares `Hexalith.Builds` for broader reuse by
generalizing `initialize-dotnet` to support `global.json`.

## 2. Impact Analysis

- **Epic impact:** None. No product epic or story is affected.
- **Story impact:** None.
- **Artifact conflicts:**
  - PRD — none.
  - Architecture (product) — none.
  - UI/UX — none.
  - **CI/CD pipelines — primary target.** `Hexalith.Builds` (shared submodule),
    `ci.yml`, `release.yml`.
  - `project-context.md` — optional doc note (Development Workflow Rules) once adopted.
- **Technical impact:** Removes 4 duplicated Dapr blocks; introduces a cross-repo
  dependency on a `Hexalith.Builds` composite action (must be pushed before Tenants can
  pin it). Supply-chain posture preserved: the new action reuses the exact same pinned
  third-party action SHAs already used by Tenants.

## 3. Recommended Approach

**Option 1 — Direct Adjustment.** Modify the CI artifacts in place; no rollback, no MVP
review. Effort: **Low**. Risk: **Low**. Timeline impact: negligible.

Deliberately **out of Phase 1 scope** (kept for a later "Full" phase):

- Adopting `initialize-dotnet` inside Tenants (would swap Tenants' SHA-pinned
  `actions/setup-dotnet@v4.3.1` for the action's floating `@v5` — a posture regression).
- A parameterized `workflow_call` reusable workflow in `Hexalith.Builds` consumed by all
  domain modules (the real, larger mutualization — Phase 2).

## 4. Detailed Change Proposals

### A. `Hexalith.Builds` (shared submodule)

**A1 — NEW `Github/dapr-init/action.yml`** — composite action factoring the Dapr block,
reusing the same pinned SHAs (`dapr/setup-dapr@8d98091…` / `nick-fields/retry@ad98453…`),
`version` input defaulting to `1.17.0`. (+ `Github/dapr-init/README.md`.)

**A2 — EDIT `Github/initialize-dotnet/action.yml`** — add an optional
`global-json-file` input (backward compatible: unchanged `10.0.302` default behavior when
omitted). Enables future adoption by Tenants and other modules.

### B. `Hexalith.Tenants`

**B1 — EDIT `ci.yml`** — replace the 3 identical Dapr install+init blocks with a single
call each:

```yaml
      - name: Install and initialize Dapr
        uses: Hexalith/Hexalith.Builds/Github/dapr-init@main # TODO: pin to Builds commit SHA once pushed
        with:
          version: ${{ env.DAPR_VERSION }}
```

**B2 — EDIT `release.yml`** — same replacement for the single Dapr block.

## 5. Implementation Handoff

- **Scope:** Moderate (cross-repo).
- **Sequencing (critical):**
  1. Commit + push `Hexalith.Builds` with `dapr-init` (and generalized
     `initialize-dotnet`) → obtain commit SHA `X`.
  2. In Tenants `ci.yml` / `release.yml`, replace `@main` with `@X` (pin to SHA to match
     Tenants' supply-chain posture).
  3. Commit Tenants workflow changes.
  4. Verify on a CI run / PR that all tiers still install Dapr correctly.
- **Owner:** Developer (Jérôme Piquot to push the `Hexalith.Builds` submodule, per the
  repo's cross-submodule push policy — the assistant does not push).
- **Success criteria:** `ci.yml` + `release.yml` contain zero inline Dapr blocks; all CI
  tiers green; the `dapr-init` ref is pinned to a pushed Builds SHA.

## 6. Checklist Status

- §1 Trigger & context — Done
- §2 Epic impact — N/A
- §3.1 PRD / §3.2 Architecture / §3.3 UI/UX — N/A
- §3.4 Other artifacts (CI/CD) — Action-needed (this proposal)
- §4 Path forward — Option 1 selected (Done)
- §5 Proposal components — Done
- §6 Final review & handoff — pending user approval

---

# Phase 2 — Reusable `workflow_call` CI (applied 2026-06-30)

Phase 2 promotes the entire Tenants CI pipeline into a single parameterized reusable
workflow in `Hexalith.Builds`, consumed by Tenants and reusable by other domain modules
(Memories / EventStore / FrontComposer).

## Changes applied

- **NEW `Hexalith.Builds/.github/workflows/domain-ci.yml`** — reusable (`workflow_call`)
  pipeline reproducing the 3 jobs (`build-and-test` incl. consumer validation + Tier 1 +
  Tier 2 + coverage gate; `aspire-tests`; nightly `performance-tests`). Fully
  parameterized via inputs (solution, per-tier test project lists, Dapr version, coverage
  thresholds + isolation targets, toggles). Same third-party action SHAs as Tenants.
- **NEW `Hexalith.Builds/.github/workflows/domain-ci.md`** — usage doc.
- **REWRITE `Hexalith.Tenants/.github/workflows/ci.yml`** — 198 → 40 lines; now a thin
  caller of `domain-ci.yml@main` (TODO: pin to SHA).

## Design decisions

- **D1** All 3 jobs generic; module-specific values are inputs.
- **D2** Multiline lists (`workflow_call` has no array type) passed via `env:` and split
  in bash (`<<< "$VAR"`) — avoids heredoc indentation fragility.
- **D3** Python scripts stay in the consumer repo (reusable workflow checks out the
  caller). Convention: consumers provide `scripts/`. Moving scripts into Builds = future
  work, out of scope.
- **D4** Test result folders derived from each project basename under `TestResults/`
  (artifact label change only; coverage gate globs `TestResults/**`, unaffected).
- **D5** Supply-chain posture preserved (pinned SHAs); caller pins `@main` → SHA after the
  Builds push.

## Validation

- YAML parse OK (ci.yml + domain-ci.yml).
- `actionlint` 1.7.7: **0 issues** on ci.yml, domain-ci.yml, release.yml (incl. shellcheck
  of the bash run-steps).
- **Live CI run still required** end-to-end (cannot execute GitHub Actions locally).

## Handoff (updated — Phase 1 already landed)

State discovered at end of session:

- **Phase 1 (Builds): DONE** — committed + pushed as
  `2e238ebe5ce1a8a633a8481f05c821500b02ed4b` on `Hexalith.Builds@main`; Tenants parent
  gitlink already records it. `dapr-init` + generalized `initialize-dotnet` are live.
- **Phase 2 (Builds): pending** — `.github/workflows/domain-ci.yml` + `domain-ci.md`
  untracked, need commit + push.
- **Tenants workflows: pending** — `ci.yml` + `release.yml` modified, uncommitted, still
  reference `@main`.

Remaining steps:

1. Commit + push `Hexalith.Builds` Phase 2 files (domain-ci.yml + domain-ci.md) → SHA `Y`
   (descendant of `2e238eb`, so `Y` contains both phases).
2. Pin `@main` → `@Y` in Tenants `ci.yml` (caller → domain-ci.yml) and `release.yml`
   (dapr-init), and inside Builds `domain-ci.yml` (internal dapr-init reference). The
   already-published dapr-init refs may instead be pinned to `2e238eb` now if preferred
   (mixed per-action SHAs are idiomatic).
3. Bump the Tenants parent gitlink to record `Y`; commit Tenants.
4. Verify a real CI run (PR) is green across all tiers (could not run Actions locally).

---

# Phase 3 — Release mutualization + initialize-dotnet adoption (applied 2026-06-30)

Scope chosen: **3a + 3c** (3b — moving scripts into Builds — rejected: the scripts are
heavily domain-specific, e.g. `pack-release-packages.py` hardcodes the 5 Tenants csproj;
moving them would leak domain knowledge into the shared build repo).

## Changes applied

- **NEW `Hexalith.Builds/.github/workflows/domain-release.yml`** — reusable
  (`workflow_call`) release pipeline (checkout, .NET via initialize-dotnet, node, npm ci,
  restore, `Release -warnaserror` build, Dapr, Tier 1+2 tests, `npx semantic-release`).
  `secrets.NUGET_API_KEY` required; `GITHUB_TOKEN` auto-available. Parameterized
  (solution, test-projects, node-version, dapr-version, timeouts).
- **EDIT `Hexalith.Builds/Github/initialize-dotnet/action.yml`** — pin `actions/setup-dotnet`
  `@v5` → `@67a3573…` (v4.3.1) on both steps (matches Tenants' pinned SHA; preserves
  supply-chain posture). Side effect: Builds' own `build-packages` action now also uses
  the pinned v4.3.1 setup-dotnet (minor alignment).
- **EDIT `Hexalith.Builds/.github/workflows/domain-ci.yml`** — 3 inlined `setup-dotnet`
  steps → `uses: Hexalith/Hexalith.Builds/Github/initialize-dotnet@main` (DRY).
- **REWRITE `Hexalith.Tenants/.github/workflows/release.yml`** — 77 → 26 lines; thin caller
  of `domain-release.yml@main` (TODO: pin to SHA).

## Validation

- YAML parse OK (release.yml, domain-release.yml, domain-ci.yml, initialize-dotnet).
- `actionlint` 1.7.7: **0 issues** on ci.yml, release.yml, domain-ci.yml, domain-release.yml.
- **Live CI/release run still required** end-to-end.

## Remaining handoff

1. Commit + push `Hexalith.Builds` Phase 2 + Phase 3 files (domain-ci.yml, domain-ci.md,
   domain-release.yml, initialize-dotnet edit) → SHA `Y` (descendant of `2e238eb`).
2. Pin every `@main` → `@Y`: Tenants `ci.yml` (→ domain-ci) and `release.yml`
   (→ domain-release); inside Builds `domain-ci.yml` and `domain-release.yml` the internal
   `dapr-init@main` / `initialize-dotnet@main` references.
3. Bump the Tenants parent gitlink to `Y`; commit Tenants.
4. Verify a real CI run (PR) **and** a real release run (merge to main) are green.

## Still deferred (future)

- Move/genericize `scripts/*.py` into Builds (rejected for now — domain-specific).
- Adopt the same reusable workflows in Memories / EventStore / FrontComposer.
