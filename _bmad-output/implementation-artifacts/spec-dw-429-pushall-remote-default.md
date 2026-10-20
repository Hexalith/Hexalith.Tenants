---
title: 'DW-429: Honor the remote default branch during pushall'
type: 'bugfix'
created: '2026-10-20'
status: 'done'
route: 'oneshot'
baseline_commit: 'ce88a70b6af777c059b5fb4335d429b9272911bd'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The tracked `/pushall` procedure prefers local `main` or `master` over the remote's actual default. It can merge, push and prune against the wrong branch.

**Approach:** Resolve origin's live advertised default first, validate its branch and fetched commit, and use that exact default throughout the existing procedure. Refuse missing, ambiguous, malformed or unavailable evidence before committing or switching; preserve DW-428's attachment gate. Keep all three tracked copies consistent and qualify the documented commands in disposable local Git repositories. Close only DW-429 after verification and preserve historical findings and release holds.

</frozen-after-approval>

## Implementation Notes

- Entry revision `ce88a70b6af777c059b5fb4335d429b9272911bd`, clean `main`; DW-429 is the first maintenance-index row and is P1/actionable. This is a procedure edit, not an invocation of `/pushall` on the workspace.
- No unresolved intent gap or irreversible implementation action. User authorization includes committing/pushing the finished task. Footprint: three pushall manifests, a stdlib Git-fixture lane and its Story Guards invocation, selected ledger/index records, this spec and validation evidence. No application, dependency, persistence or Aspire execution is relevant.
- Reuse the per-repository fetch and merge/push/prune flow. Query live `ls-remote --symref origin HEAD`; cached origin/HEAD and local conventional names are insufficient. Require exactly one advertised branch and OID, a valid branch name and a direct canonical origin tracking ref at the same commit. Missing/narrowed fetch coverage or a changed remote tip causes an explicit skip/rerun, not a guessed branch or forced fetch. Create a missing local default from that validated remote-tracking ref in the existing checkout step.
- Failed resolution preserves the post-fetch HEAD/index/worktree and skips subsequent numbered steps; fetch may already have updated tracking refs. A skipped repo is exempt from the default-branch return when the default cannot be established. Accepted repos keep the established downstream merge/prune exclusions and leases.
- Tests execute extracted documented Bash against local bare remotes, with both conventional branches, custom/slash defaults, stale cached HEAD, absent local default, absent/ambiguous/malformed advertisements, failed inspection and fetched-ref mismatches. Verify preservation on refusal and inspect actual local-remote merge/push/prune outcomes on success. Retain the original detached gate and 77-test gitlink lane.
- Validate Python lanes, Bash syntax, actionlint, skill manifest validator, exact ledger/index preservation, own-story gitlink guard, whitespace and pinned commitlint. Preserve existing explicit-only invocation policy if the manifest validator repeats its known unsupported-key refusal.
- Initial maintained verification passed 22 new tests, 13 retained attachment tests and 77 gitlink regressions; five isolated faulty resolver variants produce six intended assertion failures with zero errors. Python/Bash syntax, actionlint, manifest identity/frontmatter and ledger/index integrity checks pass. The known manifest-validator unsupported-key refusal reproduces on the entry manifest.
- External activity moved six dependency checkouts after activation, then committed/pushed an intermediate task snapshot and those pointers as `fd155028bddd61f6b20a2750c797199ec9778246`. Preserve that commit and the true entry baseline. Default and explicit-HEAD story guard runs report six undeclared pointer movements; do not misattribute or reset external changes to obtain a PASS. Complete remaining corrections/evidence in an explicit-path follow-up commit and verify its own diff contains no dependency pointer changes.
- Independent review returned seven findings, all addressed below. Step 5 and downstream default uses now carry fully qualified quoted refs, exact HEAD records are separated from unrelated suffix matches, and Git decides Unicode branch validity. Added collision, metacharacter, Unicode, locally present mismatch and real SHA-256 controls. A separately reproduced symbolic local default could attach HEAD to another branch; the checkout block now refuses it and inspection failures. Resolver traces now constrain `remote get-url` and read-only symbolic-ref argument shapes, and refusal snapshots include Git config.

- Final independent correction recheck accepts every finding and separately passes 31/31 tests. No review deferral remains. Ledger/index preservation confirms 656 IDs, 390 done/266 open and 238 exact-covering maintenance rows. The whole-story provenance failure caused by concurrent fd155028 remains explicitly recorded; the follow-up commit will contain no dependency movement.

## Review Triage Log

Blind Hunter: 40.645 kB; `min(floor(sqrt(40.645) + 1), 10) = 7`; seven findings, all retained below. No finding is deferred.

| Finding | Verdict and action | Evidence |
| --- | --- | --- |
| A local tag can shadow the short remote merge ref. | Medium; patch. | Reviewer reproduced master advancing to an unrelated `origin/master` tag commit. Step 5 uses the validated full tracking ref; a regression executes that documented command and requires the fetched OID. Push source/destination and other default refs are also explicit. |
| Unquoted downstream branch substitutions permit shell interpretation. | Medium; patch. | Git accepts metacharacters in branch names. Downstream commands now use quoted variables/full refs; a real default named `release;$(false)` survives documented switch/merge/push execution literally. |
| Unrelated refs ending in HEAD make valid discovery fail. | Medium; patch. | Reviewer reproduced a real `topic/HEAD` tag adding an extra ls-remote record. Filter exact HEAD records and keep strict validation of that evidence; a real branch and tag with the suffix now coexist with valid main discovery. |
| Locale whitespace matching rejects valid Unicode branch names. | Medium; patch. | Git accepts U+2003/U+3000 names that the initial regex rejects. Capture excludes only structural tab/newline bytes and leaves validity to Git; both published defaults are tested. |
| The trace permits remote configuration mutation and snapshots omit config. | Low; patch. | Review injected `remote set-url --push` without detection. Require the exact read-only remote argv and include config bytes in resolver snapshots. Restrict symbolic-ref to a read operation as well. |
| Concurrent advancement only tests a missing local object. | Low; patch. | Added a fetch into an unrelated tracking ref so both commits exist locally while canonical origin/main remains stale. It must refuse on fetched/live inequality. The original absent-object case remains. |
| No real SHA-256 fixture exercises the accepted 64-digit OID. | Low; patch. | Added a local SHA-256 bare origin and clone; assert a 64-digit commit and run every manifest's actual resolver. |
| Additional root check: symbolic local default switches to another local branch. | Medium; patch. | An isolated refs/heads/master -> refs/heads/main ended with HEAD attached to main. Refuse symbolic local defaults and attachment-inspection errors before switching, with preservation regressions. |
