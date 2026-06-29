# Sprint Change Proposal: Historical UI Evidence Documentation Drift

Date: 2026-06-29
Project: tenants
Trigger: Epic 5 retrospective action item `epic-5-retro-2026-06-29-doc-drift`

## 1. Issue Summary

Historical audit/recovery and UI evidence specifications still contained stale readiness language after Epic 1 through Epic 5 delivered Tenants-owned UI flows. The stale wording could make delivered tenant list/detail, member, configuration, global-administrator, flat audit, receipt, and tenant-domain correction slices look blocked or planning-only.

The audit/recovery spec also said global-administrator correction was fail-closed until Epic 4 command support existed. Epic 4 now supplies grant/remove command support; the current gate is Story 5.7 fixed-scope correction verification. Several support-safety lines also used broad "ID" wording where the intended unsafe values are internal command message/correlation identifiers, not support-safe caller-supplied tenant/user identifiers.

## 2. Impact Analysis

Epic impact: No epic scope changes are required. This is documentation drift against completed Epic 1, Epic 2, Epic 3, Epic 4, and in-progress Epic 5 evidence.

Story impact: Current implementation stories stay unchanged. Story 5.7 remains the global-administrator correction verification gate. Story 5.8 remains unaffected.

Artifact conflicts: The affected artifacts are historical UI planning/evidence docs:

- `docs/tenants-ui-phase-2-story-backlog.md`
- `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md`
- `docs/tenants-ui-truth-state-and-action-availability-spec.md`
- `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md`
- `docs/tenants-ui-frontcomposer-dependency-map.md`
- `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md`

Technical impact: Documentation only. No production code, contracts, routes, tests, packages, or submodules are changed.

## 3. Recommended Approach

Direct adjustment.

Update the affected specs to distinguish delivered Tenants-owned story evidence from still-unresolved reusable FrontComposer work. Keep reusable `<AuditTimeline>`, grouped audit mode, `<ConsequencePreview>`, batching, token, and documentation gaps visible without presenting delivered flows as blocked.

Risk is low. The main risk is over-promoting reusable FrontComposer capabilities; the edits avoid that by saying the delivered Tenants-owned slices are complete while reusable component work remains separately tracked.

## 4. Detailed Change Proposals

Backlog readiness summaries:

OLD:

```text
Ready With Approved Fallback: No rows currently qualify.
Blocked: ui-11, ui-12, ui-14, ui-15.
```

NEW:

```text
Ready With Approved Fallback: ui-10, ui-13.
Implemented / Superseded by Story Evidence: ui-01 through ui-09, ui-15.
Blocked: ui-11, ui-12, ui-14 only for reusable component work.
```

Rationale: Align historical summary text with delivered Epic 1 through Epic 5 evidence while preserving reusable FrontComposer blockers.

Audit/recovery global-administrator correction:

OLD:

```text
Global-administrator correction described Epic 4 command support as still missing.
```

NEW:

```text
Epic 4 supplies global-administrator grant/remove command support; Story 5.7 is the verification gate before audit-based global-administrator correction may enable.
```

Rationale: Current risk is fixed-scope verification, not absence of Epic 4 command support.

Identifier/support-safety wording:

OLD:

```text
Broad or ambiguous ID wording for unsafe copied/rendered values.
```

NEW:

```text
internal correlation/message ids / unsafe internal identifier exposure / internal correlation/message identifiers
```

Rationale: TenantId and UserId are support-safe caller-supplied strings when explicitly allowed; unsafe values are internal command/status identifiers and raw backend metadata.

## 5. Implementation Handoff

Scope classification: Minor documentation-only correction.

Handoff recipient: Technical writer / developer maintaining BMAD artifacts.

Success criteria:

- Delivered Epic 1 through Epic 5 Tenants-owned UI evidence is not described as active blocked/planning-only implementation work.
- Reusable FrontComposer blockers remain visible and separate from delivered story evidence.
- Story 5.7 remains the active audit-based global-administrator correction gate.
- Support-safety wording distinguishes caller-supplied tenant/user identifiers from internal command message/correlation identifiers.

## Checklist Result

- [x] Trigger and evidence identified.
- [x] Epic and story impact assessed.
- [x] Artifact conflicts identified.
- [x] Direct adjustment selected.
- [x] Documentation edits produced.
- [x] Handoff plan defined.
