# Sprint Change Proposal — 2026-06-24

**Trigger:** UI presentation issue on the Tenants list page — the "Create tenant" block dominates the viewport.
**Requested by:** Jérôme Piquot
**Mode:** Batch
**Scope classification:** Minor (direct Developer-agent implementation)

---

## Section 1 — Issue Summary

On the Tenants list page (`/` and `/tenants`), the **"Créer un locataire" (Create tenant)** block is rendered
in the `<Commands>` slot of the shared `FcAggregateListPage<TItem>` wrapper. That slot renders **vertically
between the filters and the tenant list grid**:

```
FcPageHeader → Filters → Commands (Create form) → States → Body (grid) → Pager
```

As a result the create form — a large bordered card with the id/name/description fields plus the
"Cycle de vie de la commande" (command lifecycle) sub-section — occupies roughly half the visible height on
arrival and **pushes the tenant list (the page's primary content) far down the page**. Evidence: user
screenshot at `localhost:62445` showing the create card filling the area between the filter bar and the first
grid row.

**Issue type:** Misaligned presentation / UX refinement (not a defect, not a requirement gap).

---

## Section 2 — Impact Analysis

| Artifact | Impact |
| --- | --- |
| **PRD** (`prds/prd-tenants-2026-06-02`) | None. No requirement, goal, or MVP scope change. FR13 (projection-confirmed create) is untouched. |
| **Architecture** (`architecture.md`) | None. No contract, gateway, BFF, or actor-routing change. |
| **Epics** (`epics.md`) | None. Epic 2 / Story 2.1 acceptance criteria remain satisfied. |
| **UX rules** (`hexalith-ux-instructions.md` — "Page sections") | **Positive alignment.** The rule states secondary titled sections belong in a `FluentAccordion`, while the single primary content region (the grid) stays outside it. This change moves the page *toward* conformance. |
| **Conformance guard** (`DomainUiFluentConformanceTests`) | No regression. `TenantsWorkspace.razor` is intentionally **not** in the `accordionRequiredFiles` list (that guard mandates `Expanded="true"` for pages whose accordion holds *primary* content). Here the accordion holds a *secondary* action region and is collapsed by default; the grid stays the visible primary content. |
| **Story 2.1 ACs** | All preserved. Stable selectors (`tenants-create-flow`, `tenants-create-tenant-id`, `tenants-create-submit`, `tenants-create-lifecycle`, …), lifecycle states, fail-closed gating, live-region politeness, and focus behavior are unchanged — only the container chrome and the duplicate visible title change. |

**Epic Impact Assessment (checklist §2):** No epic added, removed, resequenced, or rescoped. `[N/A]`

**Files in scope (Tenants domain only — no FrontComposer/EventStore change):**
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor`
- `tests/Hexalith.Tenants.UI.Tests/Components/CreateTenantFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`
- (optional) `src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor.css`

---

## Section 3 — Recommended Approach

**Selected path: Option 1 — Direct Adjustment** (modify the existing Story 2.1 surface in place).
Rollback (Option 2) and MVP review (Option 3) are not applicable — nothing is being reverted or rescoped.

Wrap the `CreateTenantFlow` component (the `<Commands>` slot content of `TenantsWorkspace.razor`) in a
`FluentAccordion` / `FluentAccordionItem`, **collapsed by default** (`Expanded="false"`), using the exact
v5 API already used elsewhere in the module (`Header`, `ExpandMode="AccordionExpandMode.Multi"`). The tenant
grid remains the visible primary content immediately on page load. The duplicate visible card title is
removed (the accordion header becomes the section title) via a new `ShowHeading` parameter on
`CreateTenantFlow`.

- **Effort:** Low · **Risk:** Low · **Timeline impact:** None.

---

## Section 4 — Detailed Change Proposals

### 4.1 `CreateTenantFlow.razor` — add `ShowHeading` to avoid a duplicate title

**Markup (lines 6-10) — OLD:**
```razor
<section class="tenants-create-flow" data-testid="tenants-create-flow" aria-labelledby="tenants-create-heading">
    <FluentStack Orientation="Orientation.Vertical" VerticalGap="0.25rem" Class="tenants-create-flow__heading">
        <h2 id="tenants-create-heading">@Localizer["Tenants.Create.Title"]</h2>
        <p>@Localizer["Tenants.Create.Description"]</p>
    </FluentStack>
```

**NEW:**
```razor
<section class="tenants-create-flow" data-testid="tenants-create-flow"
         aria-labelledby="@(ShowHeading ? "tenants-create-heading" : null)"
         aria-label="@(ShowHeading ? null : Localizer["Tenants.Create.Title"].Value)">
    <FluentStack Orientation="Orientation.Vertical" VerticalGap="0.25rem" Class="tenants-create-flow__heading">
        @if (ShowHeading)
        {
            <h2 id="tenants-create-heading">@Localizer["Tenants.Create.Title"]</h2>
        }
        <p>@Localizer["Tenants.Create.Description"]</p>
    </FluentStack>
```

**`@code` — add parameter:**
```csharp
[Parameter]
public bool ShowHeading { get; set; } = true;
```

*Rationale:* default `true` preserves the standalone behavior (and all existing `CreateTenantFlow` tests);
when hosted inside an accordion the host sets `ShowHeading="false"` so the title is not shown twice. The
section keeps an accessible name (`aria-label`) when its `<h2>` is suppressed, so no dangling `aria-labelledby`
IDREF is produced.

### 4.2 `TenantsWorkspace.razor` — wrap the `<Commands>` slot in a collapsed accordion

**OLD (lines 76-81):**
```razor
<Commands>
    <CreateTenantFlow IsAuthorized="@(_snapshot.Kind is not (TenantListSurfaceKind.Error or TenantListSurfaceKind.Unauthorized))"
                      IsFresh="@(_snapshot.Freshness is TenantFreshnessState.Current or TenantFreshnessState.Unknown)"
                      OnProjectionRefreshRequested="RefreshAsync"
                      ProjectionEvidenceProvider="FindTenantProjectionEvidenceAsync" />
</Commands>
```

**NEW:**
```razor
<Commands>
    <FluentAccordion ExpandMode="AccordionExpandMode.Multi" Class="tenants-workspace__commands">
        <FluentAccordionItem Header="@Localizer["Tenants.Create.Title"]"
                             Expanded="false"
                             data-testid="tenants-create-accordion">
            <CreateTenantFlow IsAuthorized="@(_snapshot.Kind is not (TenantListSurfaceKind.Error or TenantListSurfaceKind.Unauthorized))"
                              IsFresh="@(_snapshot.Freshness is TenantFreshnessState.Current or TenantFreshnessState.Unknown)"
                              ShowHeading="false"
                              OnProjectionRefreshRequested="RefreshAsync"
                              ProjectionEvidenceProvider="FindTenantProjectionEvidenceAsync" />
        </FluentAccordionItem>
    </FluentAccordion>
</Commands>
```

*Rationale:* reuses the exact accordion API already shipped in `TenantConfigurationView.razor` and
`GlobalAdministratorsPage.razor`. Collapsed by default (per the approved design decision) so the grid is the
immediate primary content. Imports already resolve `FluentAccordion`/`AccordionExpandMode` module-wide.

### 4.3 Tests

- **`CreateTenantFlowTests.cs`** — add: with `ShowHeading="false"`, no `#tenants-create-heading` `<h2>` is
  rendered and the `tenants-create-flow` section exposes an accessible name; default render still emits the
  `<h2>`.
- **`TenantsWorkspaceTests.cs`** — add: the `tenants-create-accordion` item is present, its header shows the
  create title, and the `tenants-create-flow` content is reachable inside it (bUnit renders accordion-item
  child content regardless of collapsed state).

### 4.4 (Optional) `CreateTenantFlow.razor.css`

The bordered `.tenants-create-flow` card now sits inside the accordion body, which already provides a
container. Dropping the card border/padding when hosted in the accordion is a possible visual polish but is
**not required**; left out of this proposal to keep the change minimal. Flag for a follow-up only if the
nested-container look is undesirable.

---

## Section 5 — Implementation Handoff

- **Scope:** Minor → **Developer agent**, direct implementation.
- **Deliverables:** the edits in §4.1–§4.3; build `Hexalith.Tenants.slnx` (Release, warnings-as-errors clean);
  run `Hexalith.Tenants.UI.Tests` green (including the two new tests and the `DomainUiFluentConformanceTests`
  governance lane).
- **Success criteria:**
  1. On `/tenants`, the create block renders as a single collapsed "Créer un locataire" accordion bar between
     the filters and the grid; the grid is visible without scrolling past the form.
  2. Expanding the accordion reveals the unchanged create form + lifecycle panel; all `tenants-create-*`
     selectors and behaviors still work.
  3. No duplicate visible "Créer un locataire" title.
  4. Tenants UI test suite green; conformance governance lane unaffected.
- **Out of scope:** any FrontComposer/EventStore change; backend, contract, or routing change; making the
  collapsible-command region a generic `FcAggregateListPage` capability (possible future enhancement).
