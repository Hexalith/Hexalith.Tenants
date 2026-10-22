---
title: 'Resolve five deferred release credential records'
type: 'bugfix'
created: '2026-10-22'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '1e60af449c1fdc3f9801cf57e8847e00faab2394'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve DW-552, DW-553, DW-557, DW-561 and DW-566 as one bounded release credential hardening batch. Prepare and validate recovery packages, including existing-package byte comparisons, before exchanging the temporary NuGet credential. Publish the prepared bytes without rebuilding packages and refuse missing or changed preparation evidence. Keep the existing source, manifest, asset, inventory and live-main guards, including recovery without NuGet login when all packages exist.

Pin external actions in both release workflows to the exact commits of their currently declared versions, retaining version comments and recording upstream resolution evidence. Exercise the actual account guard Bash for populated, unset, empty and whitespace account values in both workflows. Qualify parsed workflow login conditions and the output-to-publication handoff with disabled-login and empty-key negative controls, without contacting a live credential service or publishing artifacts.

Given valid partial-release state, when preparing packages, then no NuGet key is needed and no destination is mutated. Given altered or missing prepared bytes or evidence, when publishing, then fail before any publication. Given missing packages, when login is skipped or supplies no usable key, then fail before any destination mutation; given no missing packages, recovery remains possible without a key. Given an unavailable account, the executed guard refuses login; valid accounts proceed. Close exactly these five records with evidence after the relevant contract suite and independent review pass, then commit and push main as requested.

</frozen-after-approval>

## Implementation Notes

- Investigation: recovery currently validates inventory, exchanges the key, then builds/packs/validates/compares packages in publish-partial-release.sh. Both workflows use mutable action tags. Recovery account guard has three existing executable cases; the normal release account guard lacks them. Existing textual publishing checks do not reject disabled login handoffs.
- Planned footprint: two release workflows, a new internal recovery preparation script, the publisher/common helper, existing publication/workflow/governance tests and the offline recovery fixture, plus spec/evidence and two backlog documents. Reuse current manifest-driven packer, validators, state directory and isolated clients; bind prepared package hashes to the validated source/version/inventory and recheck before mutation.
- No unresolved user-visible choices, dependency upgrades, public service APIs, migrations or live release actions. Final commit/push is authorized. The five items share the credential boundary; broader normal-release OIDC/build separation DW-560 and DW-563 remains open. Existing app is already running; Aspire resource state was inspected before edits. Development verification uses Debug source references and serialized builds.

- Implemented separate preparation with exact prepared-byte/state hashes and two publication rechecks; unavailable NuGet handoffs refuse before client use. All six previously tagged action revisions were resolved from their upstream repositories and action manifests recorded in the action-pins evidence. Initial new Python fixture reported three failures because a purported state mutation preserved its original empty value; the corrected case starts with one existing package.
- Initial serialized Debug source build: zero warnings/errors. Full Contracts suite: 677/677 passed with zero errors/failures/skips/not-run, including the 22-case Python pipeline and both workflow guard/handoff tests. actionlint, shell syntax and whitespace checks pass. Independent Blind Hunter review is in progress.

## Review Triage Log

- Medium / patch: missing required destination state could be accepted. Preparation now requires the state files and validates container present/absent; missing/invalid controls pass.
- Medium / patch: state changes during build could become the new accepted proof. Snapshot before preparation and compare after; build-time mutations and missing-state controls reject.
- Medium / patch: destination context was absent from proof. Bind repository/registry/container repository and check before the first publication client; changed-context controls reject.
- Medium / patch: normal release lacked executed missing-key coverage. Its actual configured verifyReleaseCmd now rejects absent/empty/whitespace output before clients, alongside the recovery command and successful account/recovery controls.
- Low / patch: required preparation environment bindings were not checked. Parsed exact bindings and six removal/change mutations now qualify the workflow command; continued-failure mutation also rejects.
- Medium / patch: preparation failure propagation needed coverage. Four actual failing stages revoke stale markers and prevent subsequent publication; independent seven-file deletion probes also refuse without destination mutation.
- Low / patch: SHA syntax did not prove the selected immutable revision. Tests now bind actual action pins and version comments to recorded upstream evidence.

- Independent final recheck: all seven findings addressed, no remaining concrete defects and no deferred review work. Updated offline pipeline passed 27/27. All four disposable behavioral mutants reject through assertions. Exactly five ledger records close; all 657 IDs and 652 unrelated bodies remain, 505 done/152 open across 142 exact-covering maintenance rows. See the validation evidence for exact commands and practical limits.

- Final reviewed source build succeeds with zero warnings/errors. Complete Contracts suite: 687/687 passed, zero errors/failures/skips/not-run (48.984 seconds), including the 27-case offline pipeline. Workflow lint, shell syntax, scoped formatter and whitespace checks pass; the formatter reports an existing workspace-loading warning. Lockfile-pinned commitlint 21.2.3 validates the exact final message with zero problems/warnings. No live publication or OIDC exchange was attempted.
