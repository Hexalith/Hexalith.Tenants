---
title: 'Resolve five deferred repository tooling safeguards'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
baseline_commit: '349379e305d4eda41d36821376073308ef8a8ea3'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve exactly DW-292, DW-397, DW-417, DW-418 and DW-601. Make repository tooling stop safely after failed Git prerequisites, keep elicitation methods uniquely addressable by number, and qualify TEA enforcement and customization-root selection with executable regressions. Preserve supported behavior, mirrored assets, original findings and unrelated records. Verify, independently review, commit and push the completed changes on `main` as requested.

</frozen-after-approval>

## Implementation Notes

- Bounded internal tooling changes and temporary-fixture tests; no unresolved intent gaps, new public API, domain changes, dependency updates or deployments. The user's explicit five-record request sets the batch scope. Final push is authorized.
- DW-417: add checked initial fetch and explicit per-repository skip instructions to all three tracked pushall manifests. Execute the documented command in disposable repositories; failed fetch must reach no subsequent sync operation.
- DW-418: existing switch error exits already improve the historical finding. Add useful failure diagnostics and verify exact attached HEAD after switch. Exercise existing/missing defaults, real switch refusal, false-success and inspection-failure cases before merge/push/prune; preserve unrelated work.
- DW-601: reject duplicate nonempty method numbers after merging overlays in both `pick_methods.py` copies, retaining inherited numbers, unique explicit numbers and automatic allocation. Test numeric lookup, CLI error status and input preservation.
- DW-292: add standard-library Python tests importing and invoking both `tea-enforce.cjs` copies through Node. Pin registry classification/severity, scanner behavior, pre/post/stop enforcement, gates and intentional fail-open behavior with temporary roots.
- DW-397: execute the real resolver plus copied `config_utils.py` under isolated temporary projects. Pin explicit-root precedence, cwd/script/skill fallbacks, nested Git versus ancestor BMAD root and masked-override diagnostics.
- Reuse `tests/scripts/test_pushall_default_branch.py` fixtures and `.github/workflows/story-guards.yml`; add new executable suites to the existing blocking tooling job without dependency changes. Run relevant existing Python suites and both existing elicitation pytest suites, plus whitespace and changed-story checks.
- Pre-edit Aspire start/describe/stop succeeded. Describe captured 23 resources: ten Running/Healthy, one Starting, twelve Waiting. This is startup observation only; no application runtime acceptance is claimed for this tooling-only batch.
- Review and closure require exact five-record preservation and maintenance-index coverage, then pinned commitlint evidence before Git commit.

- Initial validation: all 225 repository Python tests and both existing 22-case elicitation suites pass. All four negative controls reject the baseline fetch, baseline absent HEAD check, baseline duplicate numbering and an always-success hook by assertion, with no runtime errors. The final corrected-manifest pushall suite passes 37/37. The initial standalone event guard lacked its required event file; the worktree story guard passes with no dependency pointer changes.
- Initial ledger verifier assumed all historical closed stubs had disposition fields. Using their existing done status fixes that verifier assumption; it then proves all 657 IDs, exactly five closures, all 652 unrelated records and original selected fields preserved. The index exactly covers 117 open records across 107 rows; archive and sprint remain unchanged.
- Independent Blind Hunter reviewed 86.985 kB and reported ten findings. The patches below add eight test methods and copy base catalog dictionaries, preserving supported behavior and both mirrored sources. Focused reviewed suites pass 6 catalog, 10 resolver and 14 TEA tests. No new public API, intent change or deferred record is introduced.

## Review Triage Log

| Finding | Verdict and evidence | Resolution |
| --- | --- | --- |
| Numeric-only method name shadows another number | low: pre-existing name/number alias behavior; normal shipped names are descriptive and numbers decimal. A numeric-only name that aliases another method is uncommon; repairing it adds a separate lookup namespace/input policy. | Reject the source-policy expansion under the low-finding rule; unique actual numbers and ordinary numeric lookup remain qualified. |
| Letter numbers A/a bypass exact duplicate check | low: pre-existing acceptance of nondecimal labels in the num field, outside normal numbered catalog use. The current fix qualifies decimal catalog numbers without defining a broader label grammar. | Reject a separate input-format/normalization policy; no supported decimal collision escapes the new check. |
| Allocation mutates unnumbered base rows | low: new input-preservation coverage exposes the shallow-copy gap. | Patch both copies to clone row dictionaries; prove numbered and unnumbered inputs survive allocation. |
| Quoted Bash filenames evade post extraction | low: the source explicitly documents target extraction as best effort and stop scanning catches files not named by the command. A complete shell parser would add disproportionate complexity for this transient boundary. | Reject parser expansion; add a regression proving the quoted-path post miss is blocked by the subsequent stop sweep. |
| File-scoped pre-hook behavior untested | medium: the previous tests could miss whole-file checks incorrectly running on Edit fragments or being skipped for Write. | Patch paired Write/Edit/MultiEdit C4 and H5 cases. |
| C1 advisory CLI behavior untested | low: scanner presence alone did not pin warning output and exit status. | Patch undocumented-skip exit-0 warning and documented-skip silent controls. |
| Python/Java/Go scanner branches untested | medium: independent stripping/matching branches lacked executed controls. | Patch violations and comment/string controls for all three kinds. |
| Stop cutoff untested | low: an accidentally removed cutoff could scan unrelated historical violations. | Patch an old violating file with configured stop window; prove it remains excluded. |
| Git-root and no-project fallbacks untested | medium: the previous fixtures exercised BMAD roots only. | Patch real subprocess fixtures for nearest Git root and standalone no-project defaults. |
| New implemented rules could lack positive fixtures | low: registry classification alone did not pin executable rule coverage. | Patch equality between the fixture IDs and exported implemented RULES before executing them. |


## Final Verification

Independent recheck accepts all seven corrections and the explicit quoted-Bash post-to-stop boundary. The two catalog namespace cases are pre-existing low edge cases outside ordinary decimal-numbered catalogs with descriptive names; no blocker or new deferred record remains for this batch.

The final complete repository tooling suite passes 233/233 in 77.781 seconds, with zero failures/skips. Both existing mirrored catalog pytest suites pass 22/22 after the row-copy correction. The exact-five closure verifier and worktree story guard pass; archive, sprint, dependencies and all unrelated records remain preserved. Commitlint 21.2.3, as pinned in package-lock.json, validates the full commit message with zero problems/warnings. Full logs and negative controls are retained in the batch archive. Final publication is the requested Git push to main; no product release is performed.
