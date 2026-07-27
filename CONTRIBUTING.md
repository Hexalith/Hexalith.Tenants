# Contributing to Hexalith.Tenants

Thank you for your interest in contributing to Hexalith.Tenants! This guide covers everything you need to get started.

## Getting Started

### Prerequisites

- **.NET 10 SDK** — [Download](https://dot.net/download). Verify: `dotnet --version` → `10.x.xxx`
- **DAPR CLI + Runtime** — [Getting Started](https://docs.dapr.io/getting-started/). Run `dapr init` (**full init, NOT `--slim`** — the Aspire topology requires the full DAPR runtime with placement service for actors). Verify: `dapr --version`
- **Docker** — [Download](https://docs.docker.com/get-started/get-docker/). Docker Desktop must be running. Allocate at least 4 GB of memory

### Clone the Repository

Clone the repository, then initialize root-declared submodules under `references/` only:

```bash
git clone https://github.com/Hexalith/Hexalith.Tenants.git
cd Hexalith.Tenants
git submodule update --init references/Hexalith.EventStore references/Hexalith.Commons references/Hexalith.AI.Tools references/Hexalith.Builds references/Hexalith.FrontComposer references/Hexalith.PolymorphicSerializations references/Hexalith.Memories
```

> **Windows users:** If the build fails with path-too-long errors, run `git config --system core.longpaths true` and re-clone.

### Build

```bash
dotnet build Hexalith.Tenants.slnx --configuration Release
```

### Run Tests

Run test projects individually; use the `.slnx` for restore/build, not solution-level `dotnet test`.

```bash
dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release
```

### Run Locally

Start the Aspire AppHost, which launches the full topology (CommandApi + Sample service + DAPR sidecars + Redis):

```bash
dotnet run --project src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
```

## Branch Naming Conventions

- `feat/<description>` — New features
- `fix/<description>` — Bug fixes
- `docs/<description>` — Documentation changes
- `refactor/<description>` — Code refactoring
- `test/<description>` — Test additions or changes

## Commit Message Conventions

All commits **must** follow the [Conventional Commits](https://www.conventionalcommits.org/) specification. This is required — semantic-release uses commit messages to determine version bumps and generate changelogs automatically.

Format: `<type>(<optional scope>): <description>`

| Type | Purpose | Version Bump |
|------|---------|-------------|
| `feat:` | New feature | Minor |
| `fix:` | Bug fix | Patch |
| `docs:` | Documentation only | None |
| `refactor:` | Code change (no feature/fix) | None |
| `test:` | Adding or updating tests | None |
| `chore:` | Build process, CI, tooling | None |
| `perf:` | Performance improvement | Patch |

For breaking changes, add `BREAKING CHANGE:` in the commit body or append `!` after the type (e.g., `feat!:`). This triggers a **major** version bump.

Examples:

```
feat(contracts): add TenantConfigurationSet command
fix(server): prevent duplicate user addition to tenant
docs: update quickstart with DAPR init prerequisites
chore(ci): replace MinVer with semantic-release
feat!: rename TenantAggregate state shape
```

## Pull Request Process

1. Create a branch from `main` using the naming conventions above
2. Make changes and commit using Conventional Commits format
3. Ensure all Tier 1 and Tier 2 tests pass locally before submitting
4. Open a PR against `main` with a description of changes
5. CI will run automatically, including package metadata and package-only consumer validation — PR must pass before merge
6. PRs require at least one approval
7. On merge to `main`, semantic-release automatically determines the version, publishes NuGet packages, and creates a GitHub Release

## Release Version Line

Releases run on the **4.x** line. Nothing at or below `4.0.0` can be released again, because the
five packages share one version and part of the range is already published and immutable:

- `Hexalith.Tenants.Contracts`, `.Server` and `.Aspire` occupy `3.3.0` through `3.15.1`, published
  from this repository in May 2026 (`3.15.1` was built from `5469a6b`).
- Tags `v3.2.0` through `v3.15.1` were deleted afterwards, so semantic-release resumed from `v3.1.1`
  and released back inside that range. `3.2.0` and `3.2.1` were tagged but never landed on NuGet —
  the push still carried `--skip-duplicate` then, so the collision passed silently. `3.2.2` through
  `3.2.18` occupied numbers the May line had left free.
- The next minor bump therefore proposed `3.3.0`, which the publication preflight rejected as a
  collision in run `30291329462`.

`scripts/validate-publication-preflight.sh` declares `minimum_release_version` and fails closed —
locally, before the shared preflight probes any destination — when a proposal falls below it. The
check firing means one of two things: the tag line lost its history again, so restore the deleted
tags until semantic-release resumes above the floor; or the analyzed commits do not justify the
major bump, so the change needs a `BREAKING CHANGE` footer. Never lower the floor to make a release
pass, and never publish into the consumed range.

**Consumers.** Sibling Hexalith repositories pin `Hexalith.Tenants.*` at `3.2.x`. Moving to `4.0.0`
requires a coordinated package-reference bump. A consumer coming from `3.2.18` gets no API break —
the breaking commits it names are already in `3.2.x`. A consumer still on the May `3.15.1` line does:
`09699ca` (container release moved to zot) and `f46264a` (fail-safe role/status enum defaults and
consumer-contract hardening).

## Test Requirements

All pull requests must pass Tier 1 (unit) and Tier 2 (DAPR integration) tests.

- **New domain logic** requires Tier 1 tests with 100% branch coverage on authorization paths
- **Test framework:** xUnit v3 + Shouldly + NSubstitute
- **Coverage:** Collected via coverlet (> 80% line coverage target, 100% branch coverage for the configured isolation/auth targets)

Run the test suite by project:

```bash
dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release
dotnet test samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj --configuration Release
```

## Code Style

Code style is enforced via [`.editorconfig`](.editorconfig) (inherited from EventStore conventions).

**Key conventions:**

- File-scoped namespaces (`namespace X.Y.Z;`)
- K&R braces (opening brace on the same line)
- `_camelCase` private fields
- 4-space indentation
- Warnings as errors

Run `dotnet format` before committing to auto-fix formatting issues:

```bash
dotnet format Hexalith.Tenants.slnx
```

## Submodule Management

This repository uses root-declared git submodules under `references/`: `references/Hexalith.EventStore`, `references/Hexalith.Commons`, `references/Hexalith.AI.Tools`, `references/Hexalith.Builds`, `references/Hexalith.FrontComposer`, `references/Hexalith.PolymorphicSerializations`, and `references/Hexalith.Memories`.

- **Initial clone:** Run `git submodule update --init references/Hexalith.EventStore references/Hexalith.Commons references/Hexalith.AI.Tools references/Hexalith.Builds references/Hexalith.FrontComposer references/Hexalith.PolymorphicSerializations references/Hexalith.Memories` after cloning
- **After pulling main:** Run `git submodule update --init references/Hexalith.EventStore references/Hexalith.Commons references/Hexalith.AI.Tools references/Hexalith.Builds references/Hexalith.FrontComposer references/Hexalith.PolymorphicSerializations references/Hexalith.Memories` to sync root-declared submodules
- **When a root-declared submodule reference changes in a PR:** Run `git submodule update references/Hexalith.EventStore references/Hexalith.Commons references/Hexalith.AI.Tools references/Hexalith.Builds references/Hexalith.FrontComposer references/Hexalith.PolymorphicSerializations references/Hexalith.Memories` to update your local copy
- **Nested submodules:** Do not use recursive submodule initialization unless a maintainer explicitly asks for nested submodules

> **Important:** Do NOT modify files inside `references/Hexalith.EventStore/` directly. Changes to the submodule must go through the [EventStore repository](https://github.com/Hexalith/Hexalith.EventStore).

## Project Structure

See the [README](README.md#project-structure) for a complete overview of the project layout.

Key directories:

| Directory | Purpose |
|-----------|---------|
| `src/` | Production source code — contracts, client, server, REST API host, Aspire hosting |
| `tests/` | Unit and integration tests |
| `samples/` | Example consuming service with event subscription |
| `docs/` | Guides and reference documentation |

## Reporting Issues

Found a bug or have a feature request? Open an issue on [GitHub Issues](https://github.com/Hexalith/Hexalith.Tenants/issues).

Please include:

- A clear description of the issue or feature
- Steps to reproduce (for bugs)
- Expected vs actual behavior
- .NET SDK version and OS
