# Sprint Change Proposal — CI/CD Standards Alignment (Tenants + Hexalith.Builds)

- **Date:** 2026-07-09
- **Trigger:** CI/CD architecture review of Hexalith.Tenants against `ci-cd-standards.md` (same-session senior CI/CD review, findings F1–F21)
- **Mode:** Batch (user pre-approved implementation: "do the needed changes")
- **Scope classification:** Minor–Moderate (direct implementation, no epic/story/PRD/UX impact)
- **Status:** IMPLEMENTED (uncommitted in both repositories), all validations green

## 1. Issue Summary

A strict CI/CD review found that while the Tenants pipeline architecture is fundamentally right (thin callers over Hexalith.Builds reusable workflows, SHA-pinned third parties, `@main` Builds refs, root-only submodule init), three structural debts and a tail of hygiene issues contradicted the shared standards:

1. **Split-brain release configuration.** Two diverged semantic-release configs were tracked; only `.releaserc.json` runs (cosmiconfig precedence), yet the dead `release.config.cjs` contained a *stronger* gate (consumer-package validation) and was still being edited in the same commits.
2. **Release not gated on CI.** Release ran on push to `main` in parallel with CI, re-running build + Tier 1/2 tests (≈2× compute) while its own gate was *weaker* than CI's — a commit failing consumer validation in CI could still publish to NuGet.org. Evidence of the enforcement gap: `c506c97` (`fix:` typed submodule bump, should have been `chore(deps):`) minted release 3.2.2 with no package change, because commitlint runs only on PRs while the team pushes directly to `main`.
3. **Standards violations in the shared workflows**: NuGet cache key missing `global.json` and the imported Builds package props; no TRX upload from the release test run; `NU1901–NU1904` not carved out of warnings-as-errors; contradictory shared docs (`domain-ci.md` said "pin to tag/SHA" against the `@main` policy; `domain-release.md` described steps that no longer matched the YAML); legacy pre-domain-workflow actions still documented as the release path.

## 2. Impact Analysis

- **Epics/Stories:** None affected. No epic, story, or acceptance criterion references the release trigger mechanics or the internal shape of the CI scripts.
- **PRD/Architecture/UX:** Not affected. The PRD-scoped quality gates (line coverage >80% on the four package projects, 100% branch coverage on the isolation/auth files) are preserved bit-for-bit; only their *wiring* moved (scope now passed explicitly by CI).
- **Artifacts affected:** `.github/workflows/*` (Tenants), `.releaserc.json`, `scripts/validate-coverage.py`, `scripts/validate-consumer-package-references.py`, `Directory.Build.props`, `_bmad-output/project-context.md`, and the `references/Hexalith.Builds` submodule (reusable workflows, docs, props, one new composite action).
- **Constraint reconciliation with project-context:**
  - `MSBuild.rsp` was **kept** (review suggested deletion; project-context records serialized builds as intentional — the intent lives in `Directory.Solution.props`, which is what actually enforces it).
  - `Hexalith.Build.props` is **still not imported** by Tenants (review flagged the docs contradiction; project-context records the lightweight analyzer policy as deliberate). The exception is now documented in `Directory.Build.props`, and the NuGet-audit carve-out was added in both places so each repo generation gets it.
  - The full move of the four Python validation engines into Hexalith.Builds stays **deferred** — it was explicitly rejected on 2026-06-30 ("domain-specific"). Only the non-controversial parameterization landed (coverage line scope is now a `domain-ci` input instead of a hard-coded constant).

## 3. Recommended Approach — Direct Adjustment (implemented)

Direct adjustment within the existing pipeline architecture: fix the release trust model, close the standards gaps in the shared workflows, and clean the obsolete surface. No rollback (nothing to revert — the shipped pipeline works, it is the policy conformance that lagged) and no MVP impact. Effort: low (one session). Risk: low — behavior changes are confined to CI/CD; every quality gate is preserved or strengthened.

## 4. Detailed Changes

### 4.1 Hexalith.Tenants

| File | Change | Finding |
|---|---|---|
| `.github/workflows/release.yml` | Trigger is now `workflow_run` on CI success, guarded by `conclusion == 'success' && event == 'push'` (scheduled CI runs cannot start releases). Added non-cancelling `release-*` concurrency group. Permissions moved to job level (workflow level is `contents: read`). Secrets passed explicitly (`NUGET_API_KEY`, `HEXALITH_ZOT_*`) instead of `secrets: inherit`. Dropped `test-projects` (CI is the gate now) and the redundant `dapr-version`. | F2, F4, F5, F14, F19 |
| `.github/workflows/ci.yml` | Dropped redundant `dapr-version: '1.18.0'` (equals the shared default). Added explicit `coverage-line-scope` (the four package projects) so the caller, not a script constant, is the source of truth in CI. | F14, F3-lite |
| `.github/workflows/commitlint.yml` | Also runs on `push` to `main` — closes the bypass that let `c506c97` (`fix:` submodule bump) mint release 3.2.2. | F11 |
| `.releaserc.json` | `prepareCmd`: explicit solution path, `python` → `python3`, and **consumer-package validation added** (`validate-consumer-package-references.py`) so the release gate is no longer weaker than CI. | F1, F15 |
| `release.config.cjs` | **Deleted** (dead config — lost cosmiconfig precedence to `.releaserc.json`; its one stronger step was merged into the live config first). | F1 |
| `status.txt` | **Deleted** (tracked stale `git status` dump). | F18 |
| `scripts/validate-coverage.py` | New repeatable `--line-scope` argument; `PACKAGE_LINE_SCOPE` demoted to documented local-run fallback. Docstring corrected (said "five" projects; scope is four). | F3-lite |
| `scripts/validate-consumer-package-references.py` | Comment documenting why `Hexalith.Tenants.Aspire` is excluded (AppHost-shaped consumer; covered by pack/metadata validation). | F21 |
| `Directory.Build.props` | NuGet audit kept enabled (`NuGetAudit=true`, `NuGetAuditMode=all`) with `NU1901–NU1904` in `WarningsNotAsErrors` per standards; comment documenting the deliberate non-import of `Hexalith.Build.props`. | F8, F9 |
| `_bmad-output/project-context.md` | Fixed stale facts: CI Dapr version (1.17.0 → 1.18.0 shared default), release flow description (now CI-gated via `workflow_run`, no test re-run). | doc sync |

### 4.2 Hexalith.Builds (submodule, on `main`)

| File | Change | Finding |
|---|---|---|
| `.github/workflows/domain-ci.yml` | Cache-key default now hashes `Directory.Packages.props, global.json, references/Hexalith.Builds/Props/Directory.Packages.props`. New `coverage-line-scope` input wired to `--line-scope`. Submodule init deduplicated onto the `Github/initialize-build` composite (3 jobs). Aspire tier: coverage collection removed (was never gated or uploaded on success), TRX upload now `if: always()`, non-blocking rationale commented. | F6, F3-lite, F13, F16 |
| `.github/workflows/domain-release.yml` | Cache-key default aligned with domain-ci. `npm audit signatures` after `npm ci` (parity with the legacy actions' supply-chain check). Release test evidence uploaded `if: always()` when tests run. 60-line container-publisher heredoc replaced by the new `Github/publish-containers` composite. `test-projects` description now steers `workflow_run` callers to leave it empty. | F6, F17, F7, F20 |
| `Github/publish-containers/` (**new**) | Composite action + checked-in `publish-containers.sh` (verbatim from the heredoc). Ships with the action (`GITHUB_ACTION_PATH`), so it version-matches the `@main` workflow rather than the caller's submodule pin. | F20 |
| `.github/workflows/commitlint.yml` | Event-aware: PR range on `pull_request`, `before..after` on `push` (falls back to `--last` on branch creation / unreachable before-SHA). `npm audit signatures` added. | F11, F17 |
| `Hexalith.Build.props` | Same NuGet-audit carve-out as the module (`NU1901–NU1904` in `WarningsNotAsErrors`, audit explicitly on). | F8 |
| `.github/workflows/ci-cd-standards.md` | Release Gates now prescribe the concrete `workflow_run` caller pattern, non-cancelling release concurrency, and explicit secret mapping over `secrets: inherit`. | F2, F4, F5 |
| `.github/workflows/domain-ci.md` | Version Reference contradiction fixed (`@main`, per standards). Aspire tier documented as advisory-by-default with rationale. `--line-scope` script contract documented. | F10, F16 |
| `.github/workflows/domain-release.md` | Steps corrected (`actions/setup-dotnet`, `initialize-build`, composite publisher, `npm audit signatures`). Usage example rewritten to the `workflow_run` + explicit-secrets caller. `secrets: inherit` demoted to discouraged alternative. | F10, F2, F5 |
| `README.md` + 6 legacy action READMEs | `create-release`, `package-release`, `unit-tests`, `verify`, `publish-container-to-registry`, `publish-azure-container-app` marked deprecated/legacy with pointers to `domain-ci.yml`/`domain-release.yml`; README release section now leads with the domain workflow; new `publish-containers` listed. Actions kept functional for existing consumers. | F12 |

### 4.3 Deferred (deliberate, with reasons)

- **F3 full engine/manifest split** (move the four Python validation engines to `Hexalith.Builds/Github/scripts/`, module keeps a data manifest): rejected by user decision on 2026-06-30; re-proposal belongs to a dedicated cross-module story once a second module (e.g. Parties) adopts `domain-ci`.
- **F9 shared-props import / container-defaults extraction to Builds targets**: conflicts with the intentional lightweight analyzer policy; documented as an exception instead.
- **Builds README full restructure**: banners and pointers landed; a rewrite is cosmetic and can ride any future Builds change.

## 5. Validation Evidence

- `actionlint` — 0 issues on all Tenants and all Hexalith.Builds workflows after the changes.
- `python3 -m py_compile` — both edited scripts compile.
- `validate-coverage.py` exercised against a synthetic Cobertura report in three modes: explicit CI-shape `--line-scope` (83.33%, 5/6 in-scope lines, host project correctly excluded), default fallback (identical result), and narrow scope (host `Program.cs` lines excluded from the denominator). Isolation branch gate unchanged (100%, missing-target detection intact).
- `.releaserc.json` parses as JSON; `publish-containers.sh` passes `bash -n`; both edited `.props` files parse as XML.
- Not run (environment-blocked / out of scope): a live GitHub Actions run of the new `workflow_run` chain — first push to `main` after commit will exercise it; the semantic-release `[skip ci]` changelog commit suppresses CI and therefore cannot re-trigger Release (no loop).

## 6. Implementation Handoff

- **Scope:** Minor–Moderate → Developer-executed (done in this session). No PO/PM escalation: no backlog, scope, or requirement changed.
- **Remaining owner actions (Jérôme):**
  1. Review + commit **Hexalith.Builds first** and push to `main` (module workflows resolve `@main`; Tenants CI/Release consume these changes immediately). Suggested: `ci: gate releases on CI, harden shared workflows, deprecate legacy actions`.
  2. Commit Tenants (workflows + release config + scripts + props + docs) and bump the Builds submodule gitlink in the same or a following commit (`chore(deps): bump Hexalith.Builds submodule`). Per `submodule-pointer-push-consistency`, push Builds **before** Tenants.
  3. After the first merge to `main`, verify the chain once: CI success → Release run appears (skipped for scheduled CI), semantic-release publishes as before.
- **Success criteria:** Release workflow only ever runs after a green push-event CI; a consumer-validation failure now blocks publishing; one semantic-release config in the repo; `actionlint` stays clean; coverage gate values unchanged (80/100).
