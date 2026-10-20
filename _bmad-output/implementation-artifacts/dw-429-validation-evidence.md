# DW-429 validation evidence — 2026-10-20

Entry revision: `ce88a70b6af777c059b5fb4335d429b9272911bd`; clean `main` at activation. DW-429 was the first maintenance-index item, P1/actionable.

## Behavior and scope

The three tracked pushall manifests resolve origin's live advertised default after the existing fetch and before committing or switching. Exactly one valid branch/OID pair is required. The canonical fetched origin ref must be direct, match that OID and identify a commit. Cached origin/HEAD and local main/master names cannot substitute for live evidence. Missing, malformed, ambiguous, unavailable, indirect or stale evidence explicitly skips subsequent numbered steps for that repository. Fetch may already have updated tracking refs; the resolver itself preserves HEAD, index and worktree. Other repositories continue.

The checkout block explicitly switches to an existing local default without resetting it, or creates the missing local branch from the validated full tracking ref. Creation disables automatic upstream inference so custom fetch mappings do not break it; the existing downstream operations already use explicit origin/default refs. Narrow fetch coverage without a matching canonical ref refuses and requests inspection/rerun instead of forcing a ref update. The established attachment gate, merge behavior and deletion leases remain unchanged.

This task edits a skill and does not invoke `/pushall` on the shared workspace. Git fixtures use temporary repositories and a local bare remote. No application, dependency, persistence, deployment or Aspire execution is relevant. The user authorized committing and pushing the completed task.

## Executed verification

From the repository root:

```bash
python3 tests/scripts/test_pushall_default_branch.py
python3 tests/scripts/test_pushall_detached_head.py
python3 tests/scripts/test_validate_story_gitlinks.py
actionlint .github/workflows/story-guards.yml
```

Maintained results before independent review: **22/22**, **13/13**, **77/77**, zero failures/skips; actionlint exits 0. The first new-lane run passed all 22 new tests but also discovered 13 imported attachment tests (35 total). Importing the module instead of its TestCase class removes that duplicate execution; both lanes remain independently invoked in Story Guards. Logs are temporarily retained under `/tmp/dw-429/`.

The new lane extracts and executes the documented resolution and checkout Bash blocks from all three manifests. It exercises master/main coexistence, stale/absent/dangling cached HEAD, nonstandard slash defaults, missing/existing local defaults, custom/narrow fetch mappings, absent configured origin despite a reachable local path, unavailable origin, unborn/detached remote HEAD, ambiguous/malformed advertisements, invalid branch names, failures from every inspection command even with success-shaped stdout, missing/indirect/noncommit fetched refs and concurrent remote advancement. Refusal snapshots compare HEAD attachment, index bytes, refs, worktree bytes/modes/times/symlinks, FETCH_HEAD and remote refs. Git invocation tracing rejects mutation inside resolution.

An end-to-end local-remote control resolves master, commits dirty attached work, executes the documented branch-creation block, merges and pushes into master, then safely prunes the nondefault main with its exact lease. It verifies committed ancestry, both changed file contents, removal of main and preservation of remote HEAD/master. This qualifies the changed documented commands and downstream use of their output; it does not claim autonomous execution of the entire skill.

Five isolated resolver variants fail their matching controls with zero test errors:

| Variant | Matching checks | Intended assertion failures |
| --- | --- | --- |
| Restore local-main/master preference | Remote master and nonstandard default | 2 |
| Always refuse | Live main with master also present | 1 |
| Suppress tracking symbolic-target evidence | Indirect fetched ref | 1 |
| Remove fetched-ref equality check | Unfetched default | 1 |
| Remove configured-origin check | Local path named origin plus matching leftover ref | 1 |

The temporary checker command is `python3 /tmp/dw-429/check-variants.py`. It changes only isolated manifest copies, uses the maintained test methods and requires exact failure counts with no errors. Its initial blanket-refusal run exposed a missing trace-file assumption in the test harness; allowing an empty trace corrects that harness issue and all five final variants meet the expected results. The legacy variant reproduces the entry procedure's actual preference logic; no full original skill execution is claimed.

## Manifest validation limit

Each command exits 1 with the same inherited diagnostic:

```bash
python3 /home/administrator/.codex/skills/.system/skill-creator/scripts/quick_validate.py .agents/skills/pushall
python3 /home/administrator/.codex/skills/.system/skill-creator/scripts/quick_validate.py .claude/skills/pushall
python3 /home/administrator/.codex/skills/.system/skill-creator/scripts/quick_validate.py .github/skills/pushall
```

`Unexpected key(s) in SKILL.md frontmatter: disable-model-invocation. Allowed properties are: allowed-tools, description, license, metadata, name`

A fresh validator run against an isolated entry-revision manifest reproduces exit 1 and this same diagnostic; the three entry manifests are identical. The frontmatter and established explicit-only invocation policy are preserved; full-validator success is not claimed.

## External checkout drift and attribution

Six root-declared submodule checkouts moved after the clean entry check, without any task command mutating the workspace dependencies:

| Path | Recorded pointer | Observed external checkout |
| --- | --- | --- |
| references/Hexalith.Builds | 5c04dab754e269f52306b9582cacd7cf1dc1540f | 6a38cb0f2e74977d7457fc59e6af79457ed8196a |
| references/Hexalith.EventStore | fcf78d6cafb6c0c9f52dc350a9a09aff54ded288 | c9f47dd5ba72918445dfd24a674a5b2c310589bd |
| references/Hexalith.FrontComposer | c4f5c12e32182739ce1478a6cdefe602c4daeb18 | d52526a4bd1272760543e6a65840dc2b0ed93cee |
| references/Hexalith.McpCli | 447085c42ec606f642240c4cae0415e7c2f97467 | b616db3092a9b1c5af196538a3de37a5c7f2d7ca |
| references/Hexalith.Memories | 3b895163424f9a4b5fb9cc579ec96fdd79f57546 | c082f8151328b1ac91939cda8733ab78ae68e1c4 |
| references/Hexalith.Platform | 2d5178f15ea0c36157ea2c56259a8479b6b8cea5 | bb81fc9e98533246cfeed43ffcce08866a2a57f6 |

`python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-429-pushall-remote-default.md` exits 1 and reports all six as UNDECLARED. That working-tree failure is retained rather than claiming these pointers belong to this story or resetting external work. An additional explicit `--ref HEAD` invocation also exits 1: the validator intentionally includes working-tree checks for the literal HEAD value. During verification, concurrent `/pushall` committed and pushed an intermediate snapshot as `fd155028bddd61f6b20a2750c797199ec9778246`, including the initial task manifests/workflow/spec/tests and all six external pointers. That commit is preserved. The true entry baseline remains unchanged, and the full story range therefore retains its six undeclared-pointer failure even at a canonical committed endpoint. The task follow-up will stage only its explicit remaining paths and separately verify its own diff has no dependency movement; that narrower evidence does not replace the failing whole-story provenance check.

## Commit policy and ledger preservation

Pinned `@commitlint/cli@21.2.3` validates both full candidates before use:

```bash
node_modules/.bin/commitlint --edit /tmp/dw-429/fixture-message.txt --verbose
node_modules/.bin/commitlint --edit /tmp/dw-429/commit-message.txt --verbose
```

Both exit 0, zero problems/warnings. The fixture subject is `build: exercise pushall default branch selection`; the task subject is `fix(tooling): honor the remote default branch during pushall`.

Only DW-429 closes. Original replaced fields remain under `pre-dw-429-*`, every unrelated record remains unchanged, and only the DW-429 index row is removed. Expected final inventory: 656 IDs, 390 done/266 open, 238 maintenance rows covering every open ID exactly once. The archive, sprint, existing release holds and externally changed checkout pointers are preserved. Independent review, final integrity checks and commit endpoint audit follow below.

## Independent review corrections and final qualification

Blind Hunter returned seven findings, all patched; the spec records every verdict and evidence. A separately reproduced symbolic local default issue is patched as well. Default merge/push and branch deletion now use fully qualified quoted refs; real tag collisions cannot substitute a different source or target. Exact HEAD filtering ignores valid unrelated suffix matches, while branch capture permits Unicode spacing accepted by Git. Local default switching refuses symbolic refs and inspection failures. Config snapshots and precise read-only command arguments detect the reviewer's demonstrated push-URL mutation.

The maintained suite now passes **31/31**, zero failures/skips. New cases execute the documented merge and push commands, preserve shell metacharacters, accept U+2003/U+3000 branches, require fetched/live equality when both commits exist locally and run the resolver against a real SHA-256 origin/clone. Independent correction recheck also passes **31/31**, verifies identical manifests and whitespace, reproduces detection of the injected push-URL mutation, and accepts every correction without a remaining concrete finding. No finding is deferred. Its acceptance covers these local fixtures, not full autonomous skill execution or external-pointer provenance.

The isolated-variant checker additionally rejects whole-output HEAD matching, locale whitespace restrictions, short merge and push refs, and a resolver config mutation. These checks supplement the five earlier variants; all failures must be assertions with zero harness errors. Exact final output is temporarily retained at `/tmp/dw-429/variants.log`.

The original attachment **13/13** and gitlink **77/77** results remain valid for their unchanged code. Bash/Python parsing, actionlint, byte-identical manifests and preserved frontmatter pass. Fresh entry-manifest validation confirms the inherited unsupported-key limitation. Ledger integrity confirms exactly DW-429 changes, historical fields preserved, 656 IDs/390 done/266 open and 238 exact-covering rows; index/preamble/archive/sprint preservation passes.

Concurrent commit `fd155028bddd61f6b20a2750c797199ec9778246` is retained with its intermediate task snapshot and six external pointers. The full canonical story-endpoint guard remains a known attribution failure; no passing whole-story gitlink claim is made. The task follow-up is limited to explicit task paths, and its parent-to-commit diff must have no `references/` changes. This is independent evidence of follow-up scope, not a workaround or change to the guard.

Final preservation/syntax/actionlint/whitespace checks pass. The ten isolated variants produce 12 intended assertion failures in total, zero errors. Fetching origin without submodule recursion reports `HEAD...origin/main` as `0 0` at `fd155028bddd61f6b20a2750c797199ec9778246`. Auditing that explicit canonical revision exits 1 with the six preserved undeclared pointer movements, confirming that this is now recorded mixed history rather than remaining checkout drift. Commitlint revalidation again reports zero problems/warnings before the task follow-up.
