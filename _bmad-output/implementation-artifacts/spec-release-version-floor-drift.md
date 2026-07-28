---
title: 'Restore the release version floor and guard registry/tag drift'
type: 'bugfix'
created: '2026-07-28'
status: 'in-progress'
baseline_commit: 'f6ccee0'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Release run 30333764600 died at semantic-release `verifyRelease` with `[publication-preflight] version-collision`. Tags `v3.3.0`…`v3.15.1` were deleted from git, so the floor fell back to `v3.2.18` and semantic-release proposed **3.3.0** — already on nuget.org for `Hexalith.Tenants.Contracts`, `.Server` and `.Aspire`, published from this repository (nuspec `repository commit=5e27c2b`, an ancestor of `main`) on 2026-05-02. That abandoned lineage reached **3.15.1**, so the entire 3.3.0–3.15.1 band is unusable and every dispatch fails identically. Nothing was half-published — the preflight failed closed.

**Approach:** Push an annotated tag `v3.15.1` at the commit `v3.2.18` points to (`7918ac69`) to restore the floor, and move the series to **4.0.0** — a band free on all five package IDs — via a `BREAKING CHANGE:` footer. Add a guard to `release.yml`'s unprotected `verify-source` job that refuses the dispatch when any published version exceeds the highest release tag reachable from the dispatched SHA.

## Boundaries & Constraints

**Always:**
- Guard lives in `verify-source`, so it fails **before** the production environment or any secret is reachable.
- `gh api` + `curl` + `jq` only. `release.yml` must keep zero third-party actions — `PackageGovernanceTests` asserts every `uses:` is a `Hexalith/Hexalith.Builds/.github/workflows/…` reference pinned to a 40-hex SHA — so **no `actions/checkout`**: read `tools/release-packages.json` through the contents API at the dispatched SHA.
- Compare versions numerically (`split(".") | map(tonumber)` array compare in `jq`), never lexically: `3.15.1 > 3.2.18`.
- Floor = highest `v<semver>` tag **reachable from the dispatched SHA**, matching semantic-release's `git tag --merged` + highest-semver rule. Walk tags in descending semver order, take the first whose `compare/<tag>...<sha>` status is `behind` or `identical`.
- Fail closed on any probe failure, unparseable response, or absence of a reachable release tag.
- `tools/release-packages.json` stays the single inventory — read it, don't hard-code IDs.

**Ask First:**
- Deleting, moving or re-pointing any tag other than adding `v3.15.1`.
- Changing package IDs, the container repository, or the expected package count.
- Every push to `origin` (branch or tag).

**Never:**
- `feat!:` / `fix!:` to trigger the major. Pinned `conventional-changelog-angular@8.3.1` uses `headerPattern: /^(\w*)(?:\((.*)\))?: (.*)$/` and `noteKeywords: ['BREAKING CHANGE']` with no `breakingHeaderPattern`; a `!` header fails to parse and yields **no release at all**. Only a `BREAKING CHANGE:` footer works.
- Reintroducing `--skip-duplicate` — it is what let the July lineage silently overlap the May one.
- Editing `references/Hexalith.Builds`, or committing to `main`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Floor covers registry | floor `v3.15.1`, max published `3.15.1` | Passes | N/A |
| Registry ahead of floor | floor `v3.2.18`, `Contracts` at `3.15.1` | Exit 1 naming package, published version, floor tag, and the restore-the-missing-tag remedy | Fails pre-approval |
| Equal is not a collision | floor `v4.0.0`, max published `4.0.0` | Passes (strict `>`) | N/A |
| Unreachable higher tag | `v9.9.9` not merged into the dispatched SHA | Ignored; next-highest reachable tag is the floor | N/A |
| Never published | flatcontainer index 404 | Treated as no published versions | N/A |
| Probe unusable | non-200/404, malformed JSON, `gh` failure | Exit 1, drift unproven | Fails closed |
| No reachable tag | no `v*` semver tag merged into the dispatched SHA | Exit 1 | Fails closed |

</frozen-after-approval>

## Code Map

- `.github/workflows/release.yml` -- `verify-source` job (lines 18–71); guard step goes after the exact-source CI check. `release` already `needs: verify-source`.
- `tools/release-packages.json` -- five-package inventory the guard reads.
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs` -- release governance suite; reuse `GetYamlJobBlock`, `GetYamlStepRunBlock`, and the fake-`gh` harness in `RunPublicationPostconditionAsync` (line 924).
- `scripts/validate-publication-preflight.sh` + `references/Hexalith.Builds/Github/publish-containers/publication_preflight.py` -- where the collision surfaces today. Unchanged; still the last defence and the only check covering the container tag.
- `_bmad-output/project-context.md` -- its release rule wrongly claims `feat!` triggers a major.

## Tasks & Acceptance

**Execution:**
- [ ] `.github/workflows/release.yml` -- add a `Require registry versions at or below the release tag floor` step to `verify-source`: resolve the reachable floor tag, read manifest IDs at the dispatched SHA, probe `https://api.nuget.org/v3-flatcontainer/<id>/index.json` per package, fail per the matrix -- turns a post-approval collision into a named pre-approval failure.
- [ ] `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs` -- assert the new step and its remedy wording in the release-workflow governance test, and add a behavioural test driving the extracted run block with stubbed `gh`/`curl` across every matrix row.
- [ ] `_bmad-output/project-context.md` -- correct the release rule to name the `BREAKING CHANGE:` footer as the only major trigger.
- [ ] Commit on a `fix/…` branch with a `BREAKING CHANGE:` footer, footer lines ≤ 100 chars (`footer-max-line-length` is not relaxed in `commitlint.config.mjs`).
- [ ] Create annotated tag `v3.15.1` at `7918ac69`; push after human confirmation.

**Acceptance Criteria:**
- Given the tag is pushed and the branch merged, when Release is dispatched from the `main` tip with green exact-source CI, then `verify-source` passes and semantic-release proposes `4.0.0`.
- Given the floor tag were absent, when Release is dispatched, then `verify-source` exits 1 naming `Hexalith.Tenants.Contracts 3.15.1` and the release job never starts.
- Given any package probe cannot be completed, when `verify-source` runs, then it exits 1 rather than assuming absence.

## Spec Change Log

## Design Notes

The floor tag is required even though `v3.2.18` + BREAKING CHANGE alone already yields `4.0.0`: the guard compares the registry high-water mark (`3.15.1`) against the tag floor, so without the tag it would correctly block this very release. The tag is also the factual record — `3.15.1` really was published from here.

## Verification

**Commands:**
- `actionlint .github/workflows/release.yml` -- expected: no findings.
- `dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror` -- expected: 0 warnings, 0 errors.
- `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj` -- expected: all pass, including the new guard tests.
- `git tag --merged HEAD --sort=-v:refname | head -1` -- expected: `v3.15.1` after tagging.
- `git log v3.15.1..HEAD --format=%B | grep -c '^BREAKING CHANGE:'` -- expected: ≥ 1 after the fix commit.
