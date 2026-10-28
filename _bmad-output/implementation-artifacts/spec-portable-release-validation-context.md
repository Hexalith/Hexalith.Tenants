---
title: 'Carry portable validation context between release runners'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'oneshot'
baseline_commit: 'a96871778310dbb1b78396122820348a88339a9c'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

The protected publisher must revalidate the exact prepared packages using the same restore evidence that qualified them on the preparation runner. Carry portable copies of the five manifest-owned restore metadata files in the existing candidate artifact, bind them to the source/version, manifest and package bytes, and restore them before the existing validators. Preserve all package, consumer, source and publication gates without restoring the solution or rebuilding packages on the publisher.

</frozen-after-approval>

## Implementation Notes

- Evidence: release40633578515 passed complete candidate preparation, protected runtime setup and authenticated preflight, then failed before tag/package publication because validate-nuget-packages.py could not find Contracts obj/project.assets.json on the fresh publisher runner.
- Complete independent audit: only the five authoritative projects' asset JSON files are needed by metadata validation; no package caches, generated props, referenced-project assets or source bin/obj assemblies are consumed. Normalize only project.restore.projectPath to the manifest-relative path, which the validator already supports. Keep every direct/central/framework dependency field. Consumer validation creates independent temporary consumers and uses initialized Builds props; its existing validation remains unchanged.
- Independent two-directory experiment reproduced missing metadata and absolute-path rejection, then passed the actual5.11.0 CI packages with synthetic schema-valid portable fixtures. The production capture must use the actual preparation-runner package-mode restore data, after both real validators pass.
- Scope: share existing authoritative project-inventory validation; add a standard-library capture/restore utility under scripts; call capture after successful preparation validators and restore after existing source/version guards. The existing artifact already carries the candidate directory, so no workflow permission or dependency change is needed. Bind source SHA, version, manifest hash, package bytes and normalized asset bytes. Refuse symlink escapes, stale contexts and replacement of existing restore evidence.
- Route: one coherent release handoff fix, no unresolved user intent, no irreversible implementation action or public product API. Publication is separately authorized. Tests use two clean checkout directories and real package validation, with missing/altered/source/version/manifest/package/path controls.

- Implementation: the context index binds actual package IDs/versions and byte hashes, the full live source SHA, the manifest hash, and each normalized restore file. Tracked source drift is rejected. The publisher validates all staged dependency schemas before exclusive creation of any asset file and rolls back newly created files on write failure; existing data is preserved. Both original validators remain mandatory.

## Review Triage Log

- High, patched: candidate header/version alone did not prove actual package versions. Capture and restore now check every package ID and internal version against the authoritative inventory and candidate.
- Medium, patched: unchanged HEAD did not prove tracked source bytes were unchanged. A tracked-source diff check and mutation regression bind live files to the declared commit.
- Medium, patched: malformed dependency evidence could fail after materialization. The existing strict restore validator now accepts a staged path and checks all schemas before any write.
- Medium, patched: a late filesystem error could leave partial metadata. Destination types are checked up front; exclusive creation and rollback remove only files created by this operation.
- Medium, patched: wrapper tests omitted capture/restore arguments and failure ordering. Producer controls now record actual capture arguments; real publisher-wrapper tests prove restore precedes metadata and consumer validation, and failure stops both.
- Follow-up independent review found no concrete regression;21 context tests and11 dependency-range controls passed. All24 proposal/exec/shell controls,252 package-governance Contracts tests and6 executable workflow-closure tests passed without failures/skips; shell syntax and diff checks passed.
- Full independent replay: genuine package-mode restores for all5 manifest projects and actual5.11.0 CI package bytes passed capture, transfer to a different clean checkout and the complete protected helper, even after the original preparation path was unavailable. Both real consumers restored/built with zero warnings/errors; Testing ran1 passed/0failed/0skipped. Final clean local fixture commit1631c64468a076df7667fc053fc588e726e159be passed pinned commitlint and all4 script hashes matched this workspace. Reused package repository metadata remains authentica968; this qualifies the handoff/consumers, not new-source publication provenance. Evidence:/tmp/tenants-actual-handoff-5dsxq26t/final-result.json.
