---
title: '4.2 Align Source-Reference Contracts Assembly Identity'
type: 'bugfix'
created: '2026-09-21'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 88fe282d93e0a1c9da0c77b062d960d6cb13d291
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 4.2's source-reference real-handler integration cases fail during MVC controller discovery because the EventStore build graph records `Hexalith.Tenants.Contracts` with the EventStore assembly version while the copied Tenants assembly retains its own version. This prevents the end-to-end projection-provenance assertions from running.

**Approach:** Keep cross-repository source references versioned for their owning product so the MVC testing manifest and copied assembly agree, then prove all four source-reference real-handler metadata cases execute and pass without weakening package-reference behavior.

</frozen-after-approval>

## Implementation Notes

- Replaced the AppHost's four EventStore source-edge `AdditionalProperties="Version=..."` overrides with `GlobalPropertiesToRemove="Version"`. EventStore projects now select their repository-owned central version without leaking that version back through reverse references to Tenants projects.
- Added package-governance regression tests that require every build-forcing EventStore source edge, reject propagated `Version` values across attribute and child-element metadata forms, and bind the source-reference CI lane to the real-handler projection-provenance cases.
- Added a pull-request and main-branch source-reference workflow that builds the Debug project graph and executes all four real-handler projection-provenance cases.
- A reverse-reference-only adjustment inside EventStore worked for an isolated project but did not survive the complete AppHost graph. Those diagnostic edits were reverted; the owning EventStore gitlink and worktree are unchanged.
- Verified the source-reference integration project builds with warnings as errors, its MVC testing manifest records `Hexalith.Tenants.Contracts, Version=5.7.0.0`, and all four real-handler metadata cases pass.
- Verified the 24-test `PackageGovernanceTests` class passes after a Release build with warnings as errors.
- Verified the AppHost still builds successfully in Release/package mode with `UseHexalithProjectReferences=false` and warnings as errors.
- Verified workflow YAML/action linting, `git diff --check`, and the story gitlink guard; no `references/` pointer changed and the EventStore worktree is clean.

## Review Triage Log

| ID | Verdict | Route | Evidence |
|---|---|---|---|
| BH-1 | medium | patch | The initial guard only required one EventStore edge. It now requires all four build-forcing project IDs and rejects duplicate edges while continuing to govern any future EventStore edge. |
| BH-2 | medium | patch | The initial metadata check read attributes case-sensitively. It now combines attribute and child-element metadata and compares both metadata and property names case-insensitively. |
| BH-3 | medium | patch | Release/package CI does not exercise this Debug-only graph. A dedicated workflow now builds with `UseHexalithProjectReferences=true` and runs the four cases that reproduced the MVC application-parts failure. |
| BH-4 | false | reject | The filename intentionally preserves the requested Story 4.2 key for sprint synchronization and uses the workflow's `-2` collision suffix so the completed original story artifact is not overwritten; the title records the bounded follow-up scope. |
