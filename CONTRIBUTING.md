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
git submodule update --init references/Hexalith.EventStore references/Hexalith.Commons references/Hexalith.AI.Tools references/Hexalith.Builds references/Hexalith.FrontComposer references/Hexalith.PolymorphicSerializations references/Hexalith.Memories references/Hexalith.McpCli references/Hexalith.Platform
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
dotnet test tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Tenants.AppHost.Tests/Hexalith.Tenants.AppHost.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release
dotnet test samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj --configuration Release
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

All commits **must** follow the [Conventional Commits](https://www.conventionalcommits.org/) specification. This is required — semantic-release uses commit messages to determine version bumps and generate release notes.

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
7. Merging to `main` runs CI. Publication requires an operator to manually dispatch the [Release workflow](.github/workflows/release.yml) from the current `main` tip after a successful push CI run for that exact source SHA. The workflow checks release lineage before entering the protected `production` environment, rechecks the source before publication, and verifies that the published release tag resolves to the dispatched source. The release path does not rerun the test tiers.

If the source becomes stale during setup, dispatch again from the new `main` tip after its exact-source
push CI succeeds. If there are no releasable commits, the final publication check fails and
redispatching the unchanged source does not help; inspect the release output and releasable changes
before attempting another release.

## Release Version Line

Versioning follows release tags reachable from `main` and the committed
[semantic-release configuration](.releaserc.json). At local inspection on **2026-10-18**, recorded
reachable history extends through **5.x**, with highest locally available merged tag `v5.8.0`
at commit [`032573384d3df4bc7a5bc0e69945ecca4e00967f`](https://github.com/Hexalith/Hexalith.Tenants/commit/032573384d3df4bc7a5bc0e69945ecca4e00967f)
(2026-10-08). This repository snapshot does not prove current registry contents, the next release
version, or release readiness. Check the dispatch-time lineage and publication preflights for the
intended source; never reuse a published package version.

The configured `minimum_release_version=4.0.0` is a **static safety minimum**, not the current
release version. Proposals below `4.0.0` fail locally; meeting that minimum alone does not make a
version publishable. The five packages share one version, so an occupied version for any one
package prevents publication of the complete set.

**Historical 3.x/4.0 recovery context (July 2026):**

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
check firing requires release-lineage investigation; it does not establish the cause by itself.
Restore a missing release tag only when it is the authentic tag for that published version,
points to the commit that actually produced it, and is reachable from the dispatched source.
If that provenance or reachability cannot be established, stop and use a
separately reviewed release-lineage recovery procedure; never create or move a tag merely to clear this guard.
A version-line bump does not repair unproven lineage, and the earlier registry-floor guard blocks
before semantic-release computes a proposal. Published versions above the highest reachable tag
establish a lineage gap and collision risk, not a guaranteed collision with an uncomputed proposal.
Never lower the floor to make a release pass, and never publish into the consumed range.

For recovery review, collect the original successful publication run and its exact source commit,
tie the published version to that run's artifacts, and establish that the producing commit is an
ancestor of the dispatched source. A tag name alone does not establish that provenance. Send the
evidence to repository maintainers through a
[release-lineage recovery issue](https://github.com/Hexalith/Hexalith.Tenants/issues/new).
Keep publication blocked while that evidence is incomplete. A published version with a nonzero
fourth NuGet revision cannot be represented by the repository's three-part release tags and also
requires a separately reviewed recovery procedure.

**Historical consumer migration to 4.0.0.** At the time of that recovery, sibling Hexalith
repositories pinned `Hexalith.Tenants.*` at `3.2.x`; migration required a coordinated
package-reference bump. The recorded comparison from `3.2.18` identified no additional API break
because the breaking commits were already in `3.2.x`. The comparison from the May `3.15.1` line
identified `09699ca` (container release moved to zot) and `f46264a` (fail-safe role/status enum
defaults and consumer-contract hardening). These are historical observations, not an inventory
of current consumer pins or a compatibility guarantee for upgrading to later releases.

Read [GitHub Releases](https://github.com/Hexalith/Hexalith.Tenants/releases) for generated release
notes and assess compatibility from the consumer's actual pinned version. The current
semantic-release configuration does not update the repository's `CHANGELOG.md`; its existing
entries remain historical.

## Release Publication Credentials

NuGet packages are published to nuget.org through
[Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing). No NuGet API
key is stored anywhere: the protected publishing job exchanges its GitHub OIDC token for a one-hour
key with `NuGet/login` immediately before it pushes. nuget.org matches a policy against the workflow
file that defines the job, so the publishing job lives in this repository instead of the shared
Hexalith.Builds `domain-release.yml`.

The `Hexalith` package owner on nuget.org needs one active policy per publishing workflow, both with
repository owner `Hexalith`, repository `Hexalith.Tenants` and environment `production`:

| Workflow file | Purpose |
| --- | --- |
| `release.yml` | Semantic Release publication |
| `recover-partial-release.yml` | Completing a partially published version |

Set the repository variable `NUGET_USER` to the individual nuget.org username that created those
policies, not the `Hexalith` organization name. Both workflows fail before building when it is empty.
Containers still publish to `registry.hexalith.com` with the `HEXALITH_ZOT_*` secrets.

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
dotnet test tests/Hexalith.Tenants.AppHost.Tests/Hexalith.Tenants.AppHost.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release
dotnet test samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj --configuration Release
```

## Code Style

Code style is enforced via [`.editorconfig`](.editorconfig) (inherited from EventStore conventions).

**Key conventions:**

- File-scoped namespaces (`namespace X.Y.Z;`)
- Allman braces (opening brace on a new line)
- `_camelCase` private fields
- 4-space indentation
- Warnings as errors

Run `dotnet format` before committing to auto-fix formatting issues:

```bash
dotnet format Hexalith.Tenants.slnx
```

## Submodule Management

This repository uses root-declared git submodules under `references/`: `references/Hexalith.EventStore`, `references/Hexalith.Commons`, `references/Hexalith.AI.Tools`, `references/Hexalith.Builds`, `references/Hexalith.FrontComposer`, `references/Hexalith.PolymorphicSerializations`, `references/Hexalith.Memories`, `references/Hexalith.McpCli`, and `references/Hexalith.Platform`.

- **Initial clone:** Run `git submodule update --init references/Hexalith.EventStore references/Hexalith.Commons references/Hexalith.AI.Tools references/Hexalith.Builds references/Hexalith.FrontComposer references/Hexalith.PolymorphicSerializations references/Hexalith.Memories references/Hexalith.McpCli references/Hexalith.Platform` after cloning
- **After pulling main:** Run `git submodule update --init references/Hexalith.EventStore references/Hexalith.Commons references/Hexalith.AI.Tools references/Hexalith.Builds references/Hexalith.FrontComposer references/Hexalith.PolymorphicSerializations references/Hexalith.Memories references/Hexalith.McpCli references/Hexalith.Platform` to sync root-declared submodules
- **When a root-declared submodule reference changes in a PR:** Run `git submodule update references/Hexalith.EventStore references/Hexalith.Commons references/Hexalith.AI.Tools references/Hexalith.Builds references/Hexalith.FrontComposer references/Hexalith.PolymorphicSerializations references/Hexalith.Memories references/Hexalith.McpCli references/Hexalith.Platform` to update your local copy
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
