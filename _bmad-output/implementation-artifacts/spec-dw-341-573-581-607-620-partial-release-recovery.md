---
title: 'Resolve five deferred partial-release recovery records'
type: 'bugfix'
created: '2026-10-22'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '7cfa1c38847f7f4340b1cc11a9137f28430bd6c6'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve DW-341, DW-573, DW-581, DW-607 and DW-620 as one bounded partial-release recovery correction. Use the same validated package manifest throughout validation, packing, publication and verification, refusing inventory changes between steps. Recovery must require an existing package, container, matching source tag or consistent release. GitHub asset repair must verify existing package bytes and upload only missing assets, preserving matching assets. Require the Trusted Publishing account and NuGet login only when the validated inventory contains missing packages.

Given an entirely absent release, when recovery is validated, then refuse publication; given an existing package or container without a tag, preserve the supported recovery path. Given an existing GitHub release with matching assets, when recovering, then verify every present package against reviewed local bytes and upload only missing names without replacement; conflicting, duplicate, unexpected or unverifiable assets fail closed. Given a changed manifest, refuse before publication. Verify the production scripts and workflow through isolated fixtures, close exactly these five ledger records with evidence, then commit and push main as requested.

</frozen-after-approval>

## Implementation Notes

- Investigation: scripts/publish-partial-release.sh ignores its manifest override; packing and final verification use the default inventory. The validator admits zero existing destinations, the workflow always authenticates NuGet, and existing release assets are neither repaired nor content-checked.
- Reuse the existing recovery, publication, verification and package-governance fixtures. Preserve prerelease classification, source/CI guards, last-minute live-main checks, and existing-package hash checks. Publisher-only all-missing fixtures remain separate from validator eligibility.
- This is a reversible bounded script/workflow change with no unresolved product choices, new service API, dependency updates or deployment. User authorization covers the final commit/push. Existing Aspire topology was inspected before edits and is running; ordinary development checks use Debug source references.
- Planned footprint: four recovery/packing scripts, one internal shared shell helper, the existing recovery workflow and focused contract tests, plus this spec, closure evidence and the two backlog documents. A manifest hash binds steps; asset downloads use authenticated GitHub asset IDs and compare local reviewed package hashes, never asset-controlled URLs.

- Implemented manifest schema/hash binding, shared packer project-path preflight (`--validate-only`), staged validation state, explicit missing-package credential output and GitHub asset content verification. Publication uploads only manifest-listed missing assets, without replacement. All 23 existing Aspire resources were Running/Healthy.
- Initial full contract suite passed 659/659 with zero failures/skips. The initial parallel source build hit an upstream shared deps-file lock; explicit `-m:1` source builds pass with zero warnings/errors.

## Review Triage Log

- Medium / patch: refreshed release metadata was not qualified at the new mutation gate. The shared checker now requires matching tag, non-draft status and exact prerelease classification; timed metadata changes reject before every publication.
- Low / patch: reversed canonical IDs could not prove final-verifier override propagation. The offline inventory now uses distinct Fixture.Package IDs with valid real project paths and asserts exact package/asset destinations. An initial mutation control accepted the default-verifier regression; the corrected fixture is requalified below.
- Medium / patch: asset downloads and final main lookup followed the last manifest check. The publisher now rechecks immediately after that lookup and before mutation; a download-triggered manifest change rejects with credentials available and no publications.
- Medium / patch: shell preflight and packer had different project-path contracts. Common preflight invokes the packer's existing validation without touching artifacts; outside, option-like, nonexistent and wrong-extension project paths reject before API reads/build.
- Low / patch: package/container-without-tag tests ended at validation. Both now run publication and final verification.
- Low / patch: existing-release repair implicitly supplied a NuGet key. Dedicated partial-existing-release and container-only repairs now omit that key and assert their exact destinations.

- Final independent recheck confirms all six findings addressed, with no remaining concrete findings and no review work deferred. It separately proves manifest changes during the final main lookup reject before publication and that the default-verifier mutation now rejects explicitly.
- Final reviewed Debug source build: zero warnings/errors. Final complete Contracts suite: 659/659 passed, zero failures/skips, including the 18-case offline pipeline. Five isolated behavior mutations reject; actionlint, shell syntax, scoped C# formatting and whitespace checks pass. Lockfile-pinned commitlint 21.2.3 validates the exact final commit message with zero problems/warnings.
- Exactly five ledger records close. All 657 IDs and the 652 unrelated record bodies are preserved; 500 done/157 open across 147 exact-covering maintenance rows. Original selected fields remain under pre-partial-recovery-. Dependency pointers, archive/sprint and existing release holds are unchanged. See dw-341-573-581-607-620-validation-evidence.md and its run manifest for commands/results and the initially accepted then repaired control.
