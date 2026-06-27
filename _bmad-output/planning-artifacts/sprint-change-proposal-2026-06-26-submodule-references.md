# Sprint Change Proposal: Move Root-Declared Submodules Under `references/`

Date: 2026-06-26
Requested by: Administrator
Status: Implemented

## 1. Issue Summary

The repository's root-declared Hexalith dependency submodules lived at the repository root:

- `Hexalith.EventStore`
- `Hexalith.Commons`
- `Hexalith.AI.Tools`
- `Hexalith.FrontComposer`
- `Hexalith.Builds`
- `Hexalith.PolymorphicSerializations`
- `Hexalith.Memories`

That layout collided with the preferred repository boundary for Tenants and made root-level project discovery harder to distinguish from Tenants source, tests, docs, and build assets. Administrator requested moving the submodules into `/references` and updating all solution, project, documentation, and LLM instruction references.

## 2. Epic and Artifact Impact

No product capability, PRD requirement, UX behavior, domain contract, command, event, projection, or tenant-facing workflow changes are required.

Impacted artifact types:

- `.gitmodules` submodule paths
- `Hexalith.Tenants.slnx` solution references and solution folders
- `Directory.Build.props` source-project root detection
- Project references that depend on `HexalithEventStoreRoot`, `HexalithFrontComposerRoot`, or `HexalithMemoriesRoot`
- Setup documentation and quickstart commands
- Documentation tests and solution-structure tests that assert submodule paths
- BMAD planning and implementation artifacts that cite dependency source files
- Repository LLM instructions in `AGENTS.md`, `CLAUDE.md`, and `_bmad-output/project-context.md`

Non-impacted artifact types:

- Tenants domain contracts and aggregate behavior
- Runtime command/query semantics
- UI screens and UX specifications
- Package identity and repository URLs
- Nested submodule policy

## 3. Recommended Path

Recommended path: direct adjustment.

Rationale: this is a repository-layout correction with broad path references but no functional scope change. Updating the affected path surfaces in one coordinated change avoids a partially migrated state where build tooling, documentation, and AI instructions disagree.

The user's explicit request is treated as approval to implement the direct adjustment.

## 4. Change Proposal

Move each root-declared submodule under `references/`:

- `Hexalith.EventStore` -> `references/Hexalith.EventStore`
- `Hexalith.Commons` -> `references/Hexalith.Commons`
- `Hexalith.AI.Tools` -> `references/Hexalith.AI.Tools`
- `Hexalith.FrontComposer` -> `references/Hexalith.FrontComposer`
- `Hexalith.Builds` -> `references/Hexalith.Builds`
- `Hexalith.PolymorphicSerializations` -> `references/Hexalith.PolymorphicSerializations`
- `Hexalith.Memories` -> `references/Hexalith.Memories`

Update `.gitmodules` so each root-declared submodule path uses `references/<submodule>`. Keep GitHub repository URLs unchanged.

Update the solution file so dependency projects are grouped under `/References/` and point to `references/...`.

Update `Directory.Build.props` so source-project discovery supports:

- Tenants nested inside a dependency repository
- standalone Tenants with dependencies under `references/`
- sibling checkouts
- Tenants nested under another repository's `references/` folder

Update setup documentation and tests so submodule initialization/status commands target the root-declared `references/...` paths explicitly and still avoid recursive nested-submodule initialization.

Update LLM instructions and project context so agents read `references/Hexalith.AI.Tools/...` and describe the submodule policy as root-declared dependencies under `references/`.

## 5. Validation Plan

- `git submodule status` shows every root-declared dependency under `references/`.
- Stale-path scans find no old direct `Hexalith.*` slash or backslash paths outside `references/`.
- `dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror` succeeds or any unrelated pre-existing submodule revision issue is reported.
- Focused tests covering solution structure, package governance, and documentation path assertions pass or any unrelated pre-existing issue is reported.

## 6. Correct-Course Checklist

- [x] Trigger and issue identified.
- [x] Product scope impact assessed.
- [x] Architecture and build impact assessed.
- [x] Documentation and AI instruction impact assessed.
- [x] Recommended path selected.
- [x] Implementation handoff defined.
- [x] Change implemented against the approved direct-adjustment path.
