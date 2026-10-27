---
title: 'Qualify CI gates and release execution provenance'
type: 'bugfix'
created: '2026-10-27'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '2cf3a9660f71f6877923772cd145aa6856907945'
resumed_revision: 'ff8c9a6273f70754c50a53b46dd4b4ff6fc2828b'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete DW-540, DW-554, DW-570, DW-584 and DW-593: CI, package-layer and release-provenance gates. Implement, independently review, verify, commit and push to main, as requested.

## Boundaries & Constraints

Preserve standalone Trusted Publishing identities, production approval, current-main checks, artifact verification and credential sequencing. Shared capabilities belong in Builds; Tenants owns its gate inventory and package policy. Preserve other callers' defaults.

Do not dispatch publication/recovery, change external policies or rewrite history. Commit shared changes before parent gitlinks; validate messages with owning pinned commitlint. Preserve ledger history and unrelated work. Closure requires acceptance evidence; broader release readiness is separate.

## I/O & Edge-Case Matrix

| State | Required behavior |
| --- | --- |
| Required gate succeeds for exact main source | Admission may continue after other existing checks |
| Required gate fails, skips, is missing/pending or targets another source | Refuse admission with a specific diagnostic |
| Technical ServiceDefaults dependency | Apply approved package-layer policy plus existing exact ID/range/framework checks |
| Workflow identity or closure is unverifiable | Refuse before preparing publishable artifacts |
| Recovery tooling differs from approved source declaration | Refuse before build/publication; retain evidence |
| Prepared tooling/provenance changes later | Refuse reuse of prepared state |

## Approved Decisions

The user approved all five recommendations and continued implementation on 2026-10-27.

1. **DW-540:** Generic Aspire tests become blocking with browser/TLS/Dapr prerequisites and skip rejection. Other reusable callers retain their defaults.
2. **DW-554:** Both exact technical ServiceDefaults IDs are allowed only in `Hexalith.Tenants.Server`. Other published or unknown packages receive no exception; exact range/framework checks remain.
3. **DW-570:** Add standalone closure support in Builds. Validate the actual standalone workflow identity and bounded closure while preserving existing reusable identity enforcement and Trusted Publishing jobs.
4. **DW-584:** Release and recovery require `ci.yml`, `story-guards.yml` and `source-reference.yml`. The latest exact-source push run/attempt for each must succeed; older successes cannot mask later failures.
5. **DW-593:** Recovery executes the Builds revision approved by the source publication workflow. Source build props remain source-pinned; executed shared helpers retain verified identity throughout recovery.

</frozen-after-approval>

## Code Map

- `.github/workflows/ci.yml`; Builds `.github/workflows/domain-ci.yml`: opt-in prerequisites and skip contract. Reuse existing authenticated-route setup.
- `scripts/validate-nuget-packages.py`, `validate_dependency_boundaries`: replace global exceptions with approved package rules; inventory is `tools/release-packages.json`.
- `.github/workflows/release.yml`, `recover-partial-release.yml`: consume approved gates; preserve standalone jobs.
- Builds `Github/governed-provenance/action.yml`, `governed_provenance.py`: reuse closure collection, preserve `validate_workflow_identity`, add standalone support and shared source-workflow gating.
- `scripts/validate-partial-release-recovery.sh`, `partial-release-recovery-common.sh`, `prepare-partial-release.sh`, `publish-partial-release.sh`: bind execution identity; current OCI helper defaults to the source gitlink.
- `tests/scripts/test_nuget_dependency_ranges.py`, `test_partial_release_recovery.py`; Contracts `CiQualityGateScriptTests`, `PackageGovernanceTests`, `PartialReleaseRecoveryWorkflowTests`, `PartialReleaseRecoveryTests`: existing executable fixtures.
- Contracts `PartialReleasePublicationTests`, `PartialReleaseVerificationTests` and `tests/scripts/recovery_execution_fixture.py`: update additional publication/verification callers and share immutable source/tooling fixture setup.
- Builds `Github/governed-provenance/execution_identity.py`, `source_workflow_gates.py` and owning tests: bind approved recovery helpers and latest workflow runs. `Github/publish-containers/oci_registry_validator.py` exposes explicit initial creation-label observation for recovery, preserving expected-time and receipt checks.
- `scripts/validate-publication-preflight.sh`: retain the caller-owned three-workflow inventory at the final normal-publication boundary using the existing shared gate helper.

## Tasks & Acceptance

- [x] Record policy answers in the frozen block.
- [x] Update mapped CI workflows, package validator and fixtures under approved rules.
- [x] Extend mapped Builds provenance/gating tools and tests; wire release/recovery consumers before sensitive phases.
- [x] Bind recovery identity across preparation/publication; reject drift and stale state.
- [ ] Update `CONTRIBUTING.md`, ledger, `docs/maintenance-backlog.md` and validation evidence after review; preserve all 657 IDs and unselected records.

Given each matrix refusal case, when its helper executes, then it fails before the sensitive phase and preserves accepted state.

Given valid inputs, when gates/preparation execute, then they succeed without publishing and retain source/tooling/closure evidence.

Given passing review/checks, when committing and pushing main, then retain validation and external limitations.

## Implementation Notes

- Implementation handoff covers code and local verification; the parent owns independent review, ledger acceptance, commits and pushes after workflow gates. Do not commit or perform remote operations in the implementation handoff.
- The initial publication pin lacked the new helpers. The local shared implementation is committed as `ad395b72f0639951d4bb2af91a22eba7fdb95207`; all three publication declarations now reference it. Validate the final consumer diff before pushing.
- Existing package-mode builds cannot resolve the pinned EventStore routing types. Debug/source provides the focused implementation lane; record exact package-mode commands and failures separately from passing checks.
- Recovery's existing suite wrapper now invokes immutable Git fixture setup. Any increased timeout is limited to this measured full-suite invocation; short workflow checks retain their original bound.

- Parent verification corrected two inherited fixture assumptions without changing production behavior: FrontComposer routing now uses an unavailable Memories fixture root, and container governance excludes the Platform/McpCli root submodules like the other reference owners. The four focused cases pass.

- A concurrent `/pushall` run recorded the implementation and review fixes, integrated newer Builds main and advanced five unrelated reference pointers. Preserve its current parent `ff8c9a6273f70754c50a53b46dd4b4ff6fc2828b` and Builds `35f185abdec37a3ae92d82f85a21e0f3c971d82d`. All three publication pins now use the final shared revision; the original baseline remains unchanged. Final verification uses these source heads.

## Spec Change Log

## Review Triage Log

Pass 1 reviewed the complete Tenants diff plus Builds `1783c0a377615063f3222c1d28f3ab513af4e72f` through `ad395b72f0639951d4bb2af91a22eba7fdb95207`. Each row is an individual reviewer finding, before grouping.

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| BH-1 defining job identity | high | Standalone action wiring substitutes caller contexts; a reusable job can therefore supply caller identity to the validator. GitHub's [job context](https://docs.github.com/en/actions/reference/workflows-and-actions/contexts) identifies the actual defining workflow. Patch the existing wiring and refusal controls. |
| BH-2 latest-run refresh | high | `qualify` never rereads an accepted earlier gate after later gates; a new attempt or run can appear during qualification. Patch a final freshness check that refuses changed identities/attempts. |
| BH-3 final publication admission | high | The Tenants publication bridge delegates to the older shared preflight, which filters for any successful CI run. The new final three-gate policy must also run at this existing publication boundary; patch the thin caller bridge. |
| BH-4 installed publisher bytes | high | Preparation verifies original helper bytes, but restored copies can change during preparation independently of the fresh execution checkout. Patch comparison of every consumed installed publisher file to approved Git blobs before credentials. |
| BH-5 repository run-script closure | false | The evaluator explicitly collects bounded static `uses:` action/helper edges, matching the existing shared contract; it does not claim interpreter-transitive collection of arbitrary repository `run:` commands. The candidate commit separately identifies repository source. No such expanded closure guarantee was approved. |
| BH-6 post-publish helper drift | medium | Recovery calls the OCI validator after MSBuild publication without another execution check. A target can alter the helper checkout during that call. Patch revalidation before the validator and subsequent writes, with a controlled mutation test. |
| BH-7 environment declaration parsing | medium | A local probe with a flow-style real environment plus a heredoc declaration returns the heredoc SHA. Patch structural extraction of effective literal environment declarations, rejecting unsupported forms and body-text spoofing in both resolver paths. |
| BH-8 result summary integrity | medium | The extracted TRX gate admits executed=1/error=1 with an aborted summary because it only reads three counters. Patch successful-summary and consistent-passing-counter requirements, including malformed/error/timeout controls. |
| BH-9 API refusal diagnostics | low | The workflow observation records its filename, but API failures discard endpoint, exit status and HTTP failure class, leaving operators without a corrective signal. Patch bounded safe request/status context without recording credentials. |
| BH-10 consumer qualification | medium | Manual immutable snapshots of both real workflow files and the final declared execution identity pass, but persistent real-file regression coverage is missing. Patch that coverage. Fresh full generic Aspire provisioning remains unqualified behind the existing published-package routing compile blocker; retain it as a separate runtime-qualification follow-up, without claiming live CI acceptance. |
| EC-1 earlier attempt changes | high | Independently confirms BH-2's interleaving; group under the same freshness patch while retaining this individual verdict. |
| VG-1 final normal-release refusal tests | medium | Pre-verified regression gap: current executable drift/gate tests exercise recovery, while normal publication's final admission step has only text/placement checks. Patch executable matching, independently altered provenance and latest-run refusal cases using the actual workflow step. |

Patch work preserves the approved policies and existing command interfaces. The full Contracts lane passed 704/704 before these review corrections; rerun relevant checks after correction, then final acceptance. No policy question or new publication authorization is introduced.

## Verification

- Run `python3 -m unittest discover -s tests/scripts -p 'test_*release*.py'` and `python3 tests/scripts/test_nuget_dependency_ranges.py`; require positive/refusal controls.
- Run Builds `Github/governed-provenance/tests/test_governed_provenance.py` and new gate tests, preserving reusable identity refusal.
- Build/run Contracts tests in Debug/source, focused then full; check CI behavior in Release/package.
- Validate story gitlinks, five-record/index conservation and whitespace; retain commands, outputs and canonical revisions.
- Qualify prerequisite, failure, skip and passing CI outcomes; preserve nightly performance exclusion.

## File List

- `references/Hexalith.Builds`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.McpCli`
- `references/Hexalith.Memories`
- `references/Hexalith.Platform`

## Completion Notes List

Builds supplies this task's shared CI, provenance and recovery changes. The other five pointers were advanced by the concurrent run and are preserved without attributing their implementation to this task. The declarations below describe the full comparison range required by the guard.

| Submodule | Baseline | Target |
| --- | --- | --- |
| `references/Hexalith.Builds` | `1783c0a377615063f3222c1d28f3ab513af4e72f` | `35f185abdec37a3ae92d82f85a21e0f3c971d82d` |
| `references/Hexalith.EventStore` | `b1dd186ad49aba1c0c21d452c75fc7769feba1da` | `4dc256c4db7a0ba88efdbbe0f6bcbcf047c09d34` |
| `references/Hexalith.FrontComposer` | `25e8a07198987b33cef052bdbc745f836cb6cbbf` | `8d2b47bd206ffd5c5e77a5ac0461b9cfe52ea4fd` |
| `references/Hexalith.McpCli` | `aabc03c68c56ac8085ee1b145654576f5fd201a4` | `8bfd1e0592bfee258b381f41b098fe3b35170fdd` |
| `references/Hexalith.Memories` | `2bfc735600ed153bce9069570a43ba93fe77ac0f` | `f480fd70a7c25587264f50b5174358eee709c513` |
| `references/Hexalith.Platform` | `f187f346bb2a1136f4be7221af82136f779c2928` | `73903003acd33ba341e12aa3c495992bab8b0eda` |
