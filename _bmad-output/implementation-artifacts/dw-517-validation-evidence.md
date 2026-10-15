# DW-517 evidence content scanning — 2026-10-14

**Final assessment: DW-517 closed; overall release not ready.** See the final
closure evidence below. The initial assessment and all failures remain historical.

## Historical initial qualification

**The scanning repair is implemented and independently reviewed. DW-517 remains
open; release is not ready.** The restored guard reports nine historical
findings requiring an owning content-classification or approved-disposition
contract. No new retirement, re-pinned exemption or historical evidence rewrite
was used to make a gate pass.

## Entry, ownership and preservation

[Entry and external observations](dw-517-entry-state.json) record clean `main`
entry in Tenants `23aeb38f9784cd67b3c2fa66fe1c007799df6826` and EventStore
`24d719292b06d8a9d09771d837f8dd4d8d9b8997`, full remotes/history, unstaged/staged
state, and all recorded/checked-out dependency identifiers. EventStore was
already ahead/behind its remote. Those revisions include B1–B8 and external
work; none is attributed to this bundle. During this run an external Tenants
commit `51f404513f2657e98f949f58f0df43b99b68a7d0` recorded the initial entry-state
file. The original snapshot remains intact and later observations are appended.
No assistant branch, stage, commit, push, dependency or umbrella pointer mutation
ran. Nested EventStore dependencies remain uninitialized. No deployment,
production replay or data migration ran.

Hashes cover 1,371 preserved historical files: 1,369 formerly excluded paths,
the retirement registry and the sealed capture. B1–B8 evidence, their existing
specs, archive and sprint records are also checked. Existing B5 checkout EOLs
match the normalized baseline; no normalization rewrite was performed.

Historical B6 and B8 story-gitlink commands still fail on five inherited
Builds/EventStore/FrontComposer/Memories/Platform movements. DW-517's check
passes from its unchanged original entry revision to current source, with no
pointer movement. The historical failures remain release provenance blockers;
no reset, narrowed historical baseline or invented ownership declaration ran.

## Owning contract and change

The [owning scanning/retirement contract](../../references/Hexalith.EventStore/docs/architecture/evidence-secret-scanning.md)
was recorded before code implementation. The original DW-517 source spec's
pass-12 finding remains independent of its exact password-free GitHub SSH
identity decision. Source reverification found the broad exclusions still
present: ten CTRF reports, 1,358 named-verification files and one historical
candidate file returned before content reading. None has a verified retirement
approval. The archive decision preserves historical provenance, without granting
secret-scan retirement.

`ExplicitEvidenceArtifactPathPattern` is removed. The reader retains the exact
existing sealed remediation capture with its unchanged SHA256/CRLF verifier,
and the existing explicit UI generated-file handling. `ScanTrackedFile` is
shared by Git-tracked validation and isolated regression files. Existing
binary signatures, decoding/recovery, synthetic recognition and the exact SSH
identity exception are unchanged.

The effective Story 5.3 retirement approvals retain their exact original paths
and hashes from immutable commit
`df3828740c244d8add3c40ce2bbfd0d2da680b30`. The mutable registry cannot approve
new content by re-pinning. Schema, disposition, non-authority, reason, approved
hash, original bytes and unique matching record must verify before a skip.
The two inherited OTLP dispositions remain in the registry, with unavailable
originals/authorization lineage and no usable exclusion in this checkout.

## Regressions and review

290 new ingestion cases pass in Debug source and Release package mode, zero
failures/skips. Real temporary files exercise CTRF/JSON/XML/C#/Python/text,
benign placeholders and the exact SSH identity, assignments and credential URIs,
eight UTF-8/UTF-16/malformed/control decoding lanes, path boundaries, binary/UI
handling, both verified retirements, changed/re-pinned/missing bytes, missing
records, malformed/missing registry fields, contradictory duplicate records,
unapproved paths, unavailable inherited records and missing/changed seal bytes.
The owning historical files are untouched.

The configured context-free Blind Hunter review returned five findings. Each
is individually triaged in the [spec](spec-dw-517-evidence-content-scanning.md),
patched and independently rechecked: no missed correction remains. No review
finding was silently dropped, deferred or used to grant approval.

[Mutation evidence](dw-517-mutation-evidence.json) records exact-source temporary
harness setup and hashes. Its final unchanged control passes 290/290. Deliberate
ingestion/scanning/retirement-content/retirement-approval/seal bypasses fail
256/138/2/3/1 cases respectively. The harness rebinds only repository root and
mirrors the owning effective build configuration and existing resolved package
versions; it does not replace owning-project validation or modify production
files.

## Exact commands, results and failures

[Command results](dw-517-command-results.json) retain full argv, working directory,
exit code, duration and safe output for every validation command. Diagnostic
logs are under `/tmp/hexalith-dw517-20261014/`; durable results do not rely on
that directory being retained. Both final `dotnet build` commands for the
owning Server project use `-warnaserror -m:1 -nr:false`: Debug with
`-p:UseHexalithProjectReferences=true`, Release with
`-p:UseHexalithProjectReferences=false`. Each exits 0 with zero warnings/errors.
Direct xUnit v3 `-method ...EvidenceFileIngestion_*` runs pass 290 cases in both
configurations. No project-level VSTest filter is substituted for MTP.

Initial cached Release security run: 202 cases, one existing randomized
NuGet-metadata failure. A fresh Debug focused rerun passes all three relevant
cases; no unrelated fixture or gate was edited to hide the initial result.
Initial restored Debug main scan exits 1 with the nine historical findings
below. Review-era build caught missing public-parameter validation (`CA1062`),
which was corrected. Initial temporary harness lacked the owning existing
`NoWarn` policy (`CS8604`); the final harness mirrors that existing policy,
without changing the repository gate. An overly exact malformed-JSON assertion
rejected `JsonReaderException`, a `JsonException` subtype; the corrected test
requires the expected integrity exception family. Intermediate broad/focused
runs were explicitly cancelled after that known assertion failure and supply
no completion evidence. The final runs use corrected source.

The first new story-gitlink check failed because the spec used
`story_baseline_commit`; the field was corrected to the required
`baseline_commit`, preserving the exact full entry revision. The corrected
DW-517 command passes; historical B6/B8 commands still fail.

Final focused security run: 492 cases, 491 passed, one failed, zero skipped.
The only failure is `TrackedReusableContent_DoesNotContainUsableSecrets`,
reporting these preserved paths/lines:

| Artifact | Lines | Inspected content |
| --- | --- | --- |
| `6-5d-simplification/previous-candidate.md` | 2593, 9269 | Python account-key tuple and its historical mutation literal |
| `evidence/6-1-p1r-3110/verification/run_verification.py` | 1526, 1571 | Runtime password composition and interpolated connection expression |
| `evidence/6-1-p1r-3110/verification/test_run_verification.py` | 94, 114, 1707, 1712, 1764 | Literal negative/redaction probes and synthetic captured environment |

These are scanner findings, not a claim that nine operational credentials were
reproduced. The current guard intentionally still rejects them; classifying
historical synthetic/runtime content or retiring approved obsolete evidence
requires the owning contract. The pending choices are strict scanning with the
findings retained as blockers, a separately authorized narrow semantic repair
with injection controls, or exact governed retirement review while active
evidence remains scanned. Elapsed time does not approve any choice.

Full corrected Server command exits **2**: **4,979 total,4,953 passed,one
failed,25 skipped**,6m49s. The only failure is the same tracked-content guard
and the same nine findings. [TRX counter/failure/skip summary](dw-517-full-server-summary.json)
records every existing skipped test; all25 are preserved DW1 ATDD placeholders.
No test selection, exclusion, floor or production gate was weakened. The focused
and full lanes are failed qualification, despite the passing290 new controls.

## Secret scan, CI and release limits

Pinned Gitleaks 8.21.2 was downloaded only to `/tmp`; its release archive checksum
was verified. With the unchanged `.gitleaks.toml`, exact current tracked files
plus the three DW-517 additions were exported without submodule contents. Raw
Gitleaks exits 1 with 1,561 findings. Entry-source comparison also exits 1 with
1,561; the path/rule/redacted-match multiset is identical. The final three-file
check exits 1 with the single unchanged curl scanner fixture. [Comparison
metadata](dw-517-secret-scan-comparison.json) retains counts and report digests.
No exclusion was added. These are red checks, not passing security gates.

Source checks confirm Server.Tests remains in the unfiltered deterministic CI
caller. The current checked-out EventStore/Builds workflow and composite-action
YAML contain no executing Gitleaks/secret-scan reference, despite historical
configuration comments describing that job. This inherited CI wiring gap is
recorded; no gate or workflow is altered by DW-517. Current hosted execution of
this uncommitted source is unproven; no push was authorized.

The required isolated Aspire start times out after 60 seconds and exact-target
`aspire describe` reports no running AppHost. It is not topology/provider proof.
The original B8 nested-layout blockers and historical provenance failures remain.
Existing seven authority/recovery/release-governance decision holds, applicable
hosted TLS/browser/upstream checks, current NFR10/performance, production
provider/revocation/authority, authenticated browser/clipboard/denial and human
assistive-technology qualification remain unresolved. No DW-517 result establishes
full release readiness.

## Ledger and next bundle

Only DW-517 evidence/action fields change, preserving its original source,
legacy detail, pre-repair verification/action and open/actionable disposition.
Recomputed counts are unchanged:643 IDs,318 done/325 open,289 exact-covering
index rows;114 reconciled done,237 actionable,15 blocked,26 awaiting-decision,
nine accepted limitations and38 uncertain. No archive move, sprint edit or
unrelated record/disposition change ran. The next independent implementation
bundle selected from the current ledger is **DW-105/DW-108**, shared DOM-safe
identity encoding. DW-517 remains P1 pending its owning content-contract decision
and passing gates; this next selection grants no waiver or release approval.

The user requested a deeper practical comparison of the three contract options,
not approval of any option. No retirement or recognition policy is inferred
from that request. A bounded semantic repair is the recommendation because the
nine inspected lines comprise four source-expression findings and five deliberate
negative/redaction probes; blanket fixture literals and file exclusions remain
unacceptable. Governed retirement should be reserved for genuinely obsolete,
non-authoritative artifacts with explicit approval, not active verification code.


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


## Explicitly approved semantic continuation

The user replied `apply recommended`, resolving the sole owning policy choice in
favor of bounded semantic recognition. This approves compact recognition of
nonliteral source expressions and exact rejection/redaction probes while every
evidence file remains ingested. It grants no retirement, hash re-pin, path
exclusion or general fixture-marker allowance. Historical bytes remain preserved.

The first semantic handoff passed534 ingestion cases but the whole owning guard
exposed three maintained Python source sites. The complete two-line runtime
composition is now recognized within bounded syntax; two ordinary maintained
negative unit fixtures use UUID-generated values with unchanged invalid-packet
assertions. Their two focused Python methods pass. These are maintained source
edits, not historical evidence edits. Initial whole-security736 cases had735
passed/one guard failure; full Server5223 had5197 passed/one failed/25 unchanged
skips. All nine original historical findings had cleared; the three maintained
sites were the sole remaining guard findings. Exact commands and output are
retained in `dw-517-command-results.json`.

Three independent semantic review layers filed17 findings, each retained and
triaged in the spec. Their fixes and42 added cases produced576 combined
ingestion cases passing in both owning Debug/Release lanes, zero failures/skips.
The first review-round narrow run's11 failures (damaged first-line context and
legacy fallback behavior) and corrected286/286 semantic run are retained.
The owning security class then passed778/778 and full Server passed5240/5265
with zero failures and the same25 named DW1 ATDD skips. These are intermediate
results: correction recheck found four further roots in six individual reports
(spaced duplicates, annotated proof overrides, list closure and adjacent-string
boundary), requiring a new source revision and final qualification. None is
silently deferred or hidden behind those passing runs.

The intermediate exact-source mutation control passes576/576. Nine deliberate
bypasses produce318/337/2/3/1/12/10/4/92 assertion failures respectively for
ingestion, scanning, retirement content, retirement approval, seal, interpolation
boundary, probe verification, here-document ownership and disabled recognition.
All variants compile; none substitutes a compiler failure for detection proof.
The original290-case/five-bypass proof and the independently discovered surviving
244-case interpolation-boundary mutation remain historical evidence. New phases
and scripts are retained in `dw-517-mutation-evidence.json`.

The intermediate final export contains17635 tracked files plus all authored
scanner partials and the owning contract. Gitleaks8.21.2 still exits1 with1561
findings, matching the entry path/rule/redacted-match multiset: zero added or
removed findings. `dw-517-configuration-preservation.json` verifies eleven
scan/CI/build/config files unchanged from entry and the owning sprint exactly
matching the external merge. Original B1–B8 readiness text remains a byte prefix;
all protected and1371 historical artifact hashes match. Story-gitlink checks
still fail: DW517 on the external EventStore checkout, B6/B8 on five inherited
movements. No pointer reset, declaration of bundle ownership, baseline narrowing,
nested initialization or Git/production mutation ran. Final source qualification
will be appended after all residual controls and independent rechecks pass.


## Final DW-517 closure evidence — 2026-10-14

**DW-517 is closed in the validated working tree. Release remains not ready.**
The user's `apply recommended` resolved the owning decision: every textual
evidence path is ingested, with compact semantic recognition of bounded runtime
expressions and exact inert probes carrying rejection/redaction proof. Unknown
proof annotations, broken boundaries, duplicates/decorators/shadows, changed
markers, literal operands and credential-bearing URIs receive ordinary scanning.
No new path exclusion, retirement, hash re-pin or historical rewrite was created.
The original exact SSH identity, seal, binary/UI handling and both verified
content-bound Story5.3 approvals remain intact.

Final Debug source and Release package builds pass with zero warnings/errors.
Real owning ingestion passes576/576 in each configuration, with zero skips.
Final security is778/778 passed, zero failures/skips (`dw517-closure-security`).
Final unfiltered owning Server is5240 passed/zero failed/25 unchanged DW1 ATDD
skips,5265 total,exit0 (`dw517-closure-server`); all25 skip identities match the
original full-project result. The two changed maintained Python unit methods pass
with the same invalid-packet assertions and runtime-generated fixture values.
[Exact argv, cwd, outputs and all intermediate failures](dw-517-command-results.json)
and [full-project counters/skip identities](dw-517-full-server-summary.json) are
durable. The final commands use the unchanged build/warnings/gates and existing
package versions; no nested submodule was initialized.

The final exact-source control passes576/576. Eleven deliberately weakened
variants all compile and fail assertions: ingestion318, scanning337, retirement
content2, approval3, seal1, interpolation boundary12, probe verification10,
adjacent-tail boundary8, owning list closure1, here-document ownership4 and
disabled recognition92 failures. Final source/variant digests, setup scripts,
exact command indices and earlier mutation phases remain in
[mutation evidence](dw-517-mutation-evidence.json). This also closes the
independently demonstrated boundary mutation that previously survived all244
semantic cases; its initial survival is retained rather than hidden.

Independent Blind/Edge/VerificationGap review and subsequent correction rechecks
resolve every29 individually triaged finding. Final narrow reviewer runs pass
22/10/87 cases; exact reflection shows the quoted-semicolon override is detected
and the unchanged clean probe still qualifies. No finding is deferred, no DW517
policy decision remains open, and final source hashes match reviewed/built/mutated
bytes. The [spec and complete triage](spec-dw-517-evidence-content-scanning.md)
retain every report and correction.

Initial/intermediate failures remain visible: randomized NuGet negative-fixture
failures,290-case repair exposing nine historical findings, initial semantic
53/8/1 failures, review-round11 context/fallback failures, and the intermediate
full-project randomized `[8.14.0]` negative-fixture failure. That unchanged
ten-case method passes its focused rerun; final security/full-project runs also
pass. The random failing value was not retained, so no deterministic root-cause
claim is made; fixture instability remains a qualification limitation. Initial
closure-count validation also failed before any ledger write because older
closed entries lack supplementary disposition fields; recomputation correctly
uses their preserved done statuses and the204 earlier closed-record boundary.
No unrelated record or test policy was changed to satisfy either validation.

Final Gitleaks8.21.2 exits1 with1561 findings across17635 exported tracked files
plus authored scanner/contract additions. Its path/rule/redacted-match multiset
is exactly identical to entry: zero new findings or exemptions. The raw gate
remains a release blocker. Current inspected EventStore/Builds CI YAML has no
executing Gitleaks reference despite historical config comments; wiring stays
unchanged. [Comparison](dw-517-secret-scan-comparison.json) and
[eleven unchanged scan/CI/build configuration files](dw-517-configuration-preservation.json)
record that boundary; no hosted execution of uncommitted source is claimed.

Original entry Tenants `23aeb38f9784cd67b3c2fa66fe1c007799df6826` and EventStore
`24d719292b06d8a9d09771d837f8dd4d8d9b8997` remain unchanged baselines. Preserved
external heads are Tenants `51f404513f2657e98f949f58f0df43b99b68a7d0` and EventStore
`241652627a52e47c189b2b052db407cec172f813`; complete branches, working/index
state and recorded/checked-out pointers remain in the entry observations. All
1371 historical artifact hashes, B1–B8 evidence, archive and Tenants sprint match
entry; owning sprint exactly matches the external merge. DW517 gitlink validation
still exits1 on that external checkout movement, while B6/B8 checks retain their
five inherited failures. No pointer reset, baseline narrowing or false bundle
ownership attribution was used. No assistant staging, commit, push, branch,
dependency update, deployment, production replay or data migration ran.

Only DW-517 closes:643 IDs remain,319 done/324 open,288 index rows covering every
open ID exactly once. Reconciled counts are115 done/236 actionable/15 blocked/
26 awaiting-decision/9 accepted-limitation/38 uncertain; older closed records
without disposition fields stay intact. [Ledger/index verification](dw-517-ledger-validation.json)
confirms every unrelated original-entry record is unchanged. Existing seven
authority/recovery/governance decisions, provenance, raw secret-scan/CI, nested
layout/startup/provider, current-candidate NFR10/performance, hosted TLS/browser,
production authority/revocation and human/authenticated-browser holds remain.
DW517 closure establishes its local controls, not full release readiness.

**Next selected bundle: DW-105/DW-108, shared DOM-safe identity encoding.** Current
source still constructs raw tenant IDs in both grids and navigation/focus anchors;
that selection does not alter their open records or any settled decision.

Final audit reverified unchanged source digests, both empty indexes, uninitialized
nested dependencies, all1371 historical hashes, original B1–B8 readiness prefix
and exact coverage of324 open IDs by288 rows. The audit's first comparison
incorrectly trimmed the baseline file's trailing newline and was rejected before
writing any snapshot; the corrected exact-content comparison passes. This
checker failure and its correction are retained in command evidence; DW643 and
every other unrelated record remain byte-identical to original entry.
