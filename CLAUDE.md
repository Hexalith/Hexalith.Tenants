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

## Project Structure

- **Project**: Hexalith.Tenants (.NET/C#)
- **BMAD artifacts**: `_bmad-output/` (untracked, contains planning and implementation artifacts)
  - `_bmad-output/planning-artifacts/` - PRD, architecture, epics, product brief
  - `_bmad-output/implementation-artifacts/` - sprint status, story files
- **BMAD tooling**: `_bmad/` - BMAD framework installation (tracked)
- **Git submodule**: `Hexalith.EventStore` - EventStore dependency
