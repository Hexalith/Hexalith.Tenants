---
title: 'Calculate release candidates without Git write access'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'oneshot'
baseline_commit: '9202960cc31e4d6f4fa82a9f6a957ec0e12dba18'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Release preparation fails because Semantic Release dry-run authenticates a push even though the job deliberately has read-only authority. Compute the stable main-branch proposal from local reachable tags and commits using the locked Semantic Release configuration, commit-analysis pipeline, and version helpers. Preserve publication preflight, the credential-free preparation boundary, and the protected publisher's independent version and source revalidation.

</frozen-after-approval>

## Implementation Notes

- Evidence: Release run 40616957325 passed exact-source and registry-floor preflights, then preparation failed with EGITNOPERMISSION at semantic-release's unconditional verifyAuth push dry-run. No tag, package, or container was published.
- Code map: scripts/build-release-candidate.sh invokes the whole Semantic Release lifecycle; its installed lib/get-config.js, get-last-release.js, get-commits.js, and get-next-version.js expose the existing proposal semantics independently of verifyAuth. The repository supports only stable main and vMAJOR.MINOR.PATCH release tags. The protected require-prepared-release-packages.sh independently checks version/source before publication.
- Scope: introduce a read-only proposal helper, replace dry-run output parsing, retain authenticated publication preflight in the protected Semantic Release lifecycle, and add behavioral tests with a Git command guard and unavailable remote. No package or permission change, new public API, or irreversible implementation action.
- Validate patch/minor/major/no-release/first-release behavior, reachable tag selection, unsupported branch configurations, and refusal of all Git write/network commands. Run focused repository release governance checks. Publication remains the separately authorized task after exact-source CI.
- Implementation: calculate-release-version.mjs loads the locked Semantic Release configuration/analysis/version helpers, selects reachable stable tags, rejects unsupported profiles and shallow histories, and emits only the version on stdout. The shell performs the versioned build/pack without publication lifecycle hooks and removes the redundant unversioned build. The protected publisher retains verifyRelease preflight and the immediate pre-write publish preflight.
- CI: a dedicated blocking Story Guards job installs Node 24 and locked npm tooling before running the proposal tests; existing Python-only release-guard discovery remains independent.
- Verification: 14 offline/read-only proposal and shell-boundary tests passed, including the checked-in release configuration and configured exec analyzer. All 728 Contracts tests and 70 existing Python release guards passed without skips; shell and Node syntax checks passed. Current proposal is 5.11.0. No publication occurred in the failed preparation run.

## Review Triage Log

- Medium, patched: the original test filename would join a Python-only job without npm dependencies. Moved to a dedicated blocking job with pinned Node setup and npm ci; no missing-tooling skip is used.
- Medium, patched: configured exec analyzers needed stdout/stderr in the plugin context. Both streams use stderr, with a real exec-analyzer regression proving version-only stdout.
- Low, patched: incomplete Git ancestry could produce a misleading proposal. Explicit read-only shallow-history refusal and a negative test prevent it.
- Medium, patched: initial tests omitted the real configuration and shell guard boundary. Integration tests now prove actual configuration analysis, build-failure propagation, helper-failure propagation, and publication-credential refusal before work.
- High, patched: follow-up inspection showed the pinned destination preflight unconditionally requires authenticated registry enumeration. Calling it from credential-free preparation would fail next, even after fixing push authentication. Preparation no longer calls the authenticated hook; a failing sentinel proves the full candidate build/pack succeeds without invoking it. Existing protected verifyRelease and publish hooks remain unchanged and blocking before publication.
- Follow-up verification: an independent reviewer found no further defect; all 14 proposal/boundary tests, 70 Python release guards, and 298 focused release-governance Contracts tests passed with zero failures or skips. Shell syntax and diff whitespace checks also passed.
