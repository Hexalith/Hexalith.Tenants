---
title: 'Sprint Change Proposal — pragmatic release policy and local-checker exception'
date: '2026-11-16'
trigger_spec: '_bmad-output/implementation-artifacts/spec-synchronize-update-release-2026-11-14.md'
mode: 'batch'
scope: 'minor'
status: 'approved'
approved: '2026-11-16 — user approved as written'
routed_to: 'Release developer agent (§4-A, §4-D, release completion); AI.Tools maintainer (§4-B, §4-C)'
---

# Sprint Change Proposal — Pragmatic Release Policy (2026-11-16)

## 1. Issue Summary

The .NET 11 dependency synchronization and release (`spec-synchronize-update-release-2026-11-14`) turned into repeated qualification and pipeline repair. After more than two days the root release is still unpublished.

| Signal | Measured 2026-11-16 |
| --- | --- |
| Spec size | 125 KB, 12 review sections, 107 individually triaged findings |
| Evidence directory | 3,983 files / ~470 MB (221 tracked = 30.7 MB; 1,779 untracked = 266 MB) |
| Root commits since 2026-11-14 | 34 |
| Upstream re-qualification loops | FrontComposer chased `0c → e0 → 695d → b34 → 11c → ebd → a03`; each advance voided prior qualification and started new reviews and mutation runs |

The user's own verdict is that builds, focused regression tests, security checks and published-package/source verification are worth their cost. Repeated review rounds and another 4.5-hour mutation sweep have no demonstrated return.

**Verified facts behind the corrections:**

1. **The mutation hold was self-imposed.** FrontComposer's full mutation runs in `mutation-property-nightly.yml` on a nightly cron (02:17) or by manual dispatch. FrontComposer's `release.yml` requires only exact-source push CI, Quality and production approval. No pipeline gate required a fresh mutation sweep before release.
2. **The `check.py` pre-push rule can't be satisfied anywhere in this workspace.**
   - AI.Tools `7ef8166` (2026-11-15 21:58) introduced the rule. Tenants' committed AI.Tools gitlink (`17a37d8`) does not contain it yet.
   - `Builds/Github/lib/local_check.py` requires a committed `.github/hexalith-ci.json`, a Builds gitlink equal to its `buildsTag` tag, a clean committed tree and a direct Builds checkout.
   - No repository in the workspace has `hexalith-ci.json` except Builds, and `check.py` refuses to run in Builds itself.
   - Initializing the nested Builds checkout is prohibited, but that is only a secondary blocker. A standalone clone would make Builds a direct submodule, yet the missing CiCd config would still fail the check.
   - The exact refusal is retained in `resumed-parent-integration/local-check-prerequisite-refusal.json`.

## 2. Impact Analysis

| Artifact | Impact |
| --- | --- |
| PRD, epics, stories | None. No product requirement or story changes. A search found no release or mutation-testing policy in these documents. |
| Architecture, UX | None. |
| Release spec `spec-synchronize-update-release-2026-11-14` | The two open tasks stay the same (root push + CI, release + NuGet verification). Their completion criteria are narrowed (§4-A). |
| Shared agent instructions (AI.Tools, cross-repo) | The `check.py` rule needs a CiCd-adoption condition (§4-C). The release-scope rules belong in the shared instructions, because the over-qualification happened in EventStore, FrontComposer and McpCli too (§4-B). |
| Pipelines | No change needed now. Nightly mutation already exists. CiCd adoption is a separate track. |

## 3. Recommended Approach — Direct Adjustment

- Finish the release on the frozen revisions, using the existing automated gates.
- Grant the scoped `check.py` exception below.
- Move the general policy into the shared instructions.

**Effort:** low. **Risk:** low, because no automated gate is weakened or skipped. **Timeline:** the release can finish today.

### Decision: local-checker exception (answer to "May I finish using the existing checks?")

**Approved, with this scope:**

- **Applies to:** pushes made for this release in repositories with no committed `.github/hexalith-ci.json` (Tenants, EventStore, FrontComposer).
- **Still required:**
  - Pinned commitlint on the exact message.
  - Focused tests for changed behavior.
  - Required push CI and Quality on the exact pushed source.
  - Security scans (CodeQL, dependency review, secret scanning).
  - Source/provenance and published-package verification.
  - Submodule commits pushed and remotely verified before the root gitlink commit.
- **Never:** nested submodule initialization, skip markers, `skip-checks` trailers, or weakened audits or tests.
- **Expires:** when the release completes, or when the repository adopts CiCd (its `hexalith-ci.json` lands), whichever comes first.
- **Status:** already granted in the concurrent release session at 2026-11-16T11:19:16Z (`pragmatic-release-user-authorization.json`). It was used for the FrontComposer push `ebd3f76d..a03ee98a` at 11:19:43Z. This proposal ratifies it.

## 4. Detailed Change Proposals

### A. Release spec — completion criteria

Spec `spec-synchronize-update-release-2026-11-14.md`, section "Pragmatic completion steering (2026-11-16)". Append:

```
NEW:
Done when, on frozen revisions (FrontComposer a03ee98a, EventStore fbed6cd3,
McpCli held at 4cf120c4, others as currently fast-forwarded):
- each pushed owner commit has green required push CI (+ Quality where the owner has it);
- root gitlink commit passes commitlint and required root CI once on the exact source;
- release workflow publishes; GitHub Release and all five NuGet packages report the
  release version and intended repository source;
- one release report exists (§4-D).
Out of scope for this release: further upstream advances, full mutation sweeps, additional
review rounds, CiCd adoption, new evidence-qualification CI steps.
```

Rationale: these are explicit stop conditions that replace open-ended re-qualification.

**Flag:** the root has an uncommitted change to `story-guards.yml` that adds "Qualify Dapr read-policy evidence". Under this policy that is an evidence-qualification step. Keep it only if it guards a real release regression; otherwise defer it to the pipeline track. The `ci.yml` floor bump (`66 → 67`) follows a newly added test and stays.

### B. Shared instructions — release scope (AI.Tools `hexalith-llm-instructions.md`, CI/CD section)

```
NEW (bullets after "Release —"):
- Release scope: freeze dependency revisions when a release starts; later upstream
  advances go to the next release unless they fix a release blocker.
- Test the affected behavior and run each repository's required CI once on the exact
  release source. Re-run only after a source change.
- Run full mutation testing on its schedule or after substantial changes to the code it
  targets; it is not a per-release gate unless a release workflow requires it.
- Keep pipeline modernization (CiCd adoption, new CI steps) out of product releases.
- Produce one concise release report per release; do not commit bulk diagnostic evidence.
```

Rationale: the user's policy applies to every module that went through this loop. Owner: AI.Tools maintainer. If you prefer to keep it Tenants-only, put it in the Tenants release documentation instead.

### C. Shared instructions — make `check.py` conditional on adoption

```
OLD:
  Require a successful `python3 references/Hexalith.Builds/check.py` from the
  module root before every push.

NEW:
  In a module that has adopted Hexalith.CiCd (it commits `.github/hexalith-ci.json`),
  require a successful `python3 references/Hexalith.Builds/check.py` before every
  push, run from a standalone clone of that module (where Builds is a direct
  submodule). Until a module adopts CiCd, its existing required CI gates apply.
  Never initialize nested submodules in an umbrella workspace to run the checker.
```

Rationale: as written, the rule blocks every push in the ecosystem. It was published before the CiCd rollout it depends on. Owner: AI.Tools / CiCd maintainer. This proposal does not apply the change.

### D. One concise release report

Path: `_bmad-output/implementation-artifacts/release-report-<version>.md`, at most one page:
- frozen revisions (all nine gitlinks);
- run IDs of the required CI and release runs;
- the five NuGet packages and their verified source;
- known deferrals, linked to `deferred-work.md`;
- the `check.py` exception reference.

The existing bulk evidence stays on disk untouched. The 1,779 untracked files are not committed, and deleting them is your call.

## 5. Implementation Handoff

**Scope: Minor.** The release developer agent implements it directly.

| Who | Action |
| --- | --- |
| Release dev agent (concurrent session) | EventStore fixture fix is now pushed as `fbed6cd3` (re-committed from `25cde206`; observed 11:52Z). Wait for its required CI. Verify remote reachability. Commit the root gitlinks with commitlint. Push the root and run required CI once. Run the release and verify the five packages. Write the §4-D report. Append §4-A to the spec. Decide on the `story-guards.yml` flag. |
| AI.Tools maintainer (Jérôme) | Apply §4-B and §4-C in AI.Tools, then bump the gitlink in a later, separate change. |
| Pipeline track (separate) | CiCd adoption (`hexalith-ci.json` per module), so that `check.py` becomes runnable. |

**Success criteria:**
- The root release is published, and all five NuGet packages verify against the intended source.
- Every required CI run on the exact pushed sources is green.
- No gate was weakened.
- One release report exists.
- No full mutation sweep or extra review round happened in this release.
