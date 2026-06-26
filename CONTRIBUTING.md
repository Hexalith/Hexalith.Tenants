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

```bash
dotnet test Hexalith.Tenants.slnx
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

## Test Requirements

All pull requests must pass Tier 1 (unit) and Tier 2 (DAPR integration) tests.

- **New domain logic** requires Tier 1 tests with 100% branch coverage on authorization paths
- **Test framework:** xUnit v3 + Shouldly + NSubstitute
- **Coverage:** Collected via coverlet (> 80% line coverage target, 100% branch coverage for the configured isolation/auth targets)

Run the full test suite:

```bash
dotnet test Hexalith.Tenants.slnx
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
