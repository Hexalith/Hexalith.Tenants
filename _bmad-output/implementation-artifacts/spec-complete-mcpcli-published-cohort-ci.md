---
title: 'Complete McpCli CI with the authentic published Tenants cohort'
type: 'bugfix'
created: '2026-11-03'
status: 'draft'
route: 'dispatch'
baseline_commit: 'edaecb04b1a8edf78f6ad28e5bf253db594003f4'
context:
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.McpCli/AGENTS.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
---

<frozen-after-approval reason="User explicitly authorized updates, CI repairs, publication and bypass of human validation">

## Intent

Finish the root-declared McpCli submodule's CI after Tenants 7.0.0 publication. Its current failure requires the unpublished Tenants packages and a pending published-artifact approval receipt. Use the actual signed NuGet archive, preserve all cryptographic and semantic checks, and record authorized automation truthfully rather than claiming a separate manual artifact review.

## Boundaries & Constraints

Wait for the root agent to verify authentic published Tenants 7.0.0 before implementing artifact approval. Preserve Parties.Contracts 1.5.1 and other deliberately approved artifact cohorts. Preserve the legacy EventStore.Testing.Integration 3.128.4 fixture. Do not read operator profiles/credentials, initialize nested submodules, weaken validators, or claim actual client-session validation. Root owns commits/pushes and remote CI.

</frozen-after-approval>

## Code Map

- `references/Hexalith.McpCli/Directory.Build.props` pins EventStore 4.0.0 before importing shared versions; advance it to published 4.0.2, which the new Tenants packages require. Preserve the legacy test harness override.
- `references/Hexalith.McpCli/tools/dependency-policy.json` pins the exact production/test closure; update only changed effective versions from a fresh restore, preserving approved package identities and unrelated module pins.
- `references/Hexalith.McpCli/tools/conformance-vectors/v1/modules/Hexalith.Tenants.Contracts/approval.json` already targets 7.0.0, but status is pending and its hash is only a local candidate. Bind the authentic signed archive SHA-512, inventory and exact vector hashes; retain old candidate facts as explicitly historical context.
- `tools/conformance-vectors/v1/README.md` defines the validator, approval evidence and production replay checks. `check_enrolled.py` requires exact restored archive bytes, module inventory and complete vectors.
- `README.md` still describes the previous 5.10.0 enrollment and fixed system tenant; correct only current enrollment/scope statements after verifying the new actual catalog, preserving explicitly historical operator session records.

## Tasks & Acceptance

- [ ] Freshly download the published Contracts archive; verify nuspec identity/version/source against the root release and run the shared vector validator before recording artifact approval.
- [ ] Update the EventStore pin and exact changed dependency-policy selections using fresh restore assets. Build Release with warnings as errors; run Manifest.Tests against that fresh restore.
- [ ] Add a tracked approval evidence JSON containing formatVersion 1, actual package/source/archive URL and SHA-512, inventory/vector/validator revisions and hashes, successful validation commands/results, timestamp, authenticated account and delegated automation identity. Quote the user's authorization and label the mode as automated under explicit human-validation bypass; do not claim separate human review.
- [ ] Update the pending receipt to approved-published-artifact, with a real approvalReference to the new evidence and publication URL. Preserve cryptographic bindings and vector bytes unless validation proves a specific semantic mismatch requiring repair.
- [ ] Verify complete enrollment coverage and check_enrolled.py replay; run the exact relevant conformance and topology preflight checks from owning guidance. Report failures precisely; do not skip required modules or fabricate readiness.
- [ ] Correct current README enrollment/version/scope statements from the validated catalog. Return local changes and verification to root for review/commit/push, then root will verify full current-source CI and synchronize the parent pointer.

## Implementation Notes

- Baseline McpCli CI run 41515759839 fails NU1102 for Tenants 7.0.0 and refuses candidate approval. EventStore 4.0.2 is now published and independently verified. No McpCli files have been edited yet.
