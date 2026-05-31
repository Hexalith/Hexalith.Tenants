# Hexalith.Tenants - Claude Code Configuration

## File Discovery and Search

Use `rg` and `rg --files` for repository searches when available. If `rg` is unavailable in a local environment, fall back to `find`, `ls`, or `grep -rn` without changing repository behavior.

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

Only initialize submodules declared at the root of this repository:

- `Hexalith.EventStore`
- `Hexalith.Commons`
- `Hexalith.AI.Tools`
- `Hexalith.Builds`
- `Hexalith.FrontComposer`

Use:
```
git submodule update --init Hexalith.EventStore Hexalith.Commons Hexalith.AI.Tools Hexalith.Builds Hexalith.FrontComposer
```

Never initialize nested submodules. Do not run `git submodule update --init --recursive`, `git submodule foreach --recursive`, or any equivalent command that initializes submodules inside the root-level submodules. If a nested submodule appears uninitialized, leave it alone unless a human explicitly requests otherwise.

## Project Structure

- **Project**: Hexalith.Tenants (.NET/C#)
- **BMAD artifacts**: `_bmad-output/` (untracked, contains planning and implementation artifacts)
  - `_bmad-output/planning-artifacts/` - PRD, architecture, epics, product brief
  - `_bmad-output/implementation-artifacts/` - sprint status, story files
- **BMAD tooling**: `_bmad/` - BMAD framework installation (tracked)
- **Git submodules**: root-level dependencies listed in the Submodule Policy section
