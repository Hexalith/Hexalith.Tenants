# Contributing to Hexalith.Tenants

Thank you for your interest in contributing to Hexalith.Tenants! This guide covers everything you need to get started.

## Getting Started

### Prerequisites

- **.NET 10 SDK** — [Download](https://dot.net/download). Verify: `dotnet --version` → `10.x.xxx`
- **DAPR CLI + Runtime** — [Getting Started](https://docs.dapr.io/getting-started/). Run `dapr init` (**full init, NOT `--slim`** — the Aspire topology requires the full DAPR runtime with placement service for actors). Verify: `dapr --version`
- **Docker** — [Download](https://docs.docker.com/get-started/get-docker/). Docker Desktop must be running. Allocate at least 4 GB of memory

### Clone the Repository

Clone with submodules — the `Hexalith.EventStore` submodule is required:

```bash
git clone --recurse-submodules https://github.com/Hexalith/Hexalith.Tenants.git
cd Hexalith.Tenants
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
5. CI will run automatically — PR must pass before merge
6. PRs require at least one approval
7. On merge to `main`, semantic-release automatically determines the version, publishes NuGet packages, and creates a GitHub Release

## Test Requirements

All pull requests must pass Tier 1 (unit) and Tier 2 (DAPR integration) tests.

- **New domain logic** requires Tier 1 tests with 100% branch coverage on authorization paths
- **Test framework:** xUnit + Shouldly + NSubstitute
- **Coverage:** Collected via coverlet (> 80% line coverage target)

Run the full test suite:

```bash
dotnet test Hexalith.Tenants.slnx
```

## Code Style

Code style is enforced via [`.editorconfig`](.editorconfig) (inherited from EventStore conventions).

**Key conventions:**

- File-scoped namespaces (`namespace X.Y.Z;`)
- Allman braces (new line before opening brace)
- `_camelCase` private fields
- 4-space indentation
- Warnings as errors

Run `dotnet format` before committing to auto-fix formatting issues:

```bash
dotnet format Hexalith.Tenants.slnx
```

## Submodule Management

This repository uses `Hexalith.EventStore` as a git submodule.

- **Initial clone:** Use `git clone --recurse-submodules` (covered in Getting Started)
- **After pulling main:** Run `git submodule update --init --recursive` to sync the submodule
- **When the submodule reference changes in a PR:** Run `git submodule update` to update your local copy

> **Important:** Do NOT modify files inside `Hexalith.EventStore/` directly. Changes to the submodule must go through the [EventStore repository](https://github.com/Hexalith/Hexalith.EventStore).

## Project Structure

See the [README](README.md#project-structure) for a complete overview of the project layout.

Key directories:

| Directory | Purpose |
|-----------|---------|
| `src/` | Production source code — contracts, client, server, CommandApi, Aspire hosting |
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
