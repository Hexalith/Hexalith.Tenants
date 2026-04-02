# Sprint Change Proposal — Replace MinVer with Semantic Release

**Date:** 2026-04-02
**Triggered by:** Ecosystem alignment — Hexalith.EventStore uses semantic-release; Hexalith.Tenants should match
**Scope classification:** Minor — direct implementation by development team
**Approved edit proposals:** 7/7

---

## Section 1: Issue Summary

Hexalith.Tenants uses MinVer 7.0.0 for git tag-based SemVer versioning with a manual tag-push release workflow, while Hexalith.EventStore uses semantic-release with Conventional Commits and automated releases on merge to main. This divergence creates:

- Two different release workflows in the same ecosystem
- No auto-generated CHANGELOG in Tenants
- Manual tag management that is error-prone
- Unused Conventional Commit discipline (EventStore's CLAUDE.md already mandates it, but Tenants doesn't leverage it for versioning)

This is not a failure — Story 1.3 (CI/CD Pipeline) works correctly. This is a strategic alignment to make both repositories consistent.

---

## Section 2: Impact Analysis

### Epic Impact
- **Epic 1 (Project Foundation):** Only epic affected. Story 1.3 (CI/CD Pipeline) acceptance criteria need updating. Epic remains **done**.
- **Epics 2-8:** No impact. All complete, no dependencies on versioning tooling.

### Story Impact
- **Story 1.3:** Acceptance criteria updated to reflect merge-triggered semantic-release instead of tag-triggered MinVer.
- **No other stories affected.**

### Artifact Conflicts

| Artifact | Change Type | Details |
|----------|------------|---------|
| `Directory.Build.props` | Modify | Remove MinVer PropertyGroup (lines 25-28) and PackageReference (lines 31-32) |
| `Directory.Packages.props` | Modify | Remove MinVer PackageVersion entry (line 7) |
| `.github/workflows/release.yml` | Rewrite | Tag-triggered → merge-triggered with semantic-release |
| `.releaserc.json` | Create | semantic-release config modeled after EventStore |
| `package.json` | Create | npm devDependencies for semantic-release plugins |
| `_bmad-output/planning-artifacts/epics.md` | Modify | Story 1.3 acceptance criteria |
| `_bmad-output/planning-artifacts/prd.md` | Modify | Package quality standards (line 409) |
| `_bmad-output/planning-artifacts/architecture.md` | Modify | Build & Versioning (line 222), CI/CD decision (line 389), directory structure (line 665) |

### Technical Impact
- **Domain code:** Zero changes
- **Test code:** Zero changes
- **Package APIs:** Zero changes
- **CI workflow (`ci.yml`):** No changes needed — no MinVer-specific logic

---

## Section 3: Recommended Approach

**Selected:** Option 1 — Direct Adjustment

**Rationale:**
- EventStore's `.releaserc.json` and `package.json` are proven templates — copy and adapt for 5 packages
- All changes are confined to build configuration, CI workflows, and documentation
- Zero risk to domain logic, tests, or package consumers
- Effort: Low (< 1 hour implementation)
- Risk: Low (battle-tested toolchain, ecosystem precedent)

**Trade-offs considered:**
- Option 2 (Rollback): Not applicable — nothing broken to revert
- Option 3 (MVP Review): Not applicable — MVP is complete; this is post-completion tooling alignment

---

## Section 4: Detailed Change Proposals

### 4.1 — Directory.Build.props: Remove MinVer

Remove lines 25-32 (MinVer PropertyGroup and PackageReference). Version will be set externally by semantic-release via `-p:Version=${nextRelease.version}`.

### 4.2 — Directory.Packages.props: Remove MinVer package version

Remove line 7: `<PackageVersion Include="MinVer" Version="7.0.0" />`.

### 4.3 — New file: `.releaserc.json`

```json
{
  "branches": ["main"],
  "tagFormat": "v${version}",
  "plugins": [
    "@semantic-release/commit-analyzer",
    "@semantic-release/release-notes-generator",
    "@semantic-release/changelog",
    [
      "@semantic-release/exec",
      {
        "prepareCmd": "dotnet build --configuration Release -p:Version=${nextRelease.version} && dotnet pack --no-build --configuration Release --output ./nupkgs -p:Version=${nextRelease.version}",
        "publishCmd": "dotnet nuget push ./nupkgs/*.nupkg --source https://api.nuget.org/v3/index.json --api-key $NUGET_API_KEY --skip-duplicate --verbosity quiet"
      }
    ],
    [
      "@semantic-release/github",
      {
        "assets": ["nupkgs/*.nupkg"]
      }
    ],
    [
      "@semantic-release/git",
      {
        "assets": ["CHANGELOG.md"],
        "message": "chore(release): ${nextRelease.version} [skip ci]"
      }
    ]
  ]
}
```

### 4.4 — New file: `package.json`

```json
{
  "name": "hexalith-tenants",
  "version": "0.0.0",
  "private": true,
  "description": "semantic-release configuration for Hexalith.Tenants",
  "devDependencies": {
    "semantic-release": "^24.2.3",
    "@semantic-release/changelog": "^6.0.3",
    "@semantic-release/exec": "^7.0.3",
    "@semantic-release/git": "^10.0.1",
    "@semantic-release/github": "^11.0.1",
    "@semantic-release/commit-analyzer": "^13.0.1",
    "@semantic-release/release-notes-generator": "^14.0.3",
    "@commitlint/cli": "^19.8.0",
    "@commitlint/config-conventional": "^19.8.0"
  }
}
```

### 4.5 — Rewrite `.github/workflows/release.yml`

Full rewrite from tag-triggered MinVer workflow to merge-triggered semantic-release workflow. Key changes:
- Trigger: `push branches: [main]` instead of `push tags: ['v*']`
- Added Node.js setup and `npm ci`
- Tests run as quality gate before `npx semantic-release`
- Removed Python package validator (semantic-release handles version consistency)
- Removed manual tag/version matching logic
- `persist-credentials: false` for secure token handling

### 4.6 — Epics: Story 1.3 acceptance criteria

Updated to reflect merge-triggered semantic-release, version derived from Conventional Commits, CHANGELOG generation, and no-op behavior on non-releasable commits.

### 4.7 — Documentation artifact updates

- **PRD line 409:** "MinVer (git tag-based SemVer, prefix `v`)" → "semantic-release (Conventional Commits, automated SemVer on merge to main)"
- **Architecture line 222:** "MinVer for git tag-based SemVer (prefix `v`)" → "semantic-release for automated SemVer from Conventional Commits (on merge to main, tag prefix `v`)"
- **Architecture line 665:** "Tag-triggered: tests + pack + NuGet push" → "Merge-triggered: semantic-release + tests + pack + NuGet push"

---

## Section 5: Implementation Handoff

**Scope:** Minor — direct implementation by development team

**Implementation sequence:**
1. Create `.releaserc.json` and `package.json`
2. Modify `Directory.Build.props` — remove MinVer
3. Modify `Directory.Packages.props` — remove MinVer entry
4. Rewrite `.github/workflows/release.yml`
5. Update planning artifacts (epics, PRD, architecture)
6. Verify `dotnet build` succeeds without MinVer
7. Commit with `feat(ci): replace MinVer with semantic-release`

**Success criteria:**
- `dotnet build --configuration Release -p:Version=0.0.1-test` succeeds (no MinVer conflict)
- `npx semantic-release --dry-run` reports correct version determination
- All existing tests pass
- Planning artifacts consistently reference semantic-release

**Handoff:** Development team (solo developer Jerome) — direct implementation, no backlog reorganization needed.
