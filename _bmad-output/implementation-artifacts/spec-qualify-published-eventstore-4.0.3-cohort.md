---
title: 'Qualify the published EventStore 4.0.3 consumer cohort'
type: 'bugfix'
created: '2026-11-03'
status: 'draft'
route: 'dispatch'
context:
  - '/home/administrator/projects/hexalith/tenants/AGENTS.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="User authorized package upgrades, all CI fixes, unattended publication and synchronization">

## Intent

Qualify the next immutable EventStore patch needed by the actual Tenants CI repair. The already-published 4.0.2 SDK hardcodes a two-second final native observation. Producer commit 9aa4e5d690db3cce734ae38942b05c23c8380bac adds a configurable bounded deadline without changing the original default. Tenants now selects its forthcoming 4.0.3 package. The shared Builds catalog, finite Platform gates, schema, synthetic fixtures and audit must all identify that same selected cohort before the final root source gates can pass.

## Boundaries & Constraints

Preserve concurrent work, every compatibility exception, source-mode behavior, all negative controls, historical evidence, exact declaration bytes and unrelated package rows. Do not edit root Program.cs, root version pins, EventStore source, McpCli, or nested submodules. Never load a skill under references. Do not widen finite gates to arbitrary future versions, prereleases, metadata aliases or four-part versions. No dependency downgrade, validator weakening, skip, clean, reset, staging, commit or push during implementation. Root agent owns publication, commitlint, commits and remote operations under existing user authorization. Report preparation, configuration qualification, compilation and runtime evidence separately.

</frozen-after-approval>

## Code Map

- Owning repositories: `references/Hexalith.Platform` at 1f5958e24d1b414d8956d079270f89556e23415d and `references/Hexalith.Builds` at 70850b18e0d0b80facd2df0809974020dd935732. Read current owning tracked instructions/configuration before edits; assert clean target paths and report any concurrent revision movement. Capture both actual baselines in implementation notes before changing them.
- Platform gates: Composition.csproj, EventStoreHost.csproj, CatalogBuilder.csproj and Composition.Tests.csproj currently admit exact 4.0.2 in seven finite conditions. Add exact 4.0.3 beside it while retaining every prior branch. Existing `tests/catalog-builder/verify_sdk_gates.py` treats 4.0.3 as a negative: move it to the supported cases and add 4.0.4 plus 4.0.3 prerelease, metadata and four-part refusals, retaining all old refusals. Its ordinary default evaluation must use the shared Builds checkout.
- Builds `Props/Directory.Packages.props` has one shared EventStore default 4.0.2. Replace only that value with 4.0.3, preserving UTF-8 BOM and CRLF bytes. Explicit frozen EventStore.Testing.Integration 3.128.4 remains intact.
- Builds schema and module/evidence fixtures were coherently rebound to 4.0.2 in the prior repair. `/tmp/hexalith-builds-plan-sdk-fixture-refresh.py` is a read-only planning helper, not a skill. Inspect it before use. `create_plan(repo, expected_head, old_version, new_version)` returns a report and byte-preserving proposed file map; it checks negative controls, original hashes, symlinks, line endings and convergence. Apply only its validated 106 planned files after confirming producer publication; preserve `.expected.json`, tampered/superseded controls and every unrelated byte.
- Builds audit refresh must cite a committed catalog revision. First prepare the catalog-only edit and return a checkpoint report; root will validate/commit that explicit path. After root confirms the actual new owning revision and independently verified producer archives, resume with a fresh snapshot of `Tools/package-version-audit.json` outside the repository and run `Tools/audit-central-package-versions.ps1 -PriorAuditPath <snapshot> -ChangedFamily hexalith-eventstore -OutputPath Tools/package-version-audit.json`. Only thirteen selected EventStore rows may change; all other 301 entries must remain identical. Never manufacture runtime/owner approval in audit data.
- Existing Builds checks include validate-central-package-versions.ps1, test-authoritative-package-catalog.ps1, validate-package-version-exceptions.ps1, package audit generator/validator test scripts, Module.Tests, and G4 package contracts. Preserve the source-fixture follow-up already recorded in deferred-work.md; it is not a new failure caused by this patch.

## Tasks & Acceptance

- [ ] Capture actual owning baselines and inspect relevant build/configuration/tests. Prepare the catalog-only 4.0.3 edit and return its exact byte proof to root for the provenance checkpoint; make no commit.
- [ ] After the explicit root checkpoint, align the Platform seven finite gates and their existing test matrix with exact 4.0.3. Given source mode and neighboring aliases, when real MSBuild evaluation runs, then prior behavior and all explicit refusals remain intact. Retain the full machine-readable evaluation report in a fresh temporary path.
- [ ] Given root confirms all 14 published 4.0.3 archives match producer 9aa4e5d690db3cce734ae38942b05c23c8380bac, apply the validated fixture/schema plan and refresh the live incremental audit from the actual committed catalog source. Given the old fixture bytes and protected controls, when hashes are rebound, then only the selected version literal and necessary SHA-256 references differ; all protected bytes and unrelated audit rows remain unchanged.
- [ ] Run the narrow relevant existing catalog/audit/fixture checks and focused Platform catalog build/tests against the published SDK. Build test projects individually and avoid overlapping AppHosts/private runtimes. Report exact commands, counts, logs and any incomplete checks. Root will arrange automated reviews, final CI and publication; do not declare runtime acceptance from finite MSBuild evaluation alone.

## Implementation Notes

- Prerequisite release 41567697064 is running; no publication claim is made until independent archive verification succeeds. The root agent owns that dependency and will provide evidence before publication-dependent edits/checks.
