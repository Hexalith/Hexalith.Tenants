---
title: 'Render protected release hooks through the real exec plugin'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'oneshot'
baseline_commit: 'd37f7f29256707f2f32dad07c6b03d51079d398b'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Protected Semantic Release must render its configured execution hooks and enforce secret, destination and prepared-package checks before publishing. Remove the obsolete preparation-only shell conditional that the exec plugin mistakes for JavaScript interpolation, and qualify the checked-in hook commands through the locked plugin rather than through direct shell substitution.

</frozen-after-approval>

## Implementation Notes

- Evidence: release run 40628873459 passed source admission, complete package preparation, approved production admission, protected source initialization and the real arm64 probe. It failed at verifyRelease with EVERIFYRELEASE / SyntaxError: Unexpected token ':' before any tag or package publication.
- Code map: @semantic-release/exec/lib/exec.js runs lodash-es template(config[cmd]) before starting the shell. The legacy Bash default expression in .releaserc.json is parsed as a JavaScript expression. The prepare and publish commands render correctly; their intended nextRelease.version placeholders and shell $NUGET_API_KEY expansion remain unchanged.
- Scope: replace verifyReleaseCmd with mandatory secret verification followed by authenticated destination preflight. Extend the existing dedicated Node/npm behavioral lane to invoke actual verifyRelease, prepare and publish plugin exports in isolated temporary repositories with harmless command stubs. No intent gap, irreversible implementation action, permission change, new API, dependency or submodule change. Actual publication is separately authorized by the user.
- Regression evidence: before the fix, the new real-plugin controls reproduced the exact template syntax failure in four tests, while the old proposal and publish-only controls passed. The production commands are never executed by these tests; credential variables are removed and all external command entry points are inert stubs.

- Implementation: the configured verification command now always requires publication credentials and destination preflight. The real prepared-package guard also drops the unused legacy skip mode, making its source/version checks mandatory. Hook fixtures use the normal unset environment by default, and test the legacy flag only in explicit refusal controls.

## Review Triage Log

- Medium, patched: forced legacy mode and a stubbed prepared-package guard masked the real guard's obsolete bypass. Default tests now use normal mode; the actual script refuses missing/version/source mismatch with and without the old flag.
- Medium, patched: empty-key transport alone could not catch quoting regressions. A runtime-generated nonempty inert value with whitespace and shell metacharacters now reaches the NuGet stub unchanged as one argument.
- Medium, patched: final publisher failures were not exercised. The real exec plugin now has a regression proving rejection after the container publisher stub fails.
- Follow-up independent review found no concrete regression. All 23 real-plugin/proposal/boundary tests passed, including the existing 14 proposal controls. All 252 package-governance Contracts tests, 3 actual missing-login-output guard tests and 6 executable workflow-closure tests passed without failures/skips. Shell syntax and diff whitespace checks passed. No package publication occurred in the failed verifyRelease run.
