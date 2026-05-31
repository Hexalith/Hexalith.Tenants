---
title: 'Add Hexalith.Builds Submodule'
type: 'chore'
created: '2026-05-31'
status: 'done'
route: 'one-shot'
---

# Add Hexalith.Builds Submodule

## Intent

**Problem:** The Tenants repository did not include `Hexalith.Builds` as a root-level submodule, so shared build assets were not available through the standard checkout path.

**Approach:** Add `Hexalith.Builds` as a root-level Git submodule using the existing Hexalith GitHub HTTPS convention, then update tracked agent guidance that enumerates allowed root submodules.

## Suggested Review Order

- Start with the actual submodule registration and pinned repository URL.
  [`.gitmodules:13`](../../.gitmodules#L13)

- Confirm the human-facing checkout command includes the new submodule.
  [`AGENTS.md:57`](../../AGENTS.md#L57)

- Confirm the Claude-facing checkout command stayed in sync.
  [`CLAUDE.md:57`](../../CLAUDE.md#L57)

- Verify generated project context no longer omits the new root submodule.
  [`project-context.md:33`](../project-context.md#L33)
