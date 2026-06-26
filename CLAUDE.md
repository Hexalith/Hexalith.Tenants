# Hexalith.Tenants - Claude Code Configuration

## AI assistant instructions

Before working in this repository, read
[`hexalith-llm-instructions.md`](./references/Hexalith.AI.Tools/hexalith-llm-instructions.md)
(in the `Hexalith.AI.Tools` submodule) and follow it.

Before working on any module user interface or UX, also read
[`references/Hexalith.AI.Tools/hexalith-ux-instructions.md`](./references/Hexalith.AI.Tools/hexalith-ux-instructions.md)
and follow it.

## File Discovery and Search

Use `rg` and `rg --files` for repository searches when available. If `rg` is unavailable in a local environment, fall back to `find`, `ls`, or `grep -rn` without changing repository behavior.

## Domain Implementation Boundary

Hexalith.Tenants is a domain implementation for the Tenants domain. Keep this repository focused on tenant-specific contracts, behaviors, rules, events, projections, and user-facing domain flows.

Do not add boilerplate code that is common to domain modules here. Reuse existing shared implementations from the technical modules, or move the boilerplate into the appropriate technical module before consuming it from Tenants. Typical homes for shared infrastructure and scaffolding include `Hexalith.EventStore`, `Hexalith.FrontComposer`, `Hexalith.Commons`, `Hexalith.Builds`, and other cross-domain Hexalith modules.

Before adding generic hosting, event-store plumbing, serialization, dependency injection setup, UI composition scaffolding, test harness helpers, or cross-domain conventions to this repository, first check whether a technical module already provides the capability. If the shared capability is missing, implement it in the relevant technical module instead of duplicating it in Hexalith.Tenants.

## Repository responsibility

This repository should contain primarily domain code for managing work items. Do not add technical
layers here unless they are absolutely required for work items and are not common to other domain
modules.

Factor technical concerns into the relevant shared Hexalith modules. For example, persistence belongs
in `Hexalith.EventStore`, and unique identifier generation belongs in `Hexalith.Commons`.

The .NET Aspire Host is an acceptable technical component in this repository because each Hexalith
module needs a repository-specific host with servers and dependencies tailored to that module. Aspire
is required to run both manual and automated tests.

## Commit Messages

All commit messages **must** follow the [Conventional Commits](https://www.conventionalcommits.org/) specification. This is required for semantic-release to determine version bumps and generate changelogs.

Format: `<type>(<optional scope>): <description>`

- `feat:` — New feature (triggers **minor** version bump)
- `fix:` — Bug fix (triggers **patch** version bump)
- `docs:` — Documentation only
- `refactor:` — Code change that neither fixes a bug nor adds a feature
- `test:` — Adding or updating tests
- `chore:` — Build process, CI, or tooling changes
- `perf:` — Performance improvement

For breaking changes, add `BREAKING CHANGE:` in the commit body or append `!` after the type (e.g., `feat!:`). This triggers a **major** version bump.

Examples:
```
feat(contracts): add TenantConfigurationSet command
fix(server): prevent duplicate user addition to tenant
docs: update quickstart with DAPR init prerequisites
chore(ci): replace MinVer with semantic-release
feat!: rename TenantAggregate state shape
```

## CI/CD

- **CI:** GitHub Actions on push/PR to main — restore, build (Release), Tier 1+2 tests, package metadata validation, and package-only consumer validation
- **Release:** Triggered on merge to main via semantic-release — determines version from Conventional Commits, tests, packs and validates 5 NuGet packages, validates package-only consumers, publishes to NuGet, creates GitHub Release, updates CHANGELOG.md

## Submodule Policy

Only initialize submodules declared in the root `.gitmodules` file under `references/`.

Never initialize nested submodules. Do not run `git submodule update --init --recursive`, `git submodule foreach --recursive`, or any equivalent command that initializes submodules inside the root-declared submodules under `references/`. If a nested submodule appears uninitialized, leave it alone unless a human explicitly requests otherwise.

## Project Structure

- **Project**: Hexalith.Tenants (.NET/C#)
- **BMAD artifacts**: `_bmad-output/` (untracked, contains planning and implementation artifacts)
  - `_bmad-output/planning-artifacts/` - PRD, architecture, epics, product brief
  - `_bmad-output/implementation-artifacts/` - sprint status, story files
- **BMAD tooling**: `_bmad/` - BMAD framework installation (tracked)
- **Git submodules**: root-declared dependencies under `references/` listed in the Submodule Policy section
