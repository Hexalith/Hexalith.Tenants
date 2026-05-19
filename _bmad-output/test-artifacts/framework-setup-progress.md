---
stepsCompleted: ['step-01-preflight']
lastStep: 'step-01-preflight'
lastSaved: '2026-05-19'
---

# Test Framework Setup — Progress

## Step 1: Preflight

### Stack Detection

- **`config.test_stack_type`:** `auto` (from `_bmad/tea/config.yaml`)
- **Detected stack:** **`backend`**

**Evidence:**

- **Backend manifests present (dominant signal):**
  - `global.json` pinning .NET SDK `10.0.300`
  - `Hexalith.Tenants.slnx` modern XML solution file
  - 10 `*.csproj` files under `src/` and `samples/`
  - 5 `*.csproj` test files under `tests/`
  - `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props` (centralized package management)
- **Frontend manifest is tooling-only:**
  - `package.json` at root declares **only** `semantic-release`, `@semantic-release/*`, and `@commitlint/*` dev dependencies
  - **No** React/Vue/Angular/Next dependencies
  - **No** `vite.config.*`, `webpack.config.*`, or other frontend build configs
  - **No** `playwright.config.*` or `cypress.config.*` at any depth
- **Submodules with frontend** (`Hexalith.FrontComposer`, `Hexalith.EventStore`) are explicitly **out of scope** for Hexalith.Tenants work per their respective project-context rules.

### Prerequisites Validation

Per the `backend` branch of the preflight rules:

| Check | Status | Notes |
|---|---|---|
| At least one backend project manifest exists | ✅ | 15 `.csproj` files + `global.json` |
| No existing test framework config that conflicts | ⚠️ **CALL-OUT** | A mature xUnit test framework is already in place (5 test projects, Tier 1/2/3 tiering, Shouldly assertions, NSubstitute mocks, conformance helpers). Not a *conflict* in the rule's pytest/JUnit sense, but it materially changes the value calculus of running this skill. |
| Architecture/stack context available | ✅ | Rich BMAD planning artifacts under `_bmad-output/planning-artifacts/` (architecture.md, prd.md, epics.md) and `project-context.md` files in submodules |

### Project Context Gathered

**Stack:**

- .NET 10.0.300, C# `LangVersion=latest`
- xUnit + Shouldly + NSubstitute + coverlet (per CLAUDE.md and submodule conventions)
- DAPR sidecar (1.17.x), .NET Aspire orchestration
- ASP.NET Core (REST API + SignalR)
- Multi-tenant event-sourced backend service

**Existing Test Projects (5, under `tests/`):**

| Project | Tier | Coverage Areas |
|---|---|---|
| `Hexalith.Tenants.Contracts.Tests` | 1 (unit) | Queries |
| `Hexalith.Tenants.Client.Tests` | 1 (unit) | Handlers, Projections, Registration, Subscription |
| `Hexalith.Tenants.Testing.Tests` | 1 (unit) | Conformance, Fakes, Helpers, Projections |
| `Hexalith.Tenants.Server.Tests` | 2 (integration) | Aggregates, Bootstrap, CommandPipeline, Configuration, DomainProcessing, Health, Projections, Queries, Telemetry, Validators |
| `Hexalith.Tenants.IntegrationTests` | 3 (E2E/Aspire) | Fixtures |

**Existing BMAD Test Artifacts (`_bmad-output/test-artifacts/`):**

- `test-design-epic-10.md` (Epic 10 — Durable Projection Write Safety; 23 risks identified, 25 tests + 1 fixture rule + 1 CI guard)
- `atdd-checklist-10-4-projection-write-conformance-and-recovery-tests.md`
- `automation-summary.md`
- `test-design-progress.md`

**No existing E2E framework configs found** (no `playwright.config.*`, no `cypress.config.*`).

### Murat's Confirmed Findings — and a Risk Flag

Jerome, here's the honest read.

**The skill is *Playwright/Cypress*-shaped**, but Hexalith.Tenants is **backend-only** with no UI surface exposed from this repo. The Tenants admin UI lives in `Hexalith.FrontComposer` (a separate root, explicitly out-of-scope per its own project-context rules) and the EventStore admin lives in `Hexalith.EventStore` (likewise separate). Scaffolding Playwright in *this* repo would have nothing to drive against.

You also already have a **mature xUnit framework** with proper Tier 1/2/3 separation, Shouldly + NSubstitute conventions baked into `Directory.Build.props`, conformance helpers in `Hexalith.Tenants.Testing`, and integration fixtures in `Hexalith.Tenants.IntegrationTests`. That work is already done at high quality (the Epic 10 test design I drafted on 2026-05-19 leans on exactly that scaffolding).

So the **skill's default output (Playwright UI E2E scaffolding) doesn't fit cleanly**. Three plausible re-aims:

1. **Bail out of TF and re-route** — pick a different skill on the menu that better fits the actual gap:
   - **TD (Test Design)** for a new epic
   - **AT (ATDD)** to flesh out 10.4's red-phase tests
   - **RV (Review Tests)** to audit existing xUnit suites against best-practice
   - **CI (Continuous Integration)** to scaffold quality gates around the current test tiers
   - **NR (NFRs)** to formalize the multi-tenant/auth/perf assessment
2. **Press on — scaffold Playwright** anyway, targeting the eventual Tenants UI surface (i.e., a Playwright project that lives here and drives FrontComposer's Tenants admin UI when it ships). This installs the harness *now* so Story 12.x (frontshell dependency map → Tenants UI integration) lands on rails.
3. **Press on — scaffold an HTTP/API test framework** (Playwright-Utils `api-request` + `auth-session` patterns, or a dedicated `Hexalith.Tenants.ApiSmokeTests` xUnit project) for black-box service-layer testing against the running Tenants service. Useful as a CI smoke lane separate from the in-process integration tests.

I'd lean **#1 → RV or CI** as the highest-value next move given where Epic 10 sits, with #2 reserved for whenever the frontshell story ships UI surface. But you know the roadmap better than I do — what's the actual gap you wanted TF to close?
