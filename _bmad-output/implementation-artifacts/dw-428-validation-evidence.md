# DW-428 validation evidence — 2026-10-20

Entry revision: `243d0c4a3f845e3f582a32c08e373651d978ff0e`; clean `main` after the user pushed the existing Contracts edits.

## Scope and behavior

The three tracked `/pushall` skill copies now run the same read-only worktree attachment gate before fetch. A dirty repository must have a verified attached branch. A dirty detached repository, or a repository whose status/branch cannot be inspected, is skipped before any numbered sync operation. Its original HEAD, index and worktree remain in place; the procedure explicitly exempts it from returning to the default branch and continues with the other repositories. Status uses `--no-optional-locks` to avoid index-refresh writes.

Clean detached submodule checkouts retain the existing path. Dirty attached repositories retain the existing commit/merge/push path, which can carry the commit into the default branch. No temporary branch, automatic attachment choice or detached commit is created by the gate. DW-429 remote-default selection and other existing procedure findings remain separate.

The user requested a deferred-work repair and authorized its commit/push. This task edits the procedure; it does not invoke `/pushall` on the workspace. Only disposable local fixture repositories are committed, merged or pushed during testing. No fixture contacts a network remote or initializes a shared/nested submodule. No application, dependency, persistence, publication or Aspire change is involved.

## Executed verification

Run from the repository root:

```bash
python3 tests/scripts/test_pushall_detached_head.py
python3 tests/scripts/test_validate_story_gitlinks.py
actionlint .github/workflows/story-guards.yml
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-428-dirty-detached-pushall.md
git diff --check
```

Initial maintained results: new procedure lane **8/8**, existing gitlink lane **77/77**, zero failures/skips; actionlint and the own-story gitlink guard exit 0. The existing Story Guards job now invokes the new stdlib lane alongside the existing guard tests, requiring no new dependency or workflow permission.

The new lane reads and executes the exact documented Bash block from each manifest in a real temporary submodule whose path contains spaces. Five independent dirty detached states (staged, unstaged, untracked, combined staged/unstaged/untracked and deletion) are refused in all three copies. Snapshot assertions compare HEAD, branch attachment, refs, index bytes, every worktree file, status and the bare remote's refs before/after each gate call. Clean detached and dirty attached/default controls pass without gate mutations. Failed status inspection and a forced branch-inspection error refuse and have distinct diagnostics. All three manifests must be byte-identical.

The attached-branch control follows the existing commit/default-checkout/fast-forward/branch-merge/push path and verifies the exact committed ancestor and both changed files at a local bare remote. A separate legacy-order fixture reproduces the omission: committing while detached, checking out `main` and pushing leaves that detached commit outside the pushed branch, with the remote file still containing its baseline data. This is procedure qualification, not a full autonomous `/pushall` run.

Isolated controls replace only the documented gate in temporary manifest copies: replacing it with `true` produces five expected refusal-assertion failures; replacing it with `exit 1` produces three expected clean/attached-control failures. Both controls have zero test errors. The first temporary control-report writer raised a `TypeError` while concatenating a unittest subtest object; converting that object to text corrected reporting before both expected results were verified. No production/test change was needed.

## Manifest validator limitation

Each exact command below exits **1**:

```bash
python3 /home/administrator/.codex/skills/.system/skill-creator/scripts/quick_validate.py .agents/skills/pushall
python3 /home/administrator/.codex/skills/.system/skill-creator/scripts/quick_validate.py .claude/skills/pushall
python3 /home/administrator/.codex/skills/.system/skill-creator/scripts/quick_validate.py .github/skills/pushall
```

The exact diagnostic is identical for every copy:

```text
Unexpected key(s) in SKILL.md frontmatter: disable-model-invocation. Allowed properties are: allowed-tools, description, license, metadata, name
```

Executing the same validator against temporary copies of all three entry-revision manifests reproduces exit 1 and the exact same diagnostic. Their YAML frontmatter is valid and byte-preserved, including `disable-model-invocation: true`. The validator's allowlist does not support the established field; deleting it would change the existing invocation policy. No passing full-manifest validation is claimed, and the validator is not weakened or bypassed.

## Commit validation and preserved scope

The owning pinned `@commitlint/cli@21.2.3` validates both exact full messages below before use:

```bash
node_modules/.bin/commitlint --edit /tmp/dw-428/fixture-message.txt --verbose
node_modules/.bin/commitlint --edit /tmp/dw-428/commit-message.txt --verbose
```

Both exit 0 with `found 0 problems, 0 warnings`. The fixture message is `build: exercise pushall attachment safety`; the task message is `fix(tooling): skip dirty detached repositories during pushall`.

Only DW-428 closes. Preserve its historical fields under `pre-dw-428-*`; all other records, the archive, sprint tracking, dependency pointers and release holds remain unchanged. The resulting inventory is 656 IDs, 389 done / 267 open, covered exactly once by 239 maintenance rows. Review and final integrity evidence follow; the push result is reported separately after committing.

## Independent review and final qualification

Blind Hunter returned seven findings. The owning spec records every verdict. Six are patched: force inspection of untracked/submodule changes despite Git settings; require a symbolic target under `refs/heads/`; test status errors in an existing dirty repository; trace prohibited gate Git operations; and preserve file metadata/symlink identity in snapshots. The suggested two-repository orchestration test was rejected as low: there is no executable coordinator in this change, and inventing one just for testing would not qualify autonomous skill execution. The documented skip/continue rule remains explicit. No finding is deferred.

The untracked-setting and tag-target bypasses reproduced independently against the first gate version: both returned exit 0, and the tag-target commit remained outside pushed main. The corrected **13-test** lane covers both, hidden submodule changes, the original five dirty detached states, inspection failures, executable bits and symlinks. Git call tracing allows only status/symbolic-ref operations; snapshot comparison also includes FETCH_HEAD and worktree modes/modification times. All three skill copies remain byte-identical, including unchanged valid YAML frontmatter. The pre-existing quick-validator incompatibility remains unchanged.

Seven isolated controls against the final regressions produce the expected assertion failures with zero test errors:

| Temporary gate change | Selected matching regression | Expected assertion failures |
| --- | --- | --- |
| Replace gate with `true` | Five dirty-detached states | 5 |
| Replace gate with `exit 1` | Clean detached / dirty attached / dirty default | 3 |
| Remove `--untracked-files=all` | Hidden untracked files | 1 |
| Remove `--ignore-submodules=none` | Hidden dirty child | 1 |
| Remove the `refs/heads/` target check | Non-branch symbolic HEAD | 1 |
| Prefix gate with local-fixture `fetch origin` | Five dirty-detached states | 5 |
| Prefix gate with `chmod +x` on the untracked file | Five dirty-detached states | 2 |

The independent correction recheck confirms all accepted findings resolved and the rejected test expansion reasonable, with no remaining concrete issue. It independently passes 13/13 and detects fetch/chmod injections across its broader selected test inventory. Final maintained results and the scope checker confirm the sole DW-428 closure below.

Final executed maintained commands (the same commands wired into Story Guards) both exit 0: procedure **13/13**, existing gitlink **77/77**, zero failures/skips. Logs are temporarily retained at `/tmp/dw-428/procedure-final.log` and `/tmp/dw-428/gitlinks-final.log`. The final commitlint rerun exits 0 with zero problems/warnings (`/tmp/dw-428/commitlint-final.log`). Actionlint, documented Bash syntax checks, Python parsing, whitespace and own-story gitlink checks pass.

The integrity check compares the final ledger against the canonical entry revision and requires exactly DW-428 to differ. Every original selected field must remain, either verbatim or under `pre-dw-428-*`; every replacement field must occur once, with existing resolution artifacts. It requires unchanged preamble apart from the new receipt, unchanged index bytes apart from the receipt and sole removed DW-428 row, all 656 IDs with 389 done / 267 open, and 239 rows covering every open ID exactly once. It also requires precisely the nine owned paths and unchanged dependency pointers, archive and sprint. Final result: **PASS**.
