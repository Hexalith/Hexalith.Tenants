---
title: 'DW-429: Honor the remote default branch during pushall'
type: 'bugfix'
created: '2026-10-20'
status: 'in-progress'
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
