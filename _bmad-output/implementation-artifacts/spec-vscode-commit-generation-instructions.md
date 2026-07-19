---
title: 'Configure VS Code commit-generation instructions'
type: 'bugfix'
created: '2026-07-19'
status: 'done'
route: 'one-shot'
---

# Configure VS Code commit-generation instructions

## Intent

**Problem:** VS Code commit-message generation did not receive Hexalith's canonical Git and semantic-release policy.

**Approach:** Configure the workspace generator to load `references/Hexalith.AI.Tools/hexalith-git-instructions.md` directly, preserving that file as the single source of truth.

## Suggested Review Order

- Directly supplies the canonical Git policy to VS Code's specialized commit-generation prompt.
  [`settings.json:2`](../../.vscode/settings.json#L2)
