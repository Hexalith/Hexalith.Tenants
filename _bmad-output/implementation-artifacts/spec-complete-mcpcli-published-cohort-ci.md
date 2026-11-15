---
title: 'Complete McpCli CI with the authentic published Tenants cohort'
type: 'bugfix'
created: '2026-11-03'
status: 'done'
resolution: 'superseded'
closed: '2026-11-15'
superseded_by: 'spec-qualify-published-mcpcli-7.0.0-enrollment.md'
route: 'dispatch'
baseline_commit: 'be43b928ce700ae7433b1d078eec356ccd6079f8'
owning_repository_baseline_commit: 'edaecb04b1a8edf78f6ad28e5bf253db594003f4'
historical_gitlink_original_baseline: 'edaecb04b1a8edf78f6ad28e5bf253db594003f4'
historical_gitlink_creation_ref: '35ddf94679ea92fb937b59f4202c4352bf7edf49'
historical_gitlink_audit_ref: '35ddf94679ea92fb937b59f4202c4352bf7edf49'
gitlink_audit_ref: '8b03eb999cd596aa07906face9db295e6705f9de'
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

## File List

- `references/Hexalith.AI.Tools`
- `references/Hexalith.Builds`
- `references/Hexalith.Commons`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.McpCli`
- `references/Hexalith.Memories`
- `references/Hexalith.Platform`
- `references/Hexalith.PolymorphicSerializations`

## Completion Notes List

- The root release sync moved `references/Hexalith.Builds` to its SDK cohort fixture repair before the Tenants package release needed by McpCli.
- That same root sync moved `references/Hexalith.FrontComposer` to its verified Firefox CI revision; both pointers are recorded here because this spec was added in the same root sync commit.
- `references/Hexalith.Builds` 10163c3e4ffd84ddb9b2831dea767a04da04f2ef -> 70850b18e0d0b80facd2df0809974020dd935732
- `references/Hexalith.FrontComposer` e4df7256be0e89b48c830c0792033ecfeabec9ad -> f842711d67004200e3248b3c42b7d9bcb2146907

The entries above retain the original integration targets. The current audit
covers the full historical range through the committed closure at
`8b03eb999cd596aa07906face9db295e6705f9de`, including later independent dependency
synchronizations. This inventory does not introduce pointer changes or attribute
those synchronizations to the closure.

| Path | Corrected original baseline | Historical audit target |
| --- | --- | --- |
| `references/Hexalith.AI.Tools` | `1745b6663ad32bb1288167135ed0f979366c53a0` | `17a37d83db193f0b7494c1b931fd1e82a67c1424` |
| `references/Hexalith.Builds` | `10163c3e4ffd84ddb9b2831dea767a04da04f2ef` | `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee` |
| `references/Hexalith.Commons` | `11555ace9efcae2928a00122dcba91243e5a8a1a` | `fbf4e94233821f6ca336af91a06feaaeb7a16855` |
| `references/Hexalith.EventStore` | `f06abada8dc3aab4a5fd240847fd680d515c98a5` | `fdb59895975e86a1e234325a01237f259cdbd53b` |
| `references/Hexalith.FrontComposer` | `e4df7256be0e89b48c830c0792033ecfeabec9ad` | `bca9dff291ce42c57516598732340a80703dfb6d` |
| `references/Hexalith.McpCli` | `edaecb04b1a8edf78f6ad28e5bf253db594003f4` | `23072f04f423eea3f30456fe5af13d2d382624fe` |
| `references/Hexalith.Memories` | `d9d61d8a95c7dc8827acf68ae723859711703729` | `b7e195031cab96b38949b90317ea8033797027b8` |
| `references/Hexalith.Platform` | `1f5958e24d1b414d8956d079270f89556e23415d` | `f6f85032598865b9123f16364448bfd02c0ff5b0` |
| `references/Hexalith.PolymorphicSerializations` | `bce4eb9f45c6b131b960ab4fa39e50ff01e51960` | `e97b56930e213f674b64c157551f499b0473ec43` |

## Code Map

- `references/Hexalith.McpCli/Directory.Build.props` pins EventStore 4.0.0 before importing shared versions; advance it to the independently verified published 4.0.4 patch selected by the final Tenants packages. Preserve the legacy test harness override.
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

- Follow-up producer repair for the actual Tenants runtime failure selects EventStore 4.0.3 from 9aa4e5d690db3cce734ae38942b05c23c8380bac. Keep the original 4.0.2 publication facts historical; use the final published 4.0.3 dependency closure during this later McpCli handoff.

## Historical Closure

Closed on 2026-11-15 following the user's explicit instruction, "close". This operational draft was superseded by [the published-enrollment qualification spec](spec-qualify-published-mcpcli-7.0.0-enrollment.md). The original frozen intent, checklist, gitlink audit endpoints and coordination notes remain historical; the unchecked items above do not represent pending work in this closed draft.

McpCli implementation commit `bdea9dc808cb12c06a2db69d71165026d3454f43` delivered the published Tenants 7.0.0 / EventStore 4.0.4 enrollment, approval evidence, dependency policy and documentation while preserving Parties.Contracts 1.5.1 and the legacy Integration 3.128.4 fixture. Commit `08b0829809b137a57c36a1974ed13158529a7263` added independent official NuGet v3 publication verification.

Closure verification matched all fourteen historical vector hashes at the implementation revision and the retained verification/enrollment receipt hashes against [the original published-artifact approval](../../references/Hexalith.McpCli/_bmad-output/implementation-artifacts/evidence/story-4-18/tenants7-public-artifact/approval.json). Both retained receipts record `passed`; the enrollment receipt records `fullV1Accepted: true` for its historical offline gate. This audit does not assert fresh package validation, current hosted/live CI acceptance or actual operator-client validation. Current-cohort CI remains separate successor work.

Story Guards run `43709976758` refused this historical closure because its push
also moved five dependency pointers. A local replay then exposed earlier
story-associated pointer commits after the original endpoint, so that endpoint
cannot audit the later closure. The audit now covers the committed closure and
all nine dependency paths in its full range, preserving `baseline_commit`.

Before changing the active audit metadata, the production runner's independent
`verify_baseline_correction` check verified the original correction against its
creation and endpoint. Its receipt is retained in
`evidence/ci-repair-2026-11-01-review-2026-11-15/original-mcpcli-baseline-correction.json`.
The old metadata remains under `historical_gitlink_*`; the active audit uses the
already committed corrected baseline rather than requesting another correction.
This documentation correction must remain in an event without gitlink changes,
as required by `docs/story-gitlink-ci.md`. The earlier mixed event remains failed.
