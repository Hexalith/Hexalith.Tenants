---
title: 'DW-517 evidence content scanning'
type: 'bugfix'
created: '2026-10-14'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '23aeb38f9784cd67b3c2fa66fe1c007799df6826'
---

<frozen-after-approval reason="explicitly authorized DW-517 scope">

## Intent

Restore the EventStore tracked-text secret guard for evidence CTRF JSON, the named `6-1-p1r-3110/verification/` directory and `6-5d-simplification/previous-candidate.md`. These paths currently return before content ingestion. Preserve historical bytes and existing content-bound retirement and sealed evidence. Existing specifications govern retirement; no new or re-pinned exemptions may be introduced just to pass tests.

Implement only DW-517 in EventStore. Preserve the exact password-free GitHub SSH identity exception, legitimate synthetic fixtures, binary recognition and established UI generated-file handling. Exercise actual file ingestion with synthetic injected credentials, benign content, URI credentials, encodings, retirement errors and path boundaries; demonstrate failure under scanning/verification bypass. Complete independent review, focused security tests, the full owning Server project and relevant CI/secret checks. Record failures, existing skips and blockers. Close only this ledger record when proven; update counts/index/readiness and select the next current bundle. Preserve B1–B8, inherited work, external movements, archive, sprint and unresolved decisions. No dependencies/gitlinks, staging, commits, pushes, deployments or production operations are authorized.


**Approved decision (2026-10-14):** Apply the recommended bounded semantic repair for the four source-expression and five deliberate negative/redaction-probe findings. Keep complete ingestion and surrounding detection; reject literal substitutions, credential-bearing URIs, altered/broken fixture contexts and adjacent material. No general parser, blanket test-file/fixture prefix allowance, new file exclusion or retirement is authorized.

</frozen-after-approval>

## Implementation Notes

- Planning: one localized test guard and its ingestion regressions, one owning contract, and required workspace evidence/index updates; no production API or irreversible action. Existing user/spec instructions settle scope and retirement preservation. No intent gap identified. One-shot route selected under step-02.
- Entry Tenants `23aeb38f9784cd67b3c2fa66fe1c007799df6826`, EventStore `24d719292b06d8a9d09771d837f8dd4d8d9b8997`, both clean main. Full status/history/remotes/dependencies and preserved-artifact digests are in `dw-517-entry-state.json`. Those revisions already include B8 and external movements; they are not DW-517 implementation.
- Reverification: `ReadTrackedText` calls `IsExplicitGeneratedPath` before reading bytes; `ExplicitEvidenceArtifactPathPattern` contains all three defective categories and the separately sealed source capture. Retain only the exact sealed source exception, checked by existing SHA256/CRLF semantics. Reuse the same reader with an injectable repository root for isolated file-based tests, retaining the default owning root for Git enumeration.
- Existing governing sources: original Tenants `spec-5-6-preview-confirm-and-link-a-tenant-correction.md` pass-12 BH1; EventStore `spec-5-3-production-authentication-guards-and-secret-stripping.md` requires preserved historical evidence and governed retirement. Runtime persistence is untouched; required Hexalith state baseline was read.

- Owning contract recorded before code changes: `docs/architecture/evidence-secret-scanning.md`. Investigation confirmed 10 CTRF, 1,358 named-verification files and one historical candidate. No approved retirement covers these paths. Original Story5.3 manifest at `df3828740c244d8add3c40ce2bbfd0d2da680b30` verifies the two existing path/hash approval anchors, preventing registry re-pins. Missing OTLP lineage paths are absent/untracked and cannot grant retirement. No exemption/registry content was added or re-pinned.

- Independent Blind Hunter review completed; all five findings patched and rechecked: ambiguous duplicate retirement records now fail; both existing approvals exercise byte/re-pin/missing/no-record handling; unverifiable inherited OTLP records fail attempted ingestion; errors assert expected integrity exception families; fixtures use valid CTRF/JSON/XML/C#/Python syntax. No review work deferred or exemption granted.
- Final new-ingestion suite: 290/290 Debug source and Release package cases, zero failures/skips. Temporary exact-source controls pass290/290; ingestion/scanning/content-binding/approval/seal bypasses fail256/138/2/3/1 cases respectively. Initial harness missing owning NoWarn and JsonReaderException-subclass expectation failures are retained in command evidence; final control carries the existing effective configuration.
- Restored whole tracked guard fails with nine preserved historical findings. No artifact is rewritten, no retirement added, and no synthetic literal exemption created. Asked for the owning contract: retain strict blocker; separately authorize narrow semantic recognition with injected-secret proofs; or review exact governed retirements while active evidence remains scanned. Closure is pending, not release-ready. Full/focused final validation is running.
- Initial DW517 provenance check used the wrong frontmatter key `story_baseline_commit`; corrected to the validator's `baseline_commit` with the unchanged full original entry identifier. B6/B8 historical baselines remain untouched and their inherited five-movement failures remain visible.

## Review Triage Log

| Finding | Verdict / action | Verified evidence |
| --- | --- | --- |
| BH1 duplicate matching records bypass ambiguity | medium / patched | First match returned before contradictory second record; reader now requires at most one matching record and a contradictory-authority regression fails. |
| BH2 only first legacy approval tested | medium / patched | Both approved path types now cover original/changed/re-pinned/missing bytes and missing registry record. |
| BH3 unavailable inherited OTLP records lack explicit control | low / patched | Two real retained registry records fail attempted ingestion; contract documents their absent/untracked originals/lineage without changing their disposition. |
| BH4 broad exception assertions can hide unrelated failure | medium / patched | Each malformed/disposition/approval case requires the relevant integrity exception family; parser subtype is accepted. |
| BH5 generic text does not exercise document structures | medium / patched | Valid CTRF/JSON/XML and executable-language statements, with benign counterparts, run through the same reader/scanner across eight decoder lanes. |

## Code Map

- EventStore `tests/Hexalith.EventStore.Server.Tests/Security/SecretsProtectionTests.cs`: tracked reader/scanner, binary/decoder/seal and governed retirement.
- EventStore `SecretsProtectionTests.EvidenceIngestion.cs`: real file regressions and both approved retirements.
- EventStore `docs/architecture/evidence-secret-scanning.md`: owning contract recorded before implementation.
- `dw-517-entry-state.json`, `dw-517-command-results.json`, `dw-517-mutation-evidence.json`, `dw-517-secret-scan-comparison.json`, `dw-517-validation-evidence.md`: immutable original entry, appended external observations, exact commands, controls and limits.
- Canonical `deferred-work.md` DW-517 and `docs/maintenance-backlog.md`: retain original finding/action/verification, update only this repair's evidence, recompute unchanged counts. `release-readiness.md` receives an append-only dated update; B1–B8, archive/sprint and unresolved decisions stay intact.

## Approved semantic continuation

The user replied `apply recommended` after the detailed option comparison. This resolves the sole policy question in favor of bounded semantic recognition. No retirement or exclusion approval is implied. The historical unresolved discussion and failed gates remain in validation/readiness evidence. Original baseline and frozen user scope are preserved.



Final external movement: EventStore merge `241652627a52e47c189b2b052db407cec172f813`
arrived after validation, preserving parent `24d719292b06d8a9d09771d837f8dd4d8d9b8997`.
It changes owning sprint tracking and recorded Builds/FrontComposer/Memories/
Platform/Tenants pointers; nested dependencies remain uninitialized. This is
external work, not an assistant DW-517 mutation. Original entry and all full
movement identifiers remain in `dw-517-entry-state.json`. Final security source
hashes exactly match tested/mutated bytes; the external diff changes no owning
`src/` or `tests/` files, and effective initialized build dependencies are
unchanged. Prior full/focused results qualify that tested source, without
qualifying external provenance. DW-517's gitlink check initially passed and now
exits1 on the external EventStore checkout movement. No reset, new baseline,
false ownership declaration or attribution bypass was used. Historical B6/B8
failures remain. Tenants B1–B8 artifacts, archive/sprint and1,371 historical
artifact hashes are preserved; the external owning sprint edit is retained.


## Tasks & Acceptance

Implementation handoff scope is the owning EventStore at `references/Hexalith.EventStore`; work on current preserved main, no Git/dependency mutation. Read AGENTS and the permitted workspace baseline already identified in the Code Map; never discover/load skills under references. The existing broad-exclusion repair, byte-bound approval checks and 290 regressions are authorized work to preserve.

- [x] `references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Security/SecretsProtectionTests.cs` -- retain full ingestion; add bounded content recognition for the nine documented historical triggers. May put same-type partial helper code/tests in focused `SecretsProtectionTests.*.cs` files. Existing SourceExpression/GetEffectiveSourcePath helpers are the first reuse candidates. Do not add content/hash exemptions simply to silence findings.
- [x] `references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Security/SecretsProtectionTests.*.cs` -- file-ingestion regressions for real original triggers, literal/URI/adjacent substitutions, broken/missing rejection/redaction proof, operational use of inert markers, Markdown shell Python here-doc boundaries and decoder variants. Every test invokes `ScanTrackedFile` with real isolated bytes, not just the recognizer.
- [x] `references/Hexalith.EventStore/docs/architecture/evidence-secret-scanning.md` -- approved contract above is authoritative; append precise implemented boundaries after source changes, preserving initial contract/history.
- [x] Narrow build/tests -- Debug source and Release package builds with unchanged pinned dependencies and warnings policy; direct xUnit `-method`/`-class` focused cases. Record exact commands and any failure; full owning/focused security, independent review/mutation/CI/governance and ledger qualification are coordinated by parent after implementation report.

Acceptance:
- Given each historical benign source/probe, when ingested, then its noncredential expression is recognized and surrounding content remains scanned.
- Given a static/fallback credential or credential URI, changed inert marker, broken proof context or adjacent injected assignment, when ingested in any formerly excluded category, then detection fails validation.
- Given malformed/unterminated here-doc or fixture syntax, when ingested, then no benign-context recognition conceals credential content.
- Given unchanged approved retirement/seal/binary/UI handling, when old and new tests run, then the existing contract stays intact; a bypass still makes controls fail.
- Given final passing owning tests/review with remaining release blockers explicitly recorded, then only DW-517 may close and maintenance counts/index/readiness recompute. Parent owns closure; implementation agent must not edit canonical ledger/index/readiness or snapshots.

Implementation notes are append-only. Return a concise report of changed source, focused verification and unresolved risks; do not stage, commit, push, update pointers/dependencies, edit historical evidence, initialize nested dependencies or claim full release readiness.

- Semantic implementation handoff: bounded expressions/here-docs/probe contexts implemented in `SecretsProtectionTests.EvidenceRecognition.cs`, with244 real-semantic/source-ingestion cases in `EvidenceSemanticIngestion.cs`; both source/packaged lanes pass534/534 including preserved290 cases. The earlier EvidenceIngestion local `secret` was renamed `injectedBytes` so its own newly added source-ingestion check passes. Initial53/8/1 boundary/self-source failures are retained. No current failure remains in the focused handoff.
- Independent-review diff is the complete owning source/contract delta from original EventStore entry `24d719292b06d8a9d09771d837f8dd4d8d9b8997`, including all four scanner source files and untracked additions. External pointer/sprint movements stay in separate full entry observations, without attribution to this repair. Parent verifies source rather than relying on handoff report; original Tenants baseline remains unchanged.


### Semantic review pass 1 triage

All three layers returned before triage. The following individual findings are verified through current code and the reviewers' exact-source reflection/mutation probes; patch within the approved bounded-recognition contract. No new policy choice is needed. Historical broad exclusions remain removed.

| Finding | Verdict / route | Evidence |
| --- | --- | --- |
| SB1 arbitrary quoted account tuple | high / patch | Quoted operational `send` text passes the extra tuple-string branch without the approved complete mutation shape; remove that branch. |
| SB2 opposite-quote interpolation boundary | high / patch | Runtime interpolation followed by an opposite quote and static suffix is recognized; require end-of-body or an actual field separator. |
| SB3 nested/fake here-doc restart | high / patch | Regex rejects outer input then restarts at an inner opening, resetting lexical context for static string data; require top-level enclosing shell ownership. |
| SB4 Python keywords accepted as runtime references | medium / patch | `class`/other keywords match identifier grammar and grant recognition to malformed source; reject reserved segments. |
| SB5 unmatched preceding delimiters | medium / patch | An incomplete preceding expression lets a standalone tuple assignment appear valid; check supported source-context delimiter state. |
| SB6 rejection helper prefix only | high / patch | Extra operational retention/send statement after a blank line leaves proof prefix intact and keeps probe masked; require complete helper. |
| SB7 duplicate probe override | medium / patch | A later same-name `pass` method overrides the matched proof; require one definition. |
| SB8 decorators ignored | medium / patch | Skip/replacement decorators disable the matched test but keep masking; reject unsupported decorators. |
| SB9 proof-symbol shadowing | high / patch | Replaced TestCase/assertion symbols still establish matching context; reject demonstrated local overrides. |
| SE1 quoted tuple credential | high / patch | Independently reproduced same SB1 operational string case; shared root cause. |
| SE2 inner quoted/nested here-doc | high / patch | Independently reproduced same SB3 lexical-reset case; shared root cause. |
| SE3 extended rejection helper | high / patch | Independently reproduced same SB6 forwarding/retention case; shared root cause. |
| SE4 decorated/overridden probe | medium / patch | Two independently reproduced roots SB7/SB8; both need controls. |
| SE5 claimed literal/proof protection not established | high / patch | Claim consequence of SB1/SB6, resolved by their implementation/controls rather than changing the user intent. |
| SE6 claimed nested boundary protection not established | high / patch | Claim consequence of SB3; preserve whole scanning and correct ownership. |
| SG1 missing interpolation suffix control | medium / patch | Removing boundary lookahead survives all244 semantic cases; add direct suffix/opposite-quote ingestion and a widening mutation. |
| SG2 rejection helper retention | high / patch | Verified additional write/send after fixed prefix returns no findings; same SB6 root. |

Parent initial whole-security gate now removes all nine historical findings but exposes three maintained Python source findings: `tools/p1r_published_executor.py:693` is a parenthesized multi-line runtime f-string; the two maintained unit-test sites `tools/tests/test_p1r_published_executor.py:390` and `tools/tests/test_p1r_published_qualification.py:92` have fixed synthetic negative rendering values. Preserve the existing runtime and test behavior: support the bounded runtime expression, and use runtime-generated fixture values in these ordinary mutable unit-test sources rather than adding recognition/exemptions for more fixed credentials. These are maintained source files, not immutable historical captures. Run their narrow unit cases and keep the same invalid-packet/rejection assertions. This is necessary follow-through on the strengthened literal check; no historical evidence edit or new gate exclusion is permitted.


### Correction recheck triage

All three correction reviewers completed before triage. Every original reproduction is corrected; six independently filed residual findings below are verified against the current private recognition code and route to bounded patches. The initial final broad commands are superseded by this corrective source revision, not claimed as closure proof.

| Finding | Verdict / route | Evidence |
| --- | --- | --- |
| RB1 whitespace before duplicate method opening | high / patch | Exact `def NAME(` counters omit valid spaced duplicate probe/helper definitions, which Python permits to override the originals; count supported whitespace and exercise ingestion. |
| RB2 annotated proof-symbol override | high / patch | Proof-shadow assignment pattern requires `=` immediately after whitespace and misses an annotated override of a used assertion; include demonstrated annotated assignment and a direct ingestion control. |
| RB3 open owning mutation list | medium / patch | The `faults = [` exception checks only its prefix and grants recognition without the matching closing list; require bounded closure and reject removed `]` through ingestion. |
| RB4 adjacent two-line literal extends credential | high / patch | At the first f-string body's end, the current boundary rule qualifies even though the allowed adjacent database tail appends to that credential; require a field separator before appended content and test the missing separator. |
| RE1 spaced duplicate probe/helper definition | high / patch | Independently traced same RB1 counters and valid Python overwrite; shared root, each report retained. |
| RE2 unterminated mutation list | medium / patch | Independently traced same RB3 prefix exception; original malformed-prefix controls do not assert the missing closing bracket. |

Verification-gap reviewer rechecked both earlier findings, read the full frozen diff and ran the three relevant owning ingestion methods:24 passed/zero failed/skipped. No missed correction in those two findings. Original boundary-survivor evidence remains preserved; new widening mutation now fails tests.


### Final proof-shadow boundary correction

All three final correction layers returned. Blind and verification-gap layers
confirmed RB1–RB4 through22/91 focused cases. Edge confirmed RE1/RE2 through14
focused cases and filed one additional verified annotation-boundary finding.

| Finding | Verdict / route | Evidence |
| --- | --- | --- |
| RE3 quoted-semicolon proof annotation | high / patch | Reflection against current Release reports masked=true/zero findings for `self.assertNotIn: 'x;y' = lambda *args: None`, versus masked=false/a finding for the object annotation. The bounded annotation parser excludes semicolon and misses a valid override. Conservatively veto proof-symbol annotations on colon, including the class-qualified check; direct ingestion must reject unknown annotation expressions without parsing them. |

The11-mutation576-case proof applies to the preceding frozen revision and stays
recorded. The narrow source correction will receive fresh owning/focused/full
and mutation qualification; the broad commands currently running are
intermediate, not final closure evidence.


### Final independent correction result

All three final correction reviewers returned: Blind22, Edge10 plus exact
reflection, VerificationGap87 focused cases pass with zero failures/skips.
The quoted-semicolon repro now has masking disabled and a finding; the clean
inspection probe remains recognized. Equality checks and earlier RB1–RB4/RE1–RE2
controls survive. Every one of29 individual findings across the initial/semantic
reviews and correction rounds is retained, patched and independently rechecked;
no review work is deferred and no policy decision remains unresolved for DW517.
Other ledger decisions and release holds remain unchanged.

Final helper hash `53a9c5f7b3421f7c4fff7308dff9627495fbdb5e57898db354450cd29827821c`;
final semantic-test hash `539c92a8246bbf22277dbb3d18bfc11942ef3a2d37ad87ecd1fec88ba666336e`.
The owning contract is append-only over its original pre-implementation boundary.
The additional code map comprises the two EvidenceRecognition/EvidenceSemanticIngestion
partials and maintained `tools/tests/test_p1r_published_executor.py` /
`tools/tests/test_p1r_published_qualification.py` runtime fixtures.
Final576 ingestion cases pass in both real owning configurations; unchanged
exact-source control passes and eleven deliberate bypasses fail assertions.
Final focused security/full Server qualification is running after correction review.

The intermediate full pass followed by an unchanged randomized NuGet negative
fixture failure are both retained; affected ten-case focused rerun passes without
a code/policy change. This pre-existing fixture instability is a qualification
limitation, not silently attributed to DW517 or removed from a gate.


## Final completion

Only DW517 closes after all independently reviewed source controls, final owning
Debug/Release576-case ingestion,778-case security and unfiltered5265-case Server
qualification (5240 pass/zero fail/25 unchanged skips). Eleven deliberate bypasses
fail; raw Gitleaks/provenance and all other release holds remain recorded.
Counts/index recompute to319 done/324 open,115 reconciled done/236 actionable,
288 exact-covering rows. Original baseline, every unrelated ledger record,
B1–B8/archive/sprint, external commits/pointers and historical artifacts remain.
User instruction forbids staging/committing/pushing and overrides the workflow's
local-commit step; no commit or sprint synchronization runs for this deferred item.
Final commands, review/mutation digests, initial failures and limits are in
`dw-517-validation-evidence.md` and its linked durable JSON evidence.
