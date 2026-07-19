# Investigation: VS Code commit-message instructions

## Hand-off Brief

1. **What happened.** VS Code generated commit subjects that violate Hexalith's written commit-message policy because the Source Control generator has no dedicated workspace instruction setting and the repository-wide Copilot entry point only contains an agent-oriented, multi-step lookup.
2. **Where the case stands.** Concluded with high confidence: official VS Code documentation and the tracked workspace configuration confirm the prompt-routing gap; the local commitlint configuration also accepts some messages rejected by the written policy.
3. **What's needed next.** Add concise, self-contained commit-generation instructions through `github.copilot.chat.commitMessageGeneration.instructions`, then align commitlint and a local `commit-msg` hook with the same policy.

## Case Info

| Field            | Value |
| ---------------- | ----- |
| Ticket           | N/A |
| Date opened      | 2026-07-19 |
| Status           | Concluded |
| System           | Hexalith.Tenants workspace; VS Code Source Control commit-message generation |
| Evidence sources | Tracked Copilot/VS Code configuration, Hexalith instructions, commitlint and semantic-release configuration, Git history, official VS Code documentation |

## Problem Statement

The user reports that VS Code commit-message generation does not produce a message that follows the semantic-release policy even though the Hexalith LLM instructions clearly constrain commit-message format.

## Evidence Inventory

| Source | Status | Notes |
| ------ | ------ | ----- |
| `.github/copilot-instructions.md` | Available | Repository-wide Copilot entry point; contains a procedural lookup rather than the commit policy itself. |
| `references/Hexalith.AI.Tools/hexalith-{llm,git}-instructions.md` | Available | Defines the written Conventional Commit policy. |
| `.vscode/` | Available | Tracks only `launch.json` and `tasks.json`; no workspace `settings.json` or commit-generation instruction setting. |
| `commitlint.config.mjs` | Available | Disables subject-case and body-line-length checks and raises the header limit to 150. |
| `.releaserc.json` | Available | Uses semantic-release commit analysis and emits `chore(release)` commits. |
| Local Git hooks | Available | Only sample hooks are present; no active `commit-msg` hook. |
| Official VS Code docs | Available | Documents a dedicated setting for commit-message generation instructions. |
| Exact VS Code/Copilot extension version and model request trace | Missing | Not required to establish the configuration gap, but useful to verify the final prompt after remediation. |

## Investigation Backlog

| # | Path to Explore | Priority | Status | Notes |
| - | --------------- | -------- | ------ | ----- |
| 1 | Trace instruction discovery | High | Done | Repository and dedicated commit-message channels are distinct. |
| 2 | Compare written and executable policy | High | Done | Confirmed drift in subject casing, header length, and `chore(release)`. |
| 3 | Verify generated prompt after remediation | Medium | Open | Use VS Code Chat Debug/customization diagnostics after configuration is added. |

## Timeline of Events

| Time | Event | Source | Confidence |
| ---- | ----- | ------ | ---------- |
| 2026-07-19 11:39 +02:00 | Commit `fc4c5eb` introduced relaxed commitlint rules and used `feat: Update ...`. | Git commit `fc4c5eb2b707f50c5e897fbbe6755308e7e2c0f4` | Confirmed |
| 2026-07-19 | Workspace inspection found no dedicated commit-message instruction setting and no active local commit hook. | `.vscode/`; `.git/hooks/` | Confirmed |

## Confirmed Findings

### Finding 1: The commit generator has no dedicated instructions

**Evidence:** `.vscode/` tracks only `launch.json` and `tasks.json`; repository scan found no `github.copilot.chat.commitMessageGeneration.instructions` setting.

**Detail:** Current VS Code documentation says commit-message custom instructions are supplied through `github.copilot.chat.commitMessageGeneration.instructions`, which accepts inline text or a Markdown file. Repository-wide instruction files are described as applying to chat requests.

### Finding 2: The Copilot entry point does not contain the commit policy

**Evidence:** `.github/copilot-instructions.md:9`; `.github/copilot-instructions.md:11`; `.github/copilot-instructions.md:15`; `.github/copilot-instructions.md:18`

**Detail:** The file tells an agent to run Git discovery, verify `.gitmodules`, and then read a file in `references/Hexalith.AI.Tools`. The Source Control commit generator is a specialized prompt, not an autonomous agent workflow that can be relied on to execute this bootstrap.

### Finding 3: The written policy is explicit in the downstream file

**Evidence:** `references/Hexalith.AI.Tools/hexalith-git-instructions.md:192`; `references/Hexalith.AI.Tools/hexalith-git-instructions.md:197`; `references/Hexalith.AI.Tools/hexalith-git-instructions.md:210`; `references/Hexalith.AI.Tools/hexalith-git-instructions.md:219`; `references/Hexalith.AI.Tools/hexalith-git-instructions.md:227`

**Detail:** The downstream file requires `<type>[optional scope][!]: <description>`, lowercase imperative description, no trailing period, an approximately 50-character subject, and no `chore` type.

### Finding 4: Executable validation contradicts part of the written policy

**Evidence:** `commitlint.config.mjs:3`; `commitlint.config.mjs:9`; `commitlint.config.mjs:11`; `.releaserc.json:25`

**Detail:** The repository disables `subject-case`, allows headers up to 150 characters, and configures semantic-release to create `chore(release)` commits. A direct `npx commitlint --verbose` check accepted `feat: Update commitlint configuration for sentence-case subjects and extended body length`, although the written Hexalith policy requires a lowercase description and rejects `chore`.

### Finding 5: There is no pre-commit enforcement loop

**Evidence:** `.github/workflows/commitlint.yml:5`; `.github/workflows/commitlint.yml:16`; `package.json:14`; local `.git/hooks/` inspection

**Detail:** Commitlint runs in GitHub Actions, but the local repository has no active `commit-msg` hook. VS Code generation does not infer policy from `commitlint.config.mjs` or `.releaserc.json`, and nothing locally rejects a bad generated message before commit creation.

## Deduced Conclusions

### Deduction 1: Prompt routing is the primary cause

**Based on:** Findings 1-3.

**Reasoning:** The policy exists only behind an agent-oriented lookup, while VS Code exposes a separate prompt channel specifically for commit generation. That channel is unconfigured in this workspace.

**Conclusion:** The Source Control generator is not receiving the Hexalith message rules in a reliable, supported way.

### Deduction 2: Policy drift explains why some wrong messages survive validation

**Based on:** Findings 4-5.

**Reasoning:** Even when the generated message has a Conventional Commit prefix, commitlint cannot determine whether the selected semantic-release type truthfully represents the change, and the repository has explicitly relaxed some written formatting constraints.

**Conclusion:** Adding generation instructions alone improves output but does not guarantee compliance; executable validation must be aligned with the canonical policy.

## Hypothesized Paths

### Hypothesis 1: VS Code ignores the Hexalith LLM instructions entirely

**Status:** Refuted

**Theory:** VS Code cannot discover repository-wide instructions.

**Supporting indicators:** The observed commit output violates the downstream policy.

**Would confirm:** Official documentation showing no repository instruction support.

**Would refute:** Official documentation showing `.github/copilot-instructions.md` support.

**Resolution:** VS Code does discover repository-wide instructions for chat. The defect is narrower: commit generation has a dedicated setting, and the discovered file contains only an indirection that requires agent/tool behavior.

### Hypothesis 2: Workspace-root selection compounds the issue

**Status:** Open

**Theory:** If VS Code opens a subfolder or a multi-root workspace, `.github/copilot-instructions.md` might not be at the active workspace root.

**Supporting indicators:** VS Code discovery is workspace-root-sensitive.

**Would confirm:** VS Code workspace diagnostics showing the file was not loaded.

**Would refute:** Diagnostics showing the correct file and dedicated setting in the generated model request.

**Resolution:** Not needed for the primary cause; verify during remediation.

## Missing Evidence

| Gap | Impact | How to Obtain |
| --- | ------ | ------------- |
| Exact generated model request | Confirms precisely which instructions were appended | Use VS Code Chat Debug/customization diagnostics while generating a commit message. |
| Active VS Code user-level settings | Could add, override, or conflict with workspace behavior | Inspect `github.copilot.chat.commitMessageGeneration.instructions` in the Settings UI. |

## Source Code Trace

| Element | Detail |
| ------- | ------ |
| Error origin | VS Code Source Control commit-generation prompt |
| Trigger | Select the Copilot Generate Commit Message action |
| Condition | No dedicated commit-message instructions; canonical policy exists only behind a procedural repository-instruction indirection |
| Related files | `.github/copilot-instructions.md`, `.vscode/settings.json` (missing), `references/Hexalith.AI.Tools/hexalith-git-instructions.md`, `commitlint.config.mjs`, `.releaserc.json` |

## Conclusion

**Confidence:** High

The primary root cause is confirmed: the repository has not configured VS Code's dedicated commit-message instruction channel, and its repository-wide Copilot file does not contain a self-contained commit policy. A second confirmed cause is policy drift: commitlint and semantic-release configuration permit constructs that the Hexalith LLM instructions reject. Workspace-root or user-setting issues may compound the behavior but are not required to explain it.

## Recommended Next Steps

### Fix direction

1. Put a short, self-contained Conventional Commit policy in a tracked Markdown file dedicated to commit generation.
2. Reference it from `.vscode/settings.json` through `github.copilot.chat.commitMessageGeneration.instructions`.
3. Reconcile `commitlint.config.mjs` and the semantic-release-generated message with the written policy.
4. Install or document a repository-managed `commit-msg` hook so invalid generated messages fail before commit creation; retain CI as the server-side gate.

### Diagnostic

After configuration, generate messages for a code fix, documentation-only change, and submodule bump. Inspect VS Code customization diagnostics/model request and validate each output with `npx commitlint --verbose` plus the stricter Hexalith rules.

## Reproduction Plan

1. Stage a submodule pointer update.
2. Run VS Code's Generate Commit Message action with the current workspace.
3. Observe that no tracked `commitMessageGeneration.instructions` setting supplies `build(deps): bump ...` and that the repository-wide entry point requires a multi-step lookup.
4. Add the dedicated setting and repeat; expect a Conventional Commit matching the dedicated policy.

## Side Findings

- Commit `fc4c5eb` uses `feat` for a mixed tooling/documentation/submodule change, which makes semantic-release treat the commit as a minor feature regardless of whether that release impact was intended.
- Older history contains plain-English subjects such as `Update Hexalith.Builds and FrontComposer submodules`; current commitlint rejects that exact shape, but the wrong semantic type can still pass when a valid prefix is present.
