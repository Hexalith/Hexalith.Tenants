# Sprint Change Proposal: Use Latest Supported .NET

**Project:** Hexalith.Tenants  
**Date:** 2026-05-18  
**Prepared for:** Jerome  
**Mode:** Batch  
**Change trigger:** Align the project with the latest supported GA .NET release and servicing SDK.  
**Approval:** Approved by Jerome on 2026-05-18.  
**Implementation status:** Applied on 2026-05-18.

## 1. Issue Summary

Hexalith.Tenants already targets `.NET 10` and `net10.0`, which remains the correct current GA/LTS runtime line. However, project artifacts and `global.json` still pin SDK `10.0.103`. Microsoft release metadata identifies `.NET 10.0.8` as the latest .NET 10 runtime and `10.0.300` as the latest .NET 10 SDK as of 2026-05-12. Microsoft Learn lists .NET 10 as the current LTS release, supported until 2028-11-14.

This proposal interprets "latest .NET" as latest supported GA/LTS .NET, not the .NET 11 preview channel. Preview adoption would be a separate strategic change because it would put production and package release work onto prerelease tooling.

Evidence:

- Current repository `global.json` pins SDK `10.0.103` with `rollForward: latestPatch`.
- Installed SDKs include `10.0.300`, so the latest .NET 10 SDK is locally available.
- `Directory.Build.props` already targets `net10.0`; no target framework family change is required.
- `Directory.Packages.props` already pins ASP.NET Core application packages at `10.0.8`, matching the latest .NET 10 runtime servicing level for those packages.

Sources:

- Microsoft Learn, releases and support for .NET: https://learn.microsoft.com/dotnet/core/releases-and-support
- .NET release metadata index: https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/releases-index.json

## 2. Checklist Results

| Item | Status | Notes |
|---|---:|---|
| 1.1 Triggering story | N/A | No implementation story revealed the issue; the trigger is a direct strategic/runtime alignment request. |
| 1.2 Core problem | Done | Existing .NET 10 target is correct, but the SDK pin and planning references are stale at `10.0.103`. |
| 1.3 Supporting evidence | Done | Official release metadata and local SDK inventory confirm `10.0.300` is current and installed. |
| 2.1 Current epic impact | Done | Active Epic 10/11 work can continue; only tooling assumptions need refresh. |
| 2.2 Epic-level changes | Done | No new epic required. Add a small tooling alignment task or patch existing foundation/tooling story notes. |
| 2.3 Remaining epics | Done | Future stories should reference SDK `10.0.300` or "latest .NET 10 servicing SDK" instead of `10.0.103`. |
| 2.4 Obsolete epics | N/A | No planned epic is invalidated. |
| 2.5 Epic ordering | N/A | No resequencing required. |
| 3.1 PRD conflicts | Done | PRD already says `.NET 10+`; no scope conflict. It should clarify current supported GA policy. |
| 3.2 Architecture conflicts | Done | Architecture hard-codes SDK `10.0.103`; update to `10.0.300` plus a servicing policy. |
| 3.3 UX conflicts | N/A | Phase 2 UI design is unaffected. |
| 3.4 Other artifacts | Done | `global.json`, foundation story notes, quickstart/demo docs, and project-context files carry SDK/version references. |
| 4.1 Direct adjustment | Viable | Low effort, low risk: update SDK pin and version references; run restore/build/tests. |
| 4.2 Rollback | Not viable | No rollback simplifies this. |
| 4.3 MVP review | Not viable | MVP scope does not change. |
| 4.4 Recommended path | Done | Direct adjustment. |
| 5.1-5.5 Proposal components | Done | Captured below. |
| 6.1-6.2 Final review | Done | Proposal is internally consistent and implementation-ready. |
| 6.3 User approval | Action-needed | Jerome approval required before applying the proposed edits. |
| 6.4 Sprint status update | N/A | No epic/story additions or removals are proposed. |
| 6.5 Handoff plan | Done | Developer agent can implement directly after approval. |

## 3. Impact Analysis

### Epic Impact

No epic is invalidated. This is a minor tooling and documentation alignment across foundation and active story guidance.

Affected areas:

- Epic 1 foundation/build configuration: SDK pin should move from `10.0.103` to `10.0.300`.
- Active Epic 10 and Epic 11 story files: any generated story context that says "SDK `10.0.103`" should be refreshed to the approved wording.
- Future story creation: use latest .NET 10 servicing SDK wording to avoid repeating an outdated patch.

### Story Impact

No story needs to be added or removed. Recommended implementation can be handled as a small direct patch:

- Update `global.json`.
- Update planning references that hard-code SDK `10.0.103`.
- Update docs that list exact SDK version where needed.
- Run focused restore/build/test validation with SDK `10.0.300`.

### Artifact Conflicts

Artifacts requiring edits:

- `global.json`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/prd.md`
- Relevant implementation artifacts that cite SDK `10.0.103`
- `docs/quickstart.md` and `docs/demo.md` if they state an exact SDK version instead of `.NET 10 SDK`
- Project context files in dependent Hexalith repos only if this workspace is meant to update submodule guidance at the same time

Artifacts not requiring changes:

- `Directory.Build.props`: already targets `net10.0`.
- Project `.csproj` files: they inherit `net10.0`; no per-project TFM change needed.
- Phase 2 UX design: no impact.
- `sprint-status.yaml`: no backlog structure change needed.

### Technical Impact

The current `global.json` uses:

```json
{
  "sdk": {
    "version": "10.0.103",
    "rollForward": "latestPatch"
  }
}
```

With `rollForward: latestPatch`, the SDK can roll within the 10.0.1xx feature band, but it will not intentionally select the latest 10.0.3xx feature band. Updating to `10.0.300` keeps deterministic SDK selection while moving to the latest supported .NET 10 SDK feature band.

## 4. Recommended Approach

Recommended path: **Direct Adjustment**.

Rationale:

- The project is already on the correct runtime family: .NET 10 LTS.
- The mismatch is a stale SDK feature-band pin and repeated stale documentation references.
- The change has low implementation risk because `10.0.300` is installed locally.
- MVP scope, architecture shape, and current epics remain stable.

Risk level: Low  
Effort estimate: Low  
Timeline impact: Same day

## 5. Detailed Change Proposals

### `global.json`

OLD:

```json
{
  "sdk": {
    "version": "10.0.103",
    "rollForward": "latestPatch"
  }
}
```

NEW:

```json
{
  "sdk": {
    "version": "10.0.300",
    "rollForward": "latestPatch"
  }
}
```

Rationale: Pin to the latest .NET 10 SDK reported by official release metadata and already installed locally, while preserving patch-only roll-forward inside the selected feature band.

### PRD Language and Framework Support

Current section:

```markdown
- **Primary language:** C# (.NET 10+, matching EventStore's SDK pinning via `global.json`)
```

Proposed:

```markdown
- **Primary language:** C# on .NET 10 LTS (`net10.0`), using the latest supported .NET 10 servicing SDK pinned via `global.json`.
```

Rationale: Keeps the product on the latest supported GA/LTS line and avoids implying automatic preview adoption.

### Architecture Technology Stack

Current references:

```markdown
- C# on .NET 10 (SDK 10.0.103 via `global.json`, `rollForward: latestPatch`)
4. .NET SDK version -> 10.0.103
```

Proposed:

```markdown
- C# on .NET 10 LTS (SDK 10.0.300 via `global.json`, `rollForward: latestPatch`)
4. .NET SDK version -> latest supported .NET 10 SDK, currently 10.0.300
```

Rationale: Documents both the exact current pin and the upkeep policy.

### Epic 1 Foundation Acceptance Criteria

Current:

```markdown
**Then** it specifies SDK version 10.0.103 with `rollForward: latestPatch`
```

Proposed:

```markdown
**Then** it specifies the latest supported .NET 10 SDK, currently 10.0.300, with `rollForward: latestPatch`
```

Rationale: Prevents future implementation/story generation from reintroducing `10.0.103`.

### Implementation Story Context

Current pattern:

```markdown
- The repository pins .NET SDK `10.0.103` in `global.json` ...
```

Proposed pattern:

```markdown
- The repository pins the latest supported .NET 10 SDK in `global.json`; as of 2026-05-18 this is `10.0.300`.
```

Rationale: Story context should remain true after normal .NET servicing updates.

## 6. Implementation Handoff

Scope classification: **Minor**.

Route to: Developer agent for direct implementation after approval.

Developer tasks:

1. Update `global.json` to SDK `10.0.300`.
2. Update PRD, architecture, epic, and relevant story references from `10.0.103` to the approved "latest supported .NET 10 SDK" wording.
3. Search non-generated project docs for stale `.NET 9`, `.NET 10.0.103`, or `dotnet-version: '9.0.x'` references and update only where they govern this project, not historical research notes.
4. Run `dotnet --version` from the repo root and confirm `10.0.300`.
5. Run `dotnet restore` and the narrowest relevant build/test validation.
6. If package updates are desired beyond SDK/runtime alignment, run a separate package-outdated review instead of bundling it into this correction.

Success criteria:

- Repo root `dotnet --version` reports `10.0.300`.
- All source projects continue targeting `net10.0`.
- Planning artifacts no longer instruct agents to use SDK `10.0.103`.
- No .NET 11 preview dependencies are introduced.
- Validation results are recorded in the implementation artifact or Dev Agent Record.

## 7. Approval Gate

This proposal was approved by Jerome and applied on 2026-05-18.

Implemented outcome:

- `global.json` now pins SDK `10.0.300` with `rollForward: latestPatch`.
- Planning and implementation artifacts now describe the target as the latest supported .NET 10 SDK instead of instructing agents to use SDK `10.0.103`.
- Historical research notes were left unchanged when they described time-bound external research rather than current project guidance.
