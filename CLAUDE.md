# Hexalith.Tenants - Claude Code Configuration

## Critical: Glob and Grep Tools Broken — ripgrep Not Installed

The `Glob` and `Grep` tools silently fail (return "No files found" for all queries) because **ripgrep (`rg`) is not installed** on this Windows system. Both tools depend on ripgrep internally.

**To fix permanently**: Install ripgrep and restart Claude Code:
```
winget install BurntSushi.ripgrep
```

**Until fixed — workarounds (MUST follow):**

- **Instead of Glob**: Use `Bash` with `find` or `ls` for file discovery
  - Example: `find . -name "*.md" -type f` instead of `Glob("**/*.md")`
- **Instead of Grep**: Use `Bash` with `grep -rn` for content search
  - Example: `grep -rn "pattern" src/` instead of `Grep("pattern")`
- **Read tool**: Works normally, continue using it

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

- **CI:** GitHub Actions on push/PR to main — restore, build (Release), Tier 1+2 tests
- **Release:** Triggered on merge to main via semantic-release — determines version from Conventional Commits, tests, pack, publish 5 NuGet packages, creates GitHub Release, updates CHANGELOG.md

## Project Structure

- **Project**: Hexalith.Tenants (.NET/C#)
- **BMAD artifacts**: `_bmad-output/` (untracked, contains planning and implementation artifacts)
  - `_bmad-output/planning-artifacts/` - PRD, architecture, epics, product brief
  - `_bmad-output/implementation-artifacts/` - sprint status, story files
- **BMAD tooling**: `_bmad/` - BMAD framework installation (tracked)
- **Git submodule**: `Hexalith.EventStore` - EventStore dependency
