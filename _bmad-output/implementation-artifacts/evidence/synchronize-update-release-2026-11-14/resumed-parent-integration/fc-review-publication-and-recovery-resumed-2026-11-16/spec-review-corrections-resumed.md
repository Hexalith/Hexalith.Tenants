---
title: 'Resume publication and recovery corrections review'
status: done
route: oneshot
baseline_commit: '292dbf164f26bcf376ef86d749b149b075f8b502'
owner_revision: 'bc9dbedc6141b750ca92f01b40ac840dc2066704'
resumed_from: '_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/resumed-parent-integration/fc-review-publication-and-recovery/retained-original-spec-review-corrections.md'
---

## Intent

<frozen-after-approval>
Implement the parent review corrections with the smallest owner-local changes. Gate segment and aggregate publication using the exact existing normalization and unsafe-content policy; preserve safe failed reports and all strict BOTH/provenance/cohort/threshold/protection behavior. Strengthen the five named regression classes with the actual renderer identity, badge/fallback/declaration ordering, packed template versions, unaffected full descriptor and repaired-to-damaged same-driver recovery contracts. Preserve every existing assertion and test body except necessary additions. Use narrow publication refusals and the five edited test classes only. No full mutation, accepted triage, Git/remote or authority edits. Preserve previous packets/receipts and all unrelated owner changes.

Given a safe failed report, when publication is attempted, then normalization and exact unsafe-content scanning succeed and publication remains eligible while the failed segment stays failed. Given actual unsafe content, when either publication guard runs, then upload is ineligible. Given repaired source, when the same driver receives its original damaged source, then original diagnostics and fallback/suppressed output return and the healthy sibling is unchanged.
</frozen-after-approval>

## Implementation Notes

The supplied file is a retained historical spec, already in review. Its original packet and status remain untouched. This continuation resumes the rendered bmad-build review stage and records the review separately. No implementation changes are required: the five named regression classes are byte-identical to the retained corrections, while later publication repairs are already integrated in the current owner revision above.

Three independent reviewers examined the retained 35,755-byte correction diff, SHA256 `908a170279548db799fba05e4e9fb30356c409636c1ffa940dd0172ba7fad325`. Every finding below was then checked individually against current source and applicable current tests. A `false` verdict here means the reported historical defect no longer occurs in current source; it does not dispute the reproduced historical observation. Raw reviewer results are preserved alongside this spec.

The original Normalize-ArtifactText and Test-UnsafeText function texts still match their pre-extraction definitions after line-ending normalization. Current JSON decoding, serialization, unchanged-byte preservation and original/published digest bindings wrap that same string policy. Existing BOTH/provenance/cohort/threshold/job-result controls remain in place.

## Verification

- `run-with-temp python3 -B .../run-focused.py` archived the full current owner revision and its exact Builds gitlink into a disposable workspace. Its recorded Debug build uses the normal analyzers and project references: zero warnings, zero errors.
- The five named regression classes passed together against that Debug build: 53 passed, zero failed, zero skipped. Commands and source archive identities are in `verification.json`; results are in `test-results/tests.trx`.
- The nine original publication controls passed. Six later repair controls also passed, specifically checking JSON decoding and path preservation, idempotent bytes/audit hashes, original/published bindings and missing-capture refusal. These 15 tests executed 31 real PowerShell processes: 11 successful, 20 refused.
- All 192 files in the retained packet are byte-identical to the initial manifest. The owner HEAD and clean working tree are unchanged. The test and review scratch workspaces were removed after all children completed.

These are scoped local results. Full mutation scores and hosted artifact upload qualification remain outside this continuation.

## Review Triage Log

| ID | Layer | Historical claim | Verdict | Current-source evidence and route |
| --- | --- | --- | --- | --- |
| BH1 | Blind hunter | Escaped Windows user path survives raw JSON scan. | false | `mutation-artifact-policy.ps1:49` decodes the string before normalization and writes it through Utf8JsonWriter. `testJsonWindowsPathIsRedactedAndRemainsValid` passed; reject as already resolved. |
| BH2 | Blind hunter | A terminal home path consumes JSON closing syntax. | false | Current `Get-PublicationArtifactText` parses JSON before applying the unchanged regex to individual strings and serializes valid delimiters. `testJsonTerminalHomePathKeepsDelimitersAndIdempotentBytes` passed; reject as already resolved. |
| BH3 | Blind hunter | Quoted credential property names evade the scan. | false | `mutation-artifact-policy.ps1:35` scans each decoded property name plus `=` with the original unsafe policy, so a password property is refused. The historical serialized-text-only path is absent; reject as already resolved. |
| BH4 | Blind hunter | A Unicode escape hides a decoded password marker. | false | JsonDocument decoding precedes `Test-UnsafeText` at policy line 52, including escapes inside the marker. The current escaped-credential refusal control passed; reject as already resolved. |
| BH5 | Blind hunter | Repeated preparation appends LF and invalidates OutputSha256. | false | `prepare-mutation-artifacts.ps1:114` writes only changed content with WriteAllText. The actual-validator control passed with report/audit bytes and digest stable across two aggregate preparations; reject as already resolved. |
| BH6 | Blind hunter | Worker normalization destroys original input digest identity. | false | Preparation captures OriginalSha256 before writes at line 105, records original/published bindings at line 122 and verifies them before aggregate validation. The real capture/binding and missing-capture controls passed; reject as already resolved. |
| EC1 | Edge case hunter | JSON quoting/escaping conceals credentials or local paths. | false | The current decoded property-name and string scans replace the historical serialized-text-only scan. Windows path and escaped-credential controls passed; reject as already resolved. |
| EC2 | Edge case hunter | Terminal home-path redaction makes malformed JSON eligible. | false | Current JSON-aware normalization owns serialization and preserves delimiters. The terminal-home-path control passed with valid parsed JSON and stable subsequent bytes; reject as already resolved. |
| EC3 | Edge case hunter | Aggregate preparation changes audited report bytes. | false | Current preparation avoids unchanged writes and appends no newline. The real validator/audit-hash control passed across two preparations; reject as already resolved. |
| EC4 | Edge case hunter | Pre-normalization original hashes become unrecoverable. | false | Current original/published capture preserves the original report digest and verifies published input bindings before validation. Both binding and missing-capture controls passed; reject as already resolved. |
| EC5 | Edge case hunter, claims pass | Escaped password content uploads after a successful guard. | false | Both current publication modes call the JSON-aware shared policy, whose decoded string scan refuses the password marker. The escaped-credential and either-segment aggregate refusal controls passed; reject as already resolved. |
| VG1 | Verification gap reviewer, other finding | Escaped credential has no effective publication regression. | false | The current `testJsonEscapedCredentialIsDecodedAndRefused` exists and passed against the actual guard. The reviewer explicitly identifies this as historical; reject as already resolved. |
| VG2 | Verification gap reviewer, other finding | Terminal home-path JSON corruption lacks effective coverage. | false | The current `testJsonTerminalHomePathKeepsDelimitersAndIdempotentBytes` exists and passed against the actual guard. The reviewer explicitly identifies this as historical; reject as already resolved. |
| VG3 | Verification gap reviewer, other finding | Audit byte stability is untested and repeated writes remain unsafe. | false | The current `testActualValidatorAuditHashSurvivesIdempotentPublication` exists and passed using the actual validator and repeated preparation. The reviewer explicitly identifies this as historical; reject as already resolved. |

All 14 findings were individually triaged before grouping. No current findings survive; no patch, loopback, or deferred-work entry is required. No accepted mutation triage or authority is changed.

## Completion Notes

Only this new local review packet was written. The original spec, previous packets, product files and unrelated concurrent work are preserved. No branch, commit, push, merge, dependency update, submodule pointer change or /pushall occurred. The frozen intent's explicit Git/remote restriction overrides the workflow's default local-commit step.

An initial run-helper path calculation exited before verification began; the corrected helper and successful commands are retained in this packet. This has no bearing on the passing product results.
