---
title: 'Verify released NuGet content through both official endpoints'
type: 'bugfix'
created: '2026-11-03'
status: 'done'
route: 'dispatch'
baseline_commit: '1b2c71b0e3cc5b16745f87aa7f1bc2c0ac8ffc27'
context:
  - '/home/administrator/projects/hexalith/tenants/AGENTS.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="User authorized all CI/CD repairs and unattended release with human-validation bypass">

## Intent

Fix the read-only release destination verification that falsely treats an unavailable NuGet v3 flatcontainer copy as absence of a genuinely published signed package served by NuGet's official v2 download endpoint. Keep all package-content, source, OCI, GitHub asset, ownership, evidence revocation and deadline gates. The actual v7 release and source/tag are immutable.

## Boundaries & Constraints

Do not move tags, change package versions, republish existing content, weaken validators, skip checks, fake an endpoint response, rewrite approval history or launch business resources. No Git mutations, external writes or workflow dispatch. Root owns commits, review and recovery. Preserve concurrent changes, including Mcp work and root spec updates. Use only the two official public NuGet endpoints with actual returned bytes; no arbitrary mirrors, GitHub substitute or cache injection.

</frozen-after-approval>

## Code Map and Proven Failure

- Owning repository is Root Tenants. Read tracked style/build/configuration and release/recovery guidance before edits. The build workflow has already been applied by Root; implement this handoff without rendering or dispatching another workflow.
- Released source1b2c71b0e3cc5b16745f87aa7f1bc2c0ac8ffc27 passed all CI (25 actual browser cases,671 generic). Release41599005189 accepted all5 NuGet uploads08:02:14Z and published GitHubv7 at08:07:01Z, then failed only destination verification: Contracts v3 flatcontainer remained404 through the600-second wait. Exact log /tmp/tenants-release-20261103-07JciR/tenants7-release-destination-failure.log.
- All5 actual official NuGet v2 URLs https://www.nuget.org/api/v2/package/{id}/7.0.0 redirect to globalcdn.nuget.org and serve matching signed public archives. All5 identity/version/source/hash/ZIP checks and actual dotnet nuget verify --all passed. Read /tmp/tenants-release-20261103-07JciR/tenants7-authentic-public-proof.json and referenced verification/signature log; these are public NuGet bytes, not unsigned GitHub assets. The v3 failure and v2 success are observed, not a hypothetical mirror scenario.
- scripts/verify-release-artifacts.sh:79-105 owns bounded package acquisition before release-package-content.py compares every reviewed content entry. It currently requests only the v3 canonical URL; all later package, OCI, tag, source, GitHub asset and frozen input checks must remain. The fixed600 default/1800 maximum is already finite; keep one shared per-package deadline, with every request bounded by remaining time. Do not give endpoints independent full budgets.
- On v3 HTTP404, try official v2 download within that same deadline. A successful response must still pass the exact existing content comparison. Record actual endpoint/status in safe retained readiness evidence. Do not substitute v2 for a v3 HTTP200 carrying wrong content or unexpected authentication/status errors. If both copies remain absent/unavailable, retain bounded failure and no verified marker. Never hide a malformed/wrong-version/wrong-source archive using another endpoint.
- Inspect other read-only recovery package acquisition paths for this same demonstrated false-absence decision. If they can mark an existing signed v2 package missing and attempt a duplicate push, reuse the same smallest private acquisition seam coherently before making that decision. Preserve immutable archive comparison and every recovery authority gate. Do not add workflows or generalize to unrelated registry/version-floor discovery.
- Existing coverage lives in tests/scripts/test_partial_release_recovery.py and related release artifact/content guard tests. Reuse their owned curl/GitHub/OCI fixtures; no live private registry or app launch. Keep all existing cases and controls. Add meaningful controls for v3-success without v2; v3-404/v2-success accepted only for exact reviewed bytes; both unavailable remains bounded/no success marker; wrong v2 content refuses; v3 unexpected401 and v3 wrong HTTP200 content are not hidden by fallback.

## Tasks & Acceptance

- [x] Official NuGet v2 serves a real fallback only after demonstrated v3 absence, within one finite existing deadline, and existing complete package/source/content verification still decides acceptance.
- [x] Actual endpoint/status evidence remains truthful and no old success marker survives any failed attempt.
- [x] Relevant recovery read-only absence decisions cannot mark an actually available authenticated v2 package missing and attempt to republish it.
- [x] Focused existing/new offline release tests pass without suppressions or weaker expectations; inspect actual diff and report exact files/commands/results and limitations in /tmp/tenants-release-20261103-07JciR/nuget-endpoint-verifier-handoff.json. Full remote CI and actual released-content recovery remain Root's responsibility.

## File List

- scripts/verify-release-artifacts.sh
- scripts/partial-release-recovery-common.sh
- scripts/validate-partial-release-recovery.sh
- tests/scripts/test_partial_release_recovery.py
- tests/scripts/test_release_artifact_verification.py
- .github/workflows/recover-partial-release.yml
- tests/Hexalith.Tenants.Contracts.Tests/PartialReleaseRecoveryWorkflowTests.cs
- tests/Hexalith.Tenants.Contracts.Tests/PartialReleaseRecoveryTests.cs
- references/Hexalith.McpCli

## Verification

Implementation and independent review are complete. Actual signed NuGet v2/v3 archives now match. Current remote CI and publication completion remain Root responsibilities; the historical release/source/tag are unchanged.

## Local Implementation Verification

The three private scripts and two owned test files implement shared-deadline official endpoint acquisition. Focused suites98/98 and the strengthened all-five signed-v2/no-publication control passed; shell syntax and diff checks passed. Exact commands/hashes: /tmp/tenants-release-20261103-07JciR/nuget-endpoint-verifier-handoff.json. Bash5+ supplies EPOCHREALTIME on the owned Ubuntu runner. Concurrent root4574047330bc3f09fbb7dedd2cc69aff7beddb38 independently increased readiness to1800 and repaired recovery solution input initialization; those existing changes are preserved and declared above. Actual public v3 bytes now match all5 already verified v2 archives, while the original failed recovery attempted a full solution build with missing source inputs. Remote review/CI/recovery remain pending.


## Review Triage Log

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| Blind1: independent30-minute windows exceed60-minute job | medium; patch | Five package windows can exceed the enclosing job. Bound acquisition by one cohort deadline as well as each endpoint remainder, preserving zero-wait immediate probes. No new public control is needed. |
| Blind2: unrestricted redirect authority | false | Requests begin only at the two literal official NuGet endpoints. Observed v2 redirects are HTTPS to NuGet-managed globalcdn; no caller credentials are sent, and complete payload/signature/source verification still decides acceptance. No reachable foreign/downgraded redirect was demonstrated; restricting future NuGet-managed CDN names would add an unsupported guard. Existing v3 already followed redirects. |
| Blind3: requested URL versus effective URL | false | The readiness record identifies the requested official registry source and final acquisition status; it does not claim to expose the final CDN URL. This matches the package-source authority used by NuGet clients. Payload/source/signature checks are independent, and emitting arbitrary redirect query strings could expose signed coordinates. |
| Blind4: wall-clock deadline | medium; patch | EPOCHREALTIME is a wall clock and may adjust during elapsed-time enforcement. Use the already-required Python monotonic clock through a private seam; preserve one finite shared budget. |
| Blind5: elapsed test includes unrelated setup | medium; patch | The three-second assertion starts before frozen-input preparation and subprocess setup. Measure controlled acquisition directly, retaining timeout/remainder assertions and end-to-end failure-marker checks. |
| Blind6: exhausted v3 request can authorize missing state if guard regresses | medium; patch | The helper currently resets status before trying v2, but no recovery control protects the v3-404/deadline-exhausted-before-v2 branch. Add that negative authority control. |
| Blind7: stale spec verification sentence | low; rejected | A finding whose fix edits this build specification is rejected by the review workflow. Root updates completion bookkeeping after actual verification. |
| Edge1: submillisecond curl timeout becomes unlimited | high; patch | Reviewer reproduced real curl0.000001 completing after85ms. Guard remaining time below curl's supported1ms minimum rather than issuing an unlimited request. |
| Edge2: interval08 arithmetic bypass | high; patch | Reviewer ran the real verifier with accepted interval08: exit0 and verified marker despite empty NuGet hashes. Normalize accepted decimal controls before Bash arithmetic and assert no package checks can be skipped. |
| Edge claim1: finite deadline not enforced below1ms | high; patch | Same verified timeout root cause as Edge1; preserve this finding separately and group its patch with Edge1. |
| Edge claim2: content acceptance bypassed on08 | high; patch | Same verified arithmetic root cause as Edge2; preserve separately and group its patch with Edge2. |
| Verification gap: losing redirect following is undetected | medium; patch | Pre-verified mutation removed only--location and both v2-success tests still passed. Model redirect-dependent package fulfillment in the owned curl fixture and prove the mutation fails. |
| Verification Other1: mandatory Contracts fixtures regress | high; patch | Direct existing PartialReleaseRecoveryTests execution failed6 of18. The fixture rejects v2 URLs and expects old separate probe/download requests. Its source confirms both mismatches; update transport modeling and exact request expectations without weakening authority checks. |

All retained changes are private deadline arithmetic or focused test-fixture/control corrections. They add no public surface and protect demonstrated states; no intent change or permission escalation is required. Actual remote CI and release completion remain mandatory.


## Review Patch Verification

All retained findings are resolved. Acquisition uses the already-required Python monotonic clock, refuses remaining time below curl1ms, normalizes decimal controls, shares one cohort budget, and requires five completed content checks before success. Zero disables polling with bounded immediate probes. Redirect-dependent fixtures and the existing mandatory C# recovery fixture model the actual acquisition request pattern. Focused artifact30, recovery47, Contracts18, governance6 and validation-context21 controls all pass (122 total); the package-mode build has zero warnings/errors. Removing only--location from isolated script copies rejects both maintained v2 success controls. Exact patch/source hashes and commands are retained in /tmp/tenants-release-20261103-07JciR/nuget-endpoint-verifier-handoff.json; final coordinator logs are nuget-verifier-final-governance.log and nuget-verifier-final-context.log in the same directory. No findings are deferred. The old Bash5+ note above is superseded: final elapsed-time enforcement uses Python monotonic time, not EPOCHREALTIME.
