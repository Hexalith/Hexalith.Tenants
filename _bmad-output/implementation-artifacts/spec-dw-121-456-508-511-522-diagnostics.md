---
title: 'Resolve five deferred workflow and status diagnostics records'
type: 'bugfix'
created: '2026-10-25'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 'b52747d8ebb23455a7d4323b871fcc167cfd9a85'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete exactly DW-121, DW-456, DW-508, DW-511 and DW-522 from the deferred-work ledger, then push the reviewed changes to main as requested. Clarify that the existing Builds release inputs accept only main. Make the G-6 self-test refuse optimized Python execution, check expected cross-mode validation diagnostics, and exercise packet tampering in both accepted and candidate modes. Preserve historical packet/count contracts.

Declare the existing EventStore status endpoint's 409 ProblemDetails response in generated OpenAPI. Replace misleading correlation-only remediation with scope-aware guidance covering direct, indexed and legacy ambiguities; align the served error catalog and problem reference. Preserve lookup precedence, authorization, public error type/title, payload confidentiality and publication-recovery behavior. Close only these five records after focused validation and independent review; retain unrelated records and historical evidence.

</frozen-after-approval>

## Implementation Notes

- The requested five-item batch overrides the default single-goal proposal. This is a small set of settled contract, metadata and test-harness corrections with no new public API or unresolved product decision; use the oneshot route. Git publication is explicitly authorized by the user.
- Owning source repositories are root-declared `references/Hexalith.Builds` and `references/Hexalith.EventStore`; modify and validate their content from their own roots, commit and publish child changes before recording parent pointers. Do not initialize nested submodules.
- Builds begins at `857558036530a389ccb4ba16fa9777416af9b12e`. EventStore begins at `f4df781cb7247c6a5f7549106b90d3748151edba` with two existing local commits and remote divergence. Preserve those commits and reconcile without force-pushing or rewriting them.
- Relevant paths: Builds release workflow/input documentation, prepare-domain-release action/README, and `Tools/test-runtime-toolchain-evidence-validator.py`; EventStore `Controllers/CommandStatusController.cs`, `OpenApi/ErrorReferenceEndpoints.cs`, corresponding controller/OpenAPI/catalog tests, and `docs/reference/problems/`.
- Verify normal and optimized G-6 execution, preparation-action regressions, actual generated OpenAPI and rendered problem pages, controller ambiguity cases, story gitlinks and exact ledger/index preservation. Record exact commands/results and any environment blockers in the companion validation evidence.

- A concurrent workspace synchronization advanced EventStore to `749767072b8c2ea2abc8a23540ca65471d078c49`, preserving the existing commits and merging remote main without our intervention. Rechecked and reapplied the two scoped runtime source edits against that integrated baseline; tests and docs remained present. No user changes were reverted.
- Baseline `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --non-interactive --format Json` built with zero warnings/errors but exited 2: duplicate `eventstore-app-api-token` resource; focused in-process checks will provide the scoped runtime evidence.

- Review added current-v2 dual-mode controls and optimization refusal in Builds `Tools/test_runtime_toolchain_v2.py`; pending-specific metadata controls remain scoped and shared mutations rebind accepted hashes to avoid false rejection. EventStore `CommandDocumentationTransformer.cs` now restores the generated 409 description. No production validator or lookup policy changed.
- Final reviewed checks pass: Debug source build (zero warnings/errors); 61 controller/generated-OpenAPI/served-catalog cases (zero failures/errors/skips); G-6 30 logical scenarios per three baselines, 48 drift/165 authority/103 current/3 import/47 evaluated-catalog controls; preparation 9/9; publisher 150/150; actionlint. Targeted semantic-bypass and crash/output controls reject their harmful mutations.
- Exact ledger preservation confirms 657 IDs, 595 done/62 open across 57 maintenance rows. Archive, sprint and every unrelated record remain unchanged. Full evidence is in `dw-121-456-508-511-522-validation-evidence.md` and its linked JSON/log artifacts.

## Review Triage Log

| Finding | Verdict and evidence | Resolution |
| --- | --- | --- |
| Current-v2 packet tampering remained candidate-only | medium: the self-test invokes the current suite, whose helper hard-coded candidate mode. | Patched shared mutations to run both independently valid modes; preserve three mode-specific controls and logical counts. Accepted integer-validity bypass mutation now rejects. |
| Generated 409 description was not restored | low: runtime XML comment generation is disabled, and the transformer restored only 404. | Added transformer 409 description and asserted actual generated JSON; reviewed tests pass. |
| Served catalog omitted the no-resubmission caution | low: the linked Markdown had the warning but clients following the served problem URI did not. | Catalog and rendered-page test now preserve the same caution and request diagnostic reference. |
| Markdown example/support guidance omitted request diagnostics | low: runtime includes instance/correlationId but the example did not explain their use. | Added example fields and distinguished the request reference from the queried command identity. |

Independent recheck accepts all four patches and reports no remaining findings or deferrals.

## File List

- `references/Hexalith.Builds`
- `references/Hexalith.EventStore`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `docs/maintenance-backlog.md`
- `_bmad-output/implementation-artifacts/spec-dw-121-456-508-511-522-diagnostics.md`
- `_bmad-output/implementation-artifacts/dw-121-456-508-511-522-validation-evidence.md`
- `_bmad-output/implementation-artifacts/dw-121-456-508-511-522-test-results.json`
- `_bmad-output/implementation-artifacts/dw-121-456-508-511-522-ledger-validation.json`
- `_bmad-output/implementation-artifacts/dw-121-456-508-511-522-validation-logs.zip`

| Path | Baseline | Target |
| --- | --- | --- |
| references/Hexalith.Builds | 857558036530a389ccb4ba16fa9777416af9b12e | 1783c0a377615063f3222c1d28f3ab513af4e72f |
| references/Hexalith.EventStore | f4df781cb7247c6a5f7549106b90d3748151edba | 38393a15659eeb8a979764a97d185e868c2b33e6 |

The EventStore target includes independently synchronized upstream history already recorded by parent `f364406dcfb7501c93db109d1ab99db562520fb2`; this task's owning correction is `38393a15659eeb8a979764a97d185e868c2b33e6` on its integrated parent. The Builds correction is `1783c0a377615063f3222c1d28f3ab513af4e72f`. Other root gitlinks remain unchanged from the recorded baseline.


## Completion Notes List

- `references/Hexalith.Builds`
- `references/Hexalith.EventStore`

The Builds pointer adopts the two release-input/evidence-mode fixes. The EventStore pointer adopts the three status metadata/runtime/catalog fixes. Both owning commits are published on main. The independently reviewed ledger/evidence audit preserves exactly five closures and every unrelated record.

Concurrent user synchronization recorded the complete batch in parent `93fa7a7890f9a44d175b58e5de9f146bc97a232c` before this final workflow-state update. That commit is preserved; this follow-up finalizes the done state and records exact successful validation of its actual commit message and this completion message.
