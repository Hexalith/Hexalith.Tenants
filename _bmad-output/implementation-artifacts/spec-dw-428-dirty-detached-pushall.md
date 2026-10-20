---
title: 'DW-428: Preserve dirty detached repositories during pushall'
type: 'bugfix'
created: '2026-10-20'
status: 'done'
route: 'oneshot'
baseline_commit: '243d0c4a3f845e3f582a32c08e373651d978ff0e'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The repository's `/pushall` procedure commits a dirty detached HEAD before switching to the default branch. That new commit has no branch and can be omitted from the push.

**Approach:** Require a verified attached branch before committing a dirty repository. Skip a dirty detached repository without staging, committing, switching, merging, pushing or pruning it; report the required branch attachment and continue processing the other repositories. Keep the three tracked skill entry points consistent and qualify the documented gate with isolated Git submodule fixtures. Close only DW-428 after verification; preserve DW-429, historical findings and release holds.

</frozen-after-approval>

## Implementation Notes

- Entry revision: `243d0c4a3f845e3f582a32c08e373651d978ff0e`; clean `main` after the user pushed their existing changes. The priority index lists DW-428 first; editing the procedure is authorized by the deferred-work request and does not invoke `/pushall`.
- No unresolved intent gaps or irreversible implementation actions. The user separately authorized committing and pushing the completed change. The footprint is the three `.agents`, `.claude` and `.github` pushall manifests, one stdlib procedure-regression file, its invocation in the existing Story Guards job, and the selected ledger/index/spec/evidence records. No application API, dependency or runtime change is needed.
- Reuse the existing branch merge path for dirty attached repositories. Clean detached submodule checkouts must still pass the gate. Failed status/branch inspection must refuse mutation. Explicitly exempt the skipped dirty detached repository from the otherwise mandatory default-branch return.
- Tests will execute the exact documented shell block against disposable local Git repositories and submodules, assert unchanged HEAD/index/worktree/refs on refusal, and inspect a local bare remote for the accepted attached-branch path. They do not execute `/pushall` against the shared workspace or a network remote.
- Validate with `python3 tests/scripts/test_pushall_detached_head.py`, the existing story-gitlink regression lane, manifest validation, ledger/index preservation checks, `git diff --check` and the own-story gitlink guard. Application/Aspire execution is unnecessary for a procedure-only change.
- Added a read-only shell gate before fetch, using `--no-optional-locks` so status does not refresh the index. Failed status/branch inspection refuses the repository; a genuine detach has its own diagnostic. All three tracked entry points are byte-identical. The existing Story Guards job now runs the new stdlib lane without introducing dependencies.
- Initial verification: eight new procedure tests and 77 existing guard tests pass. The fixture reproduces the original omitted-commit outcome, covers five dirty detached states in all three copies, and verifies that an accepted attached commit and its files reach the local bare remote. Full workspace `/pushall`, network pushes from fixtures and application topology execution are not part of these tests.
- The skill-creator validator rejects the inherited `disable-model-invocation` frontmatter key in all three manifests. Preserve the established explicit-only policy; record its pre-existing validator incompatibility rather than deleting the key to make the validator pass.
- Independent review reproduced hidden untracked-file and non-branch symbolic HEAD bypasses; the tag-ref case also reproduced a committed change remaining outside pushed main. The corrected gate explicitly inspects untracked/submodule changes and accepts only symbolic targets under `refs/heads/`. Five added tests cover these settings, existing-dirty status failure and executable/symlink preservation. Git invocation tracing rejects fetch or other prohibited operations inside the gate; snapshots also include FETCH_HEAD, file modes, modification times and symlink targets.
- Final maintained verification passes 13/13 procedure tests and 77/77 gitlink regressions, with zero failures/skips. Seven isolated harmful variants fail their matching controls. Independent correction recheck finds no remaining concrete issue and accepts the rejected orchestration-test expansion. Only DW-428 closes; final ledger/index counts are 656 IDs, 389 done, 267 open and 239 exact-covering rows. See `dw-428-validation-evidence.md` for exact commands, initial results and limits.

## Review Triage Log

Blind Hunter: 36.260 kB; `min(floor(sqrt(36.260) + 1), 10) = 7`; seven findings. Every finding is addressed below.

| Finding | Verdict and action | Evidence |
| --- | --- | --- |
| `status.showUntrackedFiles=no` hides an untracked-only dirty tree. | Medium; patch. | Independently reproduced gate exit 0. Added `--untracked-files=all` and a configured-repository refusal/preservation test. |
| `diff.ignoreSubmodules=all` hides a dirty child. | Medium; patch. | Added `--ignore-submodules=none` and a detached parent fixture whose default status is empty despite its dirty child. |
| A symbolic HEAD outside `refs/heads/` passes attachment inspection. | Medium; patch. | Independently reproduced a tag-target commit omitted from pushed main. Check the returned symbolic target and test the non-branch refusal. |
| The status-error test uses only an absent directory. | Low; patch. | Keep that test and add a forced status error against the existing combined-dirty fixture, checking its complete snapshot. |
| A two-repository runner should prove automatic continuation and suppression of numbered steps. | Low; reject. | The changed artifact is a skill, with no executable coordinator to exercise. A test-only orchestration loop would test invented duplicate behavior. The manifest explicitly requires skipping all numbered steps and continuing; evidence limits qualification to executing the documented gate, without claiming an autonomous full-skill run. |
| Fetch inside the gate can escape the original ref-only snapshot. | Medium; patch. | Trace every gate Git invocation and permit only status/symbolic-ref operations; also snapshot FETCH_HEAD. A fetch-injection control must fail. |
| Byte-only snapshots miss executable-bit and symlink-identity changes. | Low; patch. | Snapshot `lstat` mode/time plus bytes or symlink target, and add an executable/untracked/symlink preservation case. A chmod-injection control must fail. |
