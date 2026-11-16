# Original e0 Error follow-through: review proposals only

This packet maps all **817 original problem IDs** (630 Survived, 186 NoCoverage, 1 Timeout) from the complete original Error report at `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc`. It is not a mutation gate, current-source qualification, accepted triage, human approval, or a new score. The original report SHA256 is `44079e6dd90db8bf8370b59dd90e0049fb12a9ef5643f403e738ab15172e23bc`.

`per-id-ledger.json` gives every ID's exact span, replacement, original source SHA, owning member, resolved direct source callers, actual covered case examples, cause, observable-contract question, proposed follow-up, and uncertainty. `cause-groups.json` groups 225 shared member/cause combinations. These are evidence-bearing proposals; `owner`, `accepted_action` and approval remain unset. The previous draft reviewed by the parent remains byte-preserved under `ledger-draft-01/`, with the parent's independent span/blob review in `parent-ledger-identity-review-attempt1.json`.

## Proposed corrections and dispositions

| Original Error IDs | Review proposal | Bounds / next action |
| --- | --- | --- |
| 42 | Demonstrated contract gaps with private regression candidates | 15 earlier parser controls plus 27 new literal controls. Review and integrate meaningful cases, then run the unchanged complete cohorts/tool on the actual resulting source. No `kill-test-added` acceptance is authored here. |
| 331 | Source-inspected contract/domain proposals | Exact changes and source boundaries are mapped. Some are real gaps; others need valid/invalid-source or public-API reachability proof. They are candidates for finite owned follow-up, not blanket equivalence. |
| 11 | Other comparison-contract gaps | Typed/boxed/null/absent equality can affect generic comparisons. Use real public or generator consumers; do not add exact numeric hash assertions. |
| 418 | Hash behavior unresolved | Conditional equal-value/hash congruence is insufficient to establish non-actionability, collision performance, implicit Roslyn consumers or external-client behavior. |
| 4 | Public legacy defaults unresolved | 13143/13216 source-path fallbacks; 13327 writable default; 14523 clone-assignment default. Full-value parser callers do not prove public overload equivalence. |
| 10 | Bounded equivalence/non-actionability candidates | 13811, 13885, 14010, 14347, 14651, 14693, 14724, 14727, 15060, 15157. Exact e0 source/caller/helper proofs appear per ID; timing, allocation and external bounds remain explicit. Parent must independently decide. |
| 1 | Preserve detected Timeout14021 | Removing the only `parts.Pop()` progress step in `SourceTypeNameFormatter` leaves the loop nonterminating. Final original JSON says Timeout; a partial debug `is success` line is not its final status. |

### Concrete private test proposal

`private-test-proposal.patch` contains four **new files / 39 cases**, with XML documentation and the existing `MutationErrorRegression` trait so both fixed mutation cohorts admit them. It preserves the earlier 13 cases, adds 25 public mapping/label/schema/equality cases, and adds one legitimate custom-provider parser boundary case with its internal helper in a separate file. It changes no existing cases, filters, targets, thresholds, tool, production implementation or owner triage.

The complete 39-case proposal passes original e0. The new controls show:

- Primitive aliases and generic enumerable boundaries: 13555/13556/13592/13611/13614 change public mapping/admission results.
- Visible acronym/digit labels: 14049/14089/14098/14103/14112/14120/14121/14125/14128 change actual humanized labels.
- Negotiated Markdown/skill schema metadata and field nullability: 15199/15203/15214/15219/15222/15287 change public canonical contract material. The nullability test also checks its structural SHA fingerprint; no `Object.GetHashCode` value assertion is used.
- Public generic/boxed comparison contracts: 13455/13457/13475/13524/13537 change reflexive, null/absent, inequality or boxed-type comparisons.
- Public `ProjectionTemplateMarkerParser.Parse` has no enforced matching-provider precondition. A real `ForAttributeWithMetadataName("System.ObsoleteAttribute")` context returns an empty result on original e0; 13689 (`<` to `<=`) and 13690 (`i++` to `i--`) each produce `IndexOutOfRangeException` and fail exactly the new generator case. They are **gaps**, not equivalence candidates based solely on this repository's matching generator caller.

`behavior-controls-01/verification.json`, `behavior-controls-02/verification.json` and `proposal-controls-03/verification.json` retain exact build/test commands, source hashes, nonzero exits and full raw logs/TRX. The original 25-case source is unchanged; XML/style adaptations in the packaged 13- and 25-case files have identical Roslyn token sequences (`package-check-04/token-equality.json`). Packaged files were themselves compiled and passed all 39 cases. `git apply --check` passes against clean owner695d; the patch was not applied there.

The subsequent style correction is isolated in `proposal-controls-05/` and `package-check-06/`. The previous packaged files, patch, summary, manifest and reproduction scripts are byte-preserved in `pre-fix-packet-2026-11-16-01/`. The corrected package indents the projection test attributes, documents all public theory parameters, and moves `OtherAttributeGenerator` into its own internal documented class file. All 39 cases pass original e0 again; each of literal13689/13690 again fails exactly the provider case. Roslyn confirms unchanged test/helper member tokens, one type per file, parameter documentation and attribute indentation. No original raw control or draft evidence was edited.

Two refusal distinctions are preserved. Exact 14124's ordinary strict literal build fails CS8794 before runtime; no kill or report-status change is claimed. Earlier 12207's strict build fails CS8602; its separately preserved runtime-only control retains that warning using only `WarningsNotAsErrors=CS8602`, passes the original 13-case baseline, then fails malformed `[RelativeTime(1 / 0)]`. This is bounded behavior evidence and is not unmodified-policy mutation qualification.

### Hash/equality consumer audit

`hash-consumer-audit.json` records all 418 IDs, 19 hash members, exact equality/hash source, resolved direct calls, and 28 existing test hash calls. Of the 418, 318 alter pure arithmetic, 99 drop a nullable contribution, and 13541 changes the array bound so every array returns seed17. Equal immutable inputs still produce equal hashes under the conditional algebraic argument for 417; **numeric result, distribution, lookup cost, public external behavior and invalid-state invariants are not equivalent by that argument**. The constant-array hash has a concrete collision/performance risk.

Resolved explicit production hash calls are inside other hash methods; none was found in another production project. `FrontComposerGenerator` still passes model results through Roslyn incremental providers (`Collect` and output registration), so implicit generic/default comparisons remain a platform consumer outside that explicit-call proof. Existing integration/parser tests already assert equal-copy hashes and some selected unequal-field hashes; preserve all of them and await complete Happy evidence. Canonical schema fingerprints use their own SHA material, not model `Object.GetHashCode`.

### Remaining source boundaries

All 186 NoCoverage statuses mean no coverage in the **original Error cohort**, not necessarily the complete test suite. The ledger proposes finite source fixtures for parser invalid-type/policy/default-argument branches, qualified-name/extern-alias formatting, UI role/status/date/order transforms, protocol mappings and schema metadata. `ColumnModel.SupportsFilter` has no tracked production getter reads beyond its declaration; emitted-UI harm is unproved, so no score-only flag assertion was added. Cancellation11858 and metadata-location alternatives retain their timing/metadata unknowns rather than claiming equivalence from one normal source case.

The semantic index is SDK Roslyn5.12 source analysis, not runtime/native qualification. Four missing source-generated STJ-context members are recorded because the index does not run that generator. Every actual invocation expression in the scanned SourceTools+Contracts resolves; 160 `nameof` intrinsics have no invocation symbol. Property/operator access, implicit generic dispatch and external callers remain explicit limits. The first failed source-index attempt is retained under `scan-attempt-01/`.

## Legitimate owner follow-through

The existing owner guide permits `kill-test-added`, `equivalent-accepted`, `deferred-with-owner`, and `blocking`. The tracked validator consumes aggregate entries with segment/status/action/count/owner/rationale; it checks allowed actions, nonblank owner/rationale and sufficient counts, **not the truth of each ID's evidence or a human approval**. This per-ID ledger supplies review material and must not be replaced by unsupported aggregate counts. A genuine deferred gap needs an actual owner and follow-up story assigned by the parent/owner; none is invented here.

Both segments, complete cohorts, original target roots, thresholds80/60, unchanged authenticated private tool and provenance validation remain required. `eng/stryker_compat.py` binds execution to exact HEAD/config/tool, refuses dirty/untracked source inputs, and compares the original execution receipt with current expected provenance. Committing tests or triage metadata changes HEAD; old e0 reports remain e0 diagnostics and cannot qualify695d or that new commit. Later owner changes need source-correct fresh qualification. This investigation did not rerun the full Error segment, modify owner files, author accepted records, or operate Git/remotes.

## Reproduction and cleanup

Each script refuses an existing attempt directory. From root, the focused jobs used:

```text
env TMPDIR=/dev/shm run-with-temp python3 <this-directory>/scan.py
env TMPDIR=/dev/shm run-with-temp python3 <this-directory>/verify-controls.py
env TMPDIR=/dev/shm run-with-temp python3 <this-directory>/verify-controls-followup.py
env TMPDIR=/dev/shm run-with-temp python3 <this-directory>/verify-proposal.py
env TMPDIR=/dev/shm run-with-temp python3 <this-directory>/verify-package.py
python3 <this-directory>/build-ledger.py
python3 <this-directory>/final-audit.py
env TMPDIR=/dev/shm run-with-temp python3 <this-directory>/verify-proposal-after-style-fix.py
env TMPDIR=/dev/shm run-with-temp python3 <this-directory>/verify-package-after-style-fix.py
python3 <this-directory>/final-audit-after-style-fix.py
```

The first literal-control job stops at the preserved CS8794 refusal; the second continues the finite remaining set. Builds use exact read-only archives of e0 and root-declared Builds73e60af8, private caches, MTP/Release and disabled compiler/MSBuild reuse. Only the proposed classes execute. No nested checkout/init, private commits, broad mutation rerun, retained backend action or remote mutation occurs. `summary.json` and `evidence-manifest.json` bind final counts, preserved bytes, source identity and owned scratch/process cleanup; other retained local workloads are untouched.
