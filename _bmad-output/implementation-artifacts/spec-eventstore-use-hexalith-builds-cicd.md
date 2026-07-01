---
title: 'EventStore use Hexalith.Builds CI/CD'
type: 'chore'
created: '2026-07-01'
status: 'done'
baseline_commit: '3dc24d4aee5be3d5dd9a1ce8c057d39889a4d379'
baseline_repository: 'references/Hexalith.EventStore'
context:
  - references/Hexalith.EventStore/_bmad-output/project-context.md
  - references/Hexalith.Builds/CLAUDE.md
---

<frozen-after-approval reason="human-owned intent -- do not modify unless human renegotiates">

## Intent

**Problem:** `Hexalith.EventStore` still owns hand-written GitHub Actions setup, NuGet cache, and Dapr bootstrap logic in its CI/CD workflows even though `Hexalith.Builds` now provides shared composite actions for those concerns. This leaves EventStore behind the current Hexalith CI/CD pattern and duplicates setup behavior already centralized in `Hexalith.Builds`.

**Approach:** Update EventStore GitHub workflows to consume `Hexalith/Hexalith.Builds/Github/*@main` actions, using the `Hexalith.Memories` workflows as the implementation example while preserving EventStore-specific test tiering and release semantics. Keep the change scoped to CI/CD workflow plumbing unless a Builds action contract requires a supporting file.

## Boundaries & Constraints

**Always:** Use `@main` for every Hexalith.Builds action or reusable workflow reference; initialize only root-declared submodules; preserve `UseHexalithProjectReferences=false` for Release/package paths; keep EventStore's existing release-on-main behavior, semantic-release config, and live-sidecar test split unless directly incompatible with the Builds action contract.

**Ask First:** Changing the package publishing implementation away from `.releaserc.json`, adding new release tooling scripts, replacing the workflows with `domain-ci.yml`/`domain-release.yml`, changing test tier membership, or adding/removing published packages.

**Never:** Do not initialize nested submodules recursively, do not pin Hexalith.Builds references to tags or SHAs, do not convert EventStore to Memories-specific release tooling, and do not alter C# source or package metadata for this CI/CD plumbing change.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Release workflow setup | Push to `main` runs `.github/workflows/release.yml` | Checkout, submodule init, .NET setup, Node setup, restore/build/tests, and semantic-release still run; setup/submodule/.NET work comes from Hexalith.Builds actions | If Hexalith.Builds cannot initialize root submodules or .NET, workflow fails at the named shared action step |
| Integration workflow setup | PR, push, or manual run starts `.github/workflows/integration.yml` | Checkout, submodule init, .NET setup, restore, Dapr init, and live-sidecar tests still run; Dapr setup comes from Hexalith.Builds | If Dapr bootstrap fails after retries, the integration job fails before test execution |
| Builds reference audit | Workflows contain Hexalith.Builds references | All Hexalith.Builds references use `Hexalith/Hexalith.Builds/...@main` | Any non-`@main` Builds reference is a spec failure |

</frozen-after-approval>

## Code Map

- `references/Hexalith.EventStore/.github/workflows/release.yml` -- current EventStore release workflow with direct `actions/setup-dotnet`, `actions/cache`, and checkout submodule handling.
- `references/Hexalith.EventStore/.github/workflows/integration.yml` -- current live-sidecar integration workflow with direct Dapr setup and retry logic.
- `references/Hexalith.Memories/.github/workflows/release.yml` -- working example of repository-specific release workflow using `initialize-build` and `initialize-dotnet`.
- `references/Hexalith.Memories/.github/workflows/ci.yml` -- working example of CI jobs using `initialize-build` and `initialize-dotnet`.
- `references/Hexalith.Builds/Github/initialize-build/action.yml` -- composite action that initializes root-declared submodules without recursive init.
- `references/Hexalith.Builds/Github/initialize-dotnet/action.yml` -- composite action that sets up .NET from `global.json`.
- `references/Hexalith.Builds/Github/dapr-init/action.yml` -- composite action that installs Dapr and runs `dapr init` with retry.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.EventStore/.github/workflows/release.yml` -- replace direct .NET setup/submodule initialization with Hexalith.Builds actions while preserving release gates and semantic-release behavior.
- [x] `references/Hexalith.EventStore/.github/workflows/integration.yml` -- replace direct .NET and Dapr setup with Hexalith.Builds actions while preserving live-sidecar filtering.
- [x] `references/Hexalith.EventStore/.github/workflows/*.yml` -- audit action references so Hexalith.Builds references use `@main` and the workflow remains valid YAML.

**Acceptance Criteria:**
- Given EventStore release runs on `main`, when `.github/workflows/release.yml` executes, then root submodules are initialized through `Hexalith/Hexalith.Builds/Github/initialize-build@main` and .NET is initialized through `Hexalith/Hexalith.Builds/Github/initialize-dotnet@main`.
- Given EventStore live-sidecar integration runs, when `.github/workflows/integration.yml` executes, then .NET is initialized through `initialize-dotnet@main` and Dapr is initialized through `Hexalith/Hexalith.Builds/Github/dapr-init@main`.
- Given workflows are inspected, when searching for `Hexalith/Hexalith.Builds/`, then every reference ends with `@main`.
- Given workflow behavior is compared before and after, when reviewing test and release commands, then existing EventStore project list, `Category!=LiveSidecar` release filter, `Category=LiveSidecar` integration filter, and `UseHexalithProjectReferences=false` Release behavior are preserved.

## Spec Change Log

## Verification

**Commands:**
- `python3 - <<'PY'
import pathlib, yaml
for path in pathlib.Path(".github/workflows").glob("*.yml"):
    yaml.safe_load(path.read_text())
PY` from `references/Hexalith.EventStore` -- expected: all modified workflows parse as YAML.
- `rg -n "Hexalith/Hexalith.Builds/.+@" .github/workflows` from `references/Hexalith.EventStore` -- expected: all matches end in `@main`.
- `git diff -- .github/workflows/release.yml .github/workflows/integration.yml` from `references/Hexalith.EventStore` -- expected: diff is limited to CI/CD setup reuse and does not change test/release commands except shared setup action calls.

## Suggested Review Order

**Release Workflow**

- Root submodules are initialized through the shared Builds action.
  [`release.yml:31`](../../references/Hexalith.EventStore/.github/workflows/release.yml#L31)

- .NET setup now reads EventStore's pinned SDK through Builds.
  [`release.yml:34`](../../references/Hexalith.EventStore/.github/workflows/release.yml#L34)

**Integration Workflow**

- Integration setup mirrors release with shared submodule and .NET initialization.
  [`integration.yml:38`](../../references/Hexalith.EventStore/.github/workflows/integration.yml#L38)

- Live-sidecar Dapr bootstrap is delegated to Hexalith.Builds.
  [`integration.yml:57`](../../references/Hexalith.EventStore/.github/workflows/integration.yml#L57)
