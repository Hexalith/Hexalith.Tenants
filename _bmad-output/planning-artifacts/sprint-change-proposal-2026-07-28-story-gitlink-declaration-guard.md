# Sprint Change Proposal — Story Gitlink Declaration Guard

- **Date:** 2026-07-28
- **Trigger:** Sprint action item `code-review-2026-07-27-submodule-bump-in-story-commits`
- **Requested by:** Administrator (Jérôme Piquot)
- **Scope classification:** Minor — process control, implemented directly
- **Status:** Implemented, pending approval

---

## 1. Issue Summary

Story commits move root-declared `references/` submodule pointers without declaring
them, while the story's own Completion Notes assert the opposite.

This is not a one-off. It is a repeating pattern with three confirmed occurrences:

| # | Story | Commit | Undeclared gitlink movement |
| - | ----- | ------ | --------------------------- |
| 1 | 1.4 My Tenants Self-Audit | `41e047e` | `Hexalith.Builds` `7708256`→`dfb2f3f`, `Hexalith.EventStore` `41f5ed0`→`4245f0f`, `Hexalith.Memories` `3b1ae85`→`ae591ce` |
| 2 | 1.6 Read-Only Tenant Configuration | `ec7ec8c` | `Hexalith.EventStore` `c6b72ca`→`440ff4c` |
| 3 | Guard introduction (this change) | `c407c9e` | `Hexalith.Builds` `1b1c0b0`→`53d53ae` |

**Occurrence 3 was discovered during this correct-course run.** The commit that
introduced the guard — subject `feat: add script to verify story submodule pointer
changes` — itself bundled an undeclared `Hexalith.Builds` bump. The pattern reproduced
while it was being fixed, which is the strongest available evidence that a checklist
sentence alone would not have been sufficient.

### Why the existing controls did not catch it

- **`epics.md` Story 1.0 AC (line 332)** already requires that *"no root-declared
  submodule source is modified without a separately authorized task."* It says
  **source**. Moving a gitlink pointer is not editing submodule source, so a pointer
  bump slips past the literal wording.
- **`architecture.md` (line 261)** deliberately holds that *"submodule revisions are
  implementation state rather than architecture invariants."* Architecture correctly
  does not pin gitlinks, so no architectural rule was violated.
- The **Definition of Done** required a complete File List but had no mechanical check
  that the File List agreed with the actual diff.

The gap is therefore a **process control gap**, not a product-requirement or
architecture defect.

### Aggravating factor: unreliable baselines

Story 1.6's `baseline_commit` (`b73093b`) pointed at a mid-story implementation commit.
Its File List was computed from that narrowed range and omitted 23 of 49 delivered
paths. A guard that trusts `baseline_commit` blindly can be silently narrowed, so the
check must also assess the baseline itself.

---

## 2. Impact Analysis

| Artifact | Impact |
| -------- | ------ |
| **PRD** | None. This is a delivery-process control, not product scope. |
| **Architecture** | None. `architecture.md` intentionally treats submodule revisions as implementation state; the guard does not make them invariants. |
| **UX** | None. |
| **Epics / Stories** | No epic scope, sequencing, or AC changes. Every future story inherits the new completion gate. |
| **Sprint tracking** | Action item `code-review-2026-07-27-submodule-bump-in-story-commits` closed. |
| **Process artifacts** | Definition of Done, BMad skill customization, canonical project rules, and `scripts/`. |

**Technical impact:** none on shipped code. No `src/` or `tests/` file changes, no
package, contract, endpoint, or dependency changes.

---

## 3. Recommended Approach

**Direct Adjustment.** Add a mechanical, agent-independent check at story completion
and wire it into the workflows that close stories. No rollback, no MVP change.

Rejected alternatives:

- **Rollback** — not viable and not useful. The story 1.6 bump is absorbed into `main`
  (nine later commits touched the gitlink), and rolling back stories would not prevent
  recurrence.
- **CI hard gate** — considered and deliberately deferred. A CI job failing any commit
  that touches a gitlink without a `build(deps)` scope would also catch human and
  non-BMad commits, but adds a hard gate with false positives on legitimately bundled
  submodule work. Revisit if occurrences continue after this control.

**Fail-closed posture (decided):** a story with no usable `baseline_commit` cannot
prove anything about its own gitlinks, so the check reports FAIL rather than passing
silently. The stories most likely to drift are exactly those created without a
baseline.

---

## 4. Detailed Change Proposals

### 4.1 New — `scripts/validate-story-gitlinks.py`

Stdlib-only, matching the existing `validate-*.py` convention in `scripts/`.

- Reads `baseline_commit` from the story's YAML frontmatter.
- **Fail-closed** when the baseline is missing, `NO_VCS`, not a commit, or not an
  ancestor of the compared ref.
- Diffs with `--raw --ignore-submodules=dirty` over `references/`, so it flags
  **pointer** movement and not dirty submodule working trees.
- **A declaration is a File List (or Completion Notes) list item whose entire content
  is the path.** Prose mentions do not declare. This is essential: story 1.4's notes
  said the drift *"was left untouched"* and *"Preserved / not modified by this story"*.
  A naive substring match reads those denials as declarations and would have passed
  2 of its 3 real bumps.
- Prints an exact `git checkout <old-sha> -- <path>` remediation per undeclared entry.
- **Warns when `baseline_commit` itself touches the story's own files** — a mid-story
  baseline. This independently reproduced the story 1.6 baseline defect.
- Exit codes: `0` pass, `1` fail, `2` usage.

### 4.2 Modified — `.claude/skills/bmad-dev-story/checklist.md`

Added under *Documentation & Tracking*:

> - [ ] **Submodule Pointers Declared:** `python3 scripts/validate-story-gitlinks.py <story-file>`
>   exits 0. Every `references/` gitlink that moved since `baseline_commit` is either
>   declared as a File List entry (with the reason in Completion Notes) or reverted to
>   its baseline SHA. Never claim `references/` was untouched without running this check.

### 4.3 New — `_bmad/custom/bmad-dev-story.toml`

Committed team override, which **survives BMad skill updates** (the skill directory
does not). Adds two `persistent_facts` and an `on_complete` instruction that runs the
check, blocks completion on exit 1, explains the mid-story-baseline warning, and
requires the verdict be reported verbatim.

### 4.4 New — `_bmad/custom/bmad-code-review.toml`

Three `persistent_facts` making the reviewer verify gitlinks independently, treat a
non-zero exit as a finding, and distrust the story's own prose about `references/`.

### 4.5 Modified — `_bmad-output/project-context.md`

One rule appended to *Development Workflow Rules*, beside the existing submodule
rules, so every agent that loads project context inherits it.

### 4.6 Modified — `_bmad-output/implementation-artifacts/sprint-status.yaml`

Action item `code-review-2026-07-27-submodule-bump-in-story-commits` set to `done`
with a `resolution` field recording what was built and how it was verified.

---

## 4b. Follow-up (2026-07-28, after initial approval)

Approval review surfaced that the original wiring covered only one of **three**
completion paths, and that the check had a real blind spot. Both are now closed.

### 4b.1 All three completion paths are covered

`bmad-dev-auto` and `bmad-quick-dev` are independent lanes that close work without
ever invoking `bmad-dev-story` or its Definition of Done. Wiring only `bmad-dev-story`
left the unattended lane completely unguarded.

| Path | Closes work by | Baseline | Now covered by |
| ---- | -------------- | -------- | -------------- |
| `bmad-dev-story` | Status → `review` (Step 9 DoD) | Writes `baseline_commit` at Step 4 | `_bmad/custom/bmad-dev-story.toml` |
| `bmad-quick-dev` | `step-05-present` | Already writes `baseline_commit` | `_bmad/custom/bmad-quick-dev.toml` |
| `bmad-dev-auto` | `step-04-review` sets spec `status: done` | **None** — now captured at activation | `_bmad/custom/bmad-dev-auto.toml` + `spec-template.md` |

`bmad-dev-auto`'s override adds an `activation_steps_prepend` that captures
`git rev-parse HEAD` **before** implementation starts, and `spec-template.md` gains a
`baseline_commit` field, so the fail-closed posture is actionable rather than noisy.

### 4b.2 Defect fixed: uncommitted pointer drift was invisible

The check resolved `--ref` to a commit and diffed commit-to-commit, so a submodule
whose checked-out commit had moved but was **not yet committed** did not register.
Those are the dangerous ones — a later `git add -A` sweeps them into an unrelated
commit, which is the mechanism behind every occurrence here.

Found from live evidence: mid-session the working tree held two unstaged pointer
moves (`Hexalith.EventStore` `150216c`→`5a1d277`, `Hexalith.Memories`
`327d1a9`→`1868c8f`) that the check reported as clean.

Now, a default run (`--ref HEAD`) compares baseline → **working tree**; an explicit
`--ref <sha>` still audits that commit alone. A secondary fix resolves the submodule's
real checked-out SHA, because `git diff --raw` emits a null new-SHA for unstaged
submodule moves, which previously rendered as the misleading `-> removed`.

## 5. Verification

Each case runs the real script against the real repository history.

| Case | Scenario | Expected | Result |
| ---- | -------- | -------- | ------ |
| A′ | Story 1.6 file **as it existed** at `ec7ec8c` | FAIL | **FAIL** — `references/Hexalith.EventStore` UNDECLARED |
| A | Story 1.6 file today (File List corrected) | PASS | **PASS** — declaration recognised, no over-rejection |
| B | Story 1.4 file as it existed at `41e047e` | FAIL | **FAIL** — all 3 bumps UNDECLARED; denial prose correctly rejected |
| C | Story with no `baseline_commit` | FAIL | **FAIL** — fail-closed with remediation |
| D | Story with no drift in range | PASS | **PASS** |
| E | Baseline predating the submodules | Honest output | `absent at baseline`, no bogus revert command |
| F | The guard's **own** commit `c407c9e` | FAIL | **FAIL** — `references/Hexalith.Builds` `1b1c0b0`→`53d53ae` UNDECLARED |
| G | Live **uncommitted** pointer drift (§4b.2) | FAIL | **FAIL** — EventStore + Memories, SHAs match `git submodule status` |
| H | That same drift, declared in File List | PASS | **PASS** |

Both historical occurrences are caught at exactly the moment the Definition of Done
would have run, and the third is caught in the commit that introduced the guard.

---

## 6. Declaration — `c407c9e` Builds pointer (RESOLVED 2026-07-28)

**Owner decision: declare, do not revert.** `references/Hexalith.Builds` `1b1c0b0` → `53d53ae`
is hereby declared as an intended move to the approved container smoke fix
(`fix(release): run the container smoke in a non-production environment`), matching the
stated intent of `5efbbe7`. The pointer stays at `53d53ae`.

`c407c9e` is already on `origin/main`, so its message cannot be amended without rewriting
published history. The declaration is therefore forward-only: this section is the record.
Suggested trailer for the commit that lands this proposal:

```
Declares: references/Hexalith.Builds 1b1c0b0 -> 53d53ae, moved by c407c9e
(approved container smoke fix; HexalithEventStoreVersion unchanged at 3.83.0).
```

The evidence behind that decision follows.

### Evidence

`c407c9e` bundled `references/Hexalith.Builds` `1b1c0b0` → `53d53ae` under a `feat:`
subject that does not mention it. This working tree has **not** been altered to
undo it — reverting another author's committed change is the owner's call, not the
agent's.

**The bump appears intended, but landed in the wrong commit.** Tracing the pointer:

```
8f331d6  Fix/release guard compare polarity (#40)        Builds=1b1c0b0
0faaa64  build(deps): bump Hexalith.EventStore submodule Builds=1b1c0b0
5efbbe7  fix(ci): re-pin the approved Builds identity…   Builds=1b1c0b0   <- did NOT move it
c407c9e  feat: add script to verify story submodule…     Builds=53d53ae   <- moved it here
```

`5efbbe7` is titled *"re-pin the approved Builds identity to the container smoke fix"*,
and `53d53ae` is exactly *"fix(release): run the container smoke in a non-production
environment"*. The move that `5efbbe7` describes therefore landed one commit later, in
an unrelated `feat:` commit. That is the riding-along pattern itself, not a wrong target.

Two safety checks already performed:

- `53d53ae` still pins `HexalithEventStoreVersion=3.83.0`, **identical** to `1b1c0b0`,
  so the `NU1102` restore blocker recorded for the earlier proof-pin is *not*
  reintroduced.
- `53d53ae` is `v4.23.0-11-g53d53ae` on the Builds remote.

Recommended: **declare it** — follow up with an explanatory `build(deps)` commit (or
amend `c407c9e`) recording that the Builds pointer moved to the approved container
smoke fix. A revert is available if preferred:
`git checkout 1b1c0b0 -- references/Hexalith.Builds`.

Also uncommitted: two `check=False` lines added to `scripts/validate-story-gitlinks.py`
after `c407c9e`, making the manual `returncode` inspection explicit.

---

## 7. Implementation Handoff

**Scope: Minor.** All changes are implemented in this working tree.

| Recipient | Responsibility |
| --------- | -------------- |
| **Amelia (Developer)** | Run the check at story completion; declare or revert every flagged pointer. |
| **Winston (System Architect)** | Decide the `c407c9e` Builds bump (Section 6); decide whether a CI gate is warranted if occurrences continue. |

**Success criteria**

- No story reaches `review` with an undeclared `references/` pointer change.
- Any story claiming `references/` was untouched has a passing check behind the claim.
- A fourth occurrence triggers escalation to the deferred CI gate.

**Not included (deliberate):** no CI enforcement, no `epics.md` AC rewording, no
architecture change, no changes to shipped code or tests.
