# CI segment lifetime review, 2026-11-16

The requested lifetime change is already present in FrontComposer. Bounded local verification and inline review passed; no further workflow edit was needed. After review, the user explicitly requested `set done`; the requested spec's frontmatter is now `status: done`. Hosted qualification remains parent-owned.

The requested [retained spec](../evidence/synchronize-update-release-2026-11-14/resumed-parent-integration/fc-segment-job-lifetimes/retained-original-spec-ci-segment-job-lifetimes.md) was byte-identical and `in-review` during verification. The subsequent user-authorized edit changes only that status line; its frozen intent and historical implementation notes remain unchanged. Its implementation notes explicitly exclude review agents, commits and deferred-ledger edits. Those restrictions governed the review; the build workflow's independent reviewer layers and automatic commit were therefore omitted. Local review completion does not mark hosted qualification complete.

The root revision is `292dbf164f26bcf376ef86d749b149b075f8b502`. The reviewed FrontComposer revision is `bc9dbedc6141b750ca92f01b40ac840dc2066704`; the retained implementation source is `b34be9245443d21b72a6076cf205bbbc8a8c521f`. The workflow change was incorporated in `11c8377a858a756c4e4069bf08e62db1f58e1733`, alongside separately authored product tests and publication controls. The [baseline diff](baseline.diff) and [proposal comparison](retained-proposal.diff) distinguish the requested lifetime change from those later controls.

Happy and Error retain separate 360-minute matrix jobs with `fail-fast: false`. Each independently performs the original preparation and private-tool build, invokes its complete original cohort, verifies native provenance, and publishes bounded artifacts. The required `source-tools-mutation` aggregate independently prepares its runtime, downloads both fixed same-run artifacts, invokes the original strict validator with `-RequireCompatibilityProvenance`, and requires the matrix result to equal `success`. Later publication guards, digest bindings and finite identity validation remain intact. Header, lifecycle, container, resources, SDK and preparation environment match the retained baseline.

Verification ran from the owning repository, with disposable jobs under `run-with-temp`:

- `actionlint -no-color .github/workflows/mutation-property-nightly.yml`: exit 0, actionlint 1.7.12. See [output](actionlint.txt) and [structural inspection](structure.json).
- `python3 -B tests/eng/test_stryker_compat.py`: all 19 tests passed, including source/config/runtime refusals and owned-child cleanup. See [result](native-provenance.json) and [output](native-provenance.txt).
- `python3 -B tests/eng/test_mutation_artifact_publication.py`: all 15 existing tests passed, including failed-report retention, receipt identity, publication binding and missing-capture refusal. See [result](publication.json) and [output](publication.txt).
- Executing the aggregate's actual shell guard accepted `success` and refused `failure`, `cancelled`, `skipped` and the empty result. All 15 bounded structural observations passed; these are recorded in `structure.json`.

## Review Triage Log

Inline review found no additional workflow defect and created no deferred entry. Failure propagation, cohort membership, independent preparation, exact artifact names/roots, bounded uploads, provenance verification order and unchanged strict validation were checked against the retained change and current helpers. Publication guards can withhold unsafe artifacts; the existing publication tests verify safe failed reports remain available. This is the current behavior, separately added after the retained proposal.

The archived exact-shape workflow tests describe the earlier proposal and were preserved rather than rewritten or reused as current acceptance evidence. Current inspection accounts for the later publication steps; the existing current publication suite also exercises their conditions. No independent review or complete mutation score is claimed.

At verification time, the [final audit](final-audit.json) confirmed all 6,482 tracked owner files and all 23 archived input files remained unchanged, owner HEAD and working tree remained unchanged, all three verification scratch roots were removed, and no process retained their `TMPDIR`. The [preservation record](preservation.json) binds the complete starting inventory and retains individual parent test and archive hashes. These records predate the user-authorized status update and retain their original observations. Parent test hashes are observations from that review; historical source hashes are not substituted for them.

Fresh hosted completion, independent compiled-runtime equality, full Happy/Error scores and strict BOTH acceptance remain parent-owned. No hosted run, full mutation rerun, owner Git/remote mutation, accepted triage, release authority, dependency, gitlink or policy change occurred. The only output of this continuation is this local review packet.
