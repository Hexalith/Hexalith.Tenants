# Sprint Change Proposal — Catalog-Wide NuGet Currency Sweep

- **Date:** 2026-08-20
- **Prepared by:** Amelia (Developer), via `bmad-correct-course`
- **Requested by:** Jérôme Piquot
- **Review mode:** Batch
- **Status:** DRAFT — awaiting approval (Step 5)
- **Owning repository:** `Hexalith.Builds` (not `Hexalith.Tenants`)

---

## 1. Issue Summary

### 1.1 Trigger

Direct request: *"update all nuget packages to latest version."* This is not a
story-derived defect; it is a deliberate dependency-currency sweep initiated
outside the story flow.

**Issue type:** Technical maintenance / dependency currency (not a requirements change).

### 1.2 The core problem the scan uncovered

The request cannot be executed inside `Hexalith.Tenants` at all, and the naive
reading of "latest" is actively destructive. Three findings define the change.

**F1 — Tenants does not own its package versions.**
`Directory.Packages.props` at the Tenants root is a 13-line shim. Every
`<PackageVersion>` resolves from
`references/Hexalith.Builds/Props/Directory.Packages.props`, inside a
**root-declared submodule shared by every Hexalith repository**. Per `CLAUDE.md`,
work belongs to the repo that owns the change, and submodule files are not to be
modified without explicit approval. This sweep is therefore a **Hexalith.Builds
change** that Tenants consumes via a gitlink bump.

**F2 — "Latest" is not monotonic with "better".**
Resolving all 284 centrally managed packages against nuget.org with correct SemVer
prerelease ordering yields **111 packages with a higher version available**. Of
those, 46 must be refused:

- **22 packages** (`Microsoft.AspNetCore.*` at `11.0.0-preview.7.26381.103`)
  publish **only a `net11.0` asset group**. Every project in this repo targets
  `net10.0` and `global.json` pins SDK `10.0.302`. Restoring them is an **NU1202
  hard failure**, not a warning.
- **18 more** in the same .NET 11 preview-7 band do carry a `net10.0` asset, so
  they would restore — but taking them while the 22 above stay at `10.0.11` splits
  the framework family across two major versions.
- **3 are strict regressions** despite higher SemVer:
  `System.ComponentModel.Annotations 6.0.0-preview.4.21253.7` is a 2021-era
  abandoned preview whose TFM set is identical to the `5.0.0` stable it would
  replace; `Serilog 4.4.1-dev-02443` and `Serilog.Sinks.File 8.0.0-nblumhardt-02322`
  are CI/personal-branch builds rather than release-channel artifacts.
- **`Microsoft.OpenApi 2.12.0 -> 3.10.0`** is refused against an explicit in-file
  warning that ASP.NET Core OpenAPI 10.x is compiled against the 2.x surface.

**F3 — Two undeclared gitlink advances are already sitting uncommitted.**
The working tree carries `references/Hexalith.Builds` `17b1c7aa -> eadddc7b` and
`references/Hexalith.EventStore` `c21bd749 -> a55b5bef` (the `3.96.2` preflight),
neither committed nor declared. The Builds props file still pins
`HexalithEventStoreVersion = 3.95.0`, so package/source skew is live right now —
the same shape as the July NU1107 incident that
`scripts/validate-story-gitlinks.py` was built to catch. This is the **third**
recorded occurrence of an undeclared `references/` bump.

### 1.3 Evidence

| Evidence | Result |
|---|---|
| `git submodule status` | Builds at `v4.24.0`, working tree advanced to `eadddc7b`; EventStore advanced to `a55b5bef` |
| `grep TargetFramework` across `src`/`tests`/`samples`/`tools` | **`net10.0`, single value, no exceptions** |
| `global.json` | `sdk.version 10.0.302`, `rollForward: latestPatch` |
| nuget.org manifest, `Microsoft.AspNetCore.Authentication.JwtBearer 11.0.0-preview.7.26381.103` | `tfms = ['net11.0']` — **no `net10.0` asset** |
| nuget.org, `xunit.v3 4.0.0` | published **2026-08-15** (5 days before this proposal) |
| nuget.org, `Microsoft.FluentUI.AspNetCore.Components 5.0.0-rc.5-26219.1` | published 2026-08-09 |
| `dotnet restore Hexalith.Tenants.slnx -p:Configuration=Release` | **exit 0, 0 errors** — verified pre-change baseline |
| `.github/workflows/ci.yml` | delegates to `Hexalith.Builds/.github/workflows/domain-ci.yml@main`; that workflow checks out `submodules: false`, then initialises root-declared submodules **at the pinned gitlink** |

---

## 2. Impact Analysis

### 2.1 Ownership and propagation

The change lands in `Hexalith.Builds`. Because `domain-ci.yml` initialises
submodules at each consumer's **pinned gitlink**, Tenants CI does not observe the
new versions until the Tenants gitlink advances. That is the safe propagation
order, and this proposal preserves it:

```
Hexalith.Builds  --edit Props/Directory.Packages.props-->  merge + tag
        |
        +--> each consumer repo bumps its references/Hexalith.Builds gitlink
                     |
                     +--> that repo's CI restores against the new versions
```

Consumers other than Tenants (`Hexalith.EventStore`, `Hexalith.FrontComposer`,
`Hexalith.Memories`, `Hexalith.Commons`, `Hexalith.PolymorphicSerializations`)
inherit the same catalog and **cannot be validated from this repository**. 46 of
the 65 accepted bumps touch packages Tenants does not reference.

### 2.2 Epic impact

**No epic changes scope, acceptance criteria, sequencing, or priority.** This is
infrastructure currency; no functional requirement moves.

The impact is on **in-flight review loops**, not on epic definitions:

| Story | Status | Effect |
|---|---|---|
| `2-1-reverify-projection-confirmed-membership-command-foundation` | `review` | Test-framework major (xunit v3->v4) re-baselines its evidence |
| `2-4-remove-tenant-member-with-complete-preview-and-proof` | `in-progress` | Active edits collide with a repo-wide restore change |
| `3-1-create-tenant-with-projection-confirmation` | `review` | Re-baselined |
| `3-2-edit-tenant-metadata-with-recorded-updates` | `review` | Re-baselined |

All remaining epics (3.3–3.6, 4.x, 5.x) are `backlog` — unaffected.

### 2.3 Artifact conflicts

**`_bmad-output/planning-artifacts/architecture.md`** — 5 sites pin Fluent UI
`5.0.0-rc.4-26180.1` (L226, L257, L260, L335, L368). One block (L259–261) is
**already stale before this change**: it claims a 2026-07-15 baseline of
FrontComposer `3.1.1` / EventStore `3.64.1` / Memories `2.5.0`, while the catalog
actually holds `4.1.1` / `3.95.0` / `2.21.3`.

**`_bmad-output/project-context.md`** — the "Technology Stack & Versions" section
carries **14 verified pre-existing drifts**, independent of this sweep:

| Claim in project-context.md | Actual in catalog today |
|---|---|
| Hexalith.EventStore `3.19.0` | `3.95.0` |
| Hexalith.Memories `1.31.1` | `2.21.3` |
| MediatR `14.1.0` | `14.2.0` |
| Fluent UI `5.0.0-rc.3-26138.1` | `5.0.0-rc.4-26180.1` |
| bUnit `2.8.4-preview` | `2.9.0` |
| NSubstitute `6.0.0-rc.1` | `6.2.0` |
| Testcontainers `4.12.0` | `4.14.0` |
| Microsoft.NET.Test.Sdk `18.7.0` | `18.9.0` |
| DAPR SDK `1.18.4` | `1.18.5` |
| OpenTelemetry `1.16.0` / Runtime `1.15.1` | `1.17.0` |
| IdentityModel `8.19.1` | `8.22.0` |
| YamlDotNet `18.0.0` | `18.1.0` |
| OpenAPI `10.0.9` | `10.0.11` |
| CommunityToolkit.Aspire.Hosting.Dapr `13.4.0-preview.1.260602-0230` | `13.4.1-beta.706` |

**PRD** (`prds/prd-tenants-2026-06-02/`) — no conflict. No functional requirement
references a package version.

**UX specifications** — no conflict *by content*, but Fluent `rc.4 -> rc.5` changes
the component library behind every governed UI surface. Token, ARIA-name, and
generated-markup assertions are verified against the pinned package at build, so
they must be re-run rather than assumed.

### 2.4 Technical impact

| Area | Risk | Notes |
|---|---|---|
| **xunit.v3 `3.2.2 -> 4.0.0`** | **HIGH** | A major, 5 days old. Governs all 5 CI test tiers. `xunit.runner.visualstudio 4.0.0` must move in lockstep. The repo already documents a .NET 10 Microsoft.Testing.Platform/VSTest incompatibility with an executable fallback — a runner major interacts directly with that. |
| **Shouldly `4.3.0 -> 5.0.0-preview.2`** | **HIGH** | Major *preview* of the assertion library used by every test. Ships no `net10.0` asset (netstandard2.0/net8.0/net9.0), same as 4.3.0. |
| **Fluent UI `rc.4 -> rc.5`** | **MEDIUM-HIGH** | Governed UI surface; `DomainUiFluentConformanceTests` (12 guards, div+span budget) must be re-run. Note `Components.Icons 5.0.0-rc.5-26219.1` publishes a `net9.0`-only dependency group. |
| **Dapr `1.18.5 -> 1.19.0-preview.2`** | **MEDIUM** | SDK preview against CLI/runtime `1.18.0` installed by `domain-ci`. Pub/sub, actors, and access-control routing all ride this. Runtime/SDK version pairing needs an explicit decision. |
| **Hexalith.EventStore `3.95.0 -> 3.96.2`** | **MEDIUM** | Must land together with the `references/Hexalith.EventStore` gitlink already advanced to `a55b5bef`, or the Debug-source / Release-package skew persists. |
| **Aspire `13.4.6 -> 13.5.0`** | **MEDIUM** | Includes Keycloak/Kubernetes preview siblings `13.4.6-preview.1.26319.6 -> 13.5.0-preview.1.26417.10`. The AppHost app model is built at startup — `aspire run` must be restarted and re-verified. |
| **Roslyn `5.6.0 -> 5.9.0`** | **MEDIUM** | `TreatWarningsAsErrors=true` + CI `-warnaserror` means any new analyzer diagnostic is a **build failure**, not a warning. |
| Aspire.Azure.*, SemanticKernel, NBomber, Radzen, Roslynator, Kreuzberg, HotChocolate, Verify, Azure.* betas | UNVERIFIABLE HERE | Not referenced by Tenants. Validation belongs to the owning consumer repos. |

---

## 3. Recommended Approach

**Selected path: Option 1 — Direct Adjustment, executed as a Builds-owned change
with a staged Tenants adoption.**

- *Option 2 (Rollback)* — **not viable**. Nothing is broken; there is no completed
  work whose reversion would simplify anything.
- *Option 3 (MVP review)* — **not viable / not applicable**. No functional scope,
  goal, or MVP boundary is touched.

### 3.1 Rationale

The sweep is mechanical and reversible, and it does not alter a single acceptance
criterion. It is nonetheless too large and too test-framework-invasive to ride
along inside an open story: four review loops are live, and `xunit v3->v4` plus
`Shouldly 5.0.0-preview` would re-baseline the very evidence those loops are
judging.

### 3.2 Staging

| Wave | Content | Risk | Gate |
|---|---|---|---|
| **W0** | Commit the two already-advanced gitlinks as a standalone declared `build(deps)` commit, before anything else | LOW | `dotnet restore` clean; `scripts/validate-story-gitlinks.py` |
| **W1** | Low-risk currency: Aspire `13.5.0` family, EventStore `3.96.2`, Roslyn `5.9.0`, Roslynator, Radzen, TypeScript.MSBuild, SemanticKernel, NBomber | LOW–MED | Release build `-warnaserror` 0/0; Tier 1 + Tier 2 |
| **W2** | Test-stack major: `xunit.v3` + `.assert` + `.extensibility.core` + `xunit.runner.visualstudio` -> `4.0.0`; `Shouldly 5.0.0-preview.2`; `Verify 32.0.0-beta.11` | **HIGH** | All 5 tiers green; coverage gates (line >80%, branch 100% on isolation targets) re-proven |
| **W3** | UI surface: Fluent UI `rc.5` (+ Icons) | MED–HIGH | Full `UI.Tests`; Fluent conformance governance suite; live `aspire run` check |
| **W4** | Runtime preview: Dapr SDK `1.19.0-preview.2` | MED | Tier 2 after `dapr init`; access-control YAML + route tests; Tier 3 Aspire |
| **W5** | Non-Tenants remainder (Azure.*, Kreuzberg, HotChocolate, Cosmos, Identity.Client, AngleSharp, BenchmarkDotNet, Newtonsoft beta) | UNKNOWN | Validated by owning consumer repos, not here |

**Effort:** Medium–High (W2 and W3 dominate).
**Risk:** Medium overall; High within W2.
**Timeline:** W0 immediate; W1 same day; W2–W4 one focused story each; W5 gated on other repos.

---

## 4. Detailed Change Proposals

### 4.1 `Hexalith.Builds` — `Props/Directory.Packages.props`

**65 packages accepted** (19 directly referenced by Tenants).

| Package | From | To | Bump | Tenants |
|---------|------|----|------|---------|
| `Aspire.Hosting` | `13.4.6` | `13.5.0` | minor | yes |
| `Aspire.Hosting.Testing` | `13.4.6` | `13.5.0` | minor | yes |
| `Dapr.AspNetCore` | `1.18.5` | `1.19.0-preview.2` | minor | yes |
| `Dapr.Client` | `1.18.5` | `1.19.0-preview.2` | minor | yes |
| `Hexalith.EventStore.Aspire` | `3.95.0` | `3.96.2` | minor | yes |
| `Hexalith.EventStore.Client` | `3.95.0` | `3.96.2` | minor | yes |
| `Hexalith.EventStore.Contracts` | `3.95.0` | `3.96.2` | minor | yes |
| `Hexalith.EventStore.DomainService` | `3.95.0` | `3.96.2` | minor | yes |
| `Hexalith.EventStore.Gateway` | `3.95.0` | `3.96.2` | minor | yes |
| `Hexalith.EventStore.RestApi.Generators` | `3.95.0` | `3.96.2` | minor | yes |
| `Hexalith.EventStore.Server` | `3.95.0` | `3.96.2` | minor | yes |
| `Hexalith.EventStore.ServiceDefaults` | `3.95.0` | `3.96.2` | minor | yes |
| `Hexalith.EventStore.Testing` | `3.95.0` | `3.96.2` | minor | yes |
| `Hexalith.EventStore.Testing.Integration` | `3.95.0` | `3.96.2` | minor | yes |
| `Microsoft.FluentUI.AspNetCore.Components` | `5.0.0-rc.4-26180.1` | `5.0.0-rc.5-26219.1` | pre-track | yes |
| `Shouldly` | `4.3.0` | `5.0.0-preview.2` | **MAJOR** | yes |
| `xunit.runner.visualstudio` | `3.1.5` | `4.0.0` | **MAJOR** | yes |
| `xunit.v3` | `3.2.2` | `4.0.0` | **MAJOR** | yes |
| `xunit.v3.assert` | `3.2.2` | `4.0.0` | **MAJOR** | yes |
| `AngleSharp` | `1.7.1` | `1.8.0-beta.603` | minor |  |
| `Aspire.Azure.Messaging.ServiceBus` | `13.4.6` | `13.5.0` | minor |  |
| `Aspire.Azure.Security.KeyVault` | `13.4.6` | `13.5.0` | minor |  |
| `Aspire.Azure.Storage.Blobs` | `13.4.6` | `13.5.0` | minor |  |
| `Aspire.Azure.Storage.Queues` | `13.4.6` | `13.5.0` | minor |  |
| `Aspire.Hosting.Azure.AppContainers` | `13.4.6` | `13.5.0` | minor |  |
| `Aspire.Hosting.Azure.CosmosDB` | `13.4.6` | `13.5.0` | minor |  |
| `Aspire.Hosting.Docker` | `13.4.6` | `13.5.0` | minor |  |
| `Aspire.Hosting.Keycloak` | `13.4.6-preview.1.26319.6` | `13.5.0-preview.1.26417.10` | minor |  |
| `Aspire.Hosting.Kubernetes` | `13.4.6-preview.1.26319.6` | `13.5.0-preview.1.26417.10` | minor |  |
| `Aspire.Hosting.Redis` | `13.4.6` | `13.5.0` | minor |  |
| `Aspire.Microsoft.Azure.Cosmos` | `13.4.6` | `13.5.0` | minor |  |
| `Azure.ResourceManager.CognitiveServices` | `1.5.2` | `1.6.0-beta.3` | minor |  |
| `Azure.ResourceManager.ContainerRegistry` | `1.4.0` | `1.5.0-beta.3` | minor |  |
| `Azure.Security.KeyVault.Secrets` | `4.11.0` | `4.12.0-beta.1` | minor |  |
| `Azure.Storage.Blobs` | `12.29.1` | `12.30.0-beta.1` | minor |  |
| `BenchmarkDotNet` | `0.15.8` | `0.16.0-preview.1` | minor |  |
| `Dapr.AI` | `1.18.5` | `1.19.0-preview.2` | minor |  |
| `Dapr.AI.Microsoft.Extensions` | `1.18.5` | `1.19.0-preview.2` | minor |  |
| `Dapr.Actors` | `1.18.5` | `1.19.0-preview.2` | minor |  |
| `Dapr.Actors.AspNetCore` | `1.18.5` | `1.19.0-preview.2` | minor |  |
| `Dapr.Actors.Generators` | `1.18.5` | `1.19.0-preview.2` | minor |  |
| `Dapr.Workflow` | `1.18.5` | `1.19.0-preview.2` | minor |  |
| `Hexalith.EventStore.Admin.Abstractions` | `3.95.0` | `3.96.2` | minor |  |
| `Hexalith.EventStore.Admin.Server` | `3.95.0` | `3.96.2` | minor |  |
| `Hexalith.EventStore.SignalR` | `3.95.0` | `3.96.2` | minor |  |
| `HotChocolate` | `16.6.1` | `16.6.2-p.4` | patch |  |
| `Kreuzberg` | `4.10.2` | `5.0.0-rc.35` | **MAJOR** |  |
| `Microsoft.Azure.Cosmos` | `3.62.1` | `3.63.0-preview.1` | minor |  |
| `Microsoft.CodeAnalysis.Analyzers` | `5.6.0` | `5.9.0` | minor |  |
| `Microsoft.CodeAnalysis.CSharp` | `5.6.0` | `5.9.0` | minor |  |
| `Microsoft.CodeAnalysis.CSharp.Workspaces` | `5.6.0` | `5.9.0` | minor |  |
| `Microsoft.CodeAnalysis.Common` | `5.6.0` | `5.9.0` | minor |  |
| `Microsoft.CodeAnalysis.Workspaces.Common` | `5.6.0` | `5.9.0` | minor |  |
| `Microsoft.FluentUI.AspNetCore.Components.Icons` | `5.0.0-rc.4-26180.1` | `5.0.0-rc.5-26219.1` | pre-track |  |
| `Microsoft.Identity.Client` | `4.87.0` | `4.87.1-preview.2` | patch |  |
| `Microsoft.SemanticKernel` | `1.79.0` | `1.80.0` | minor |  |
| `Microsoft.TypeScript.MSBuild` | `7.0.0` | `7.0.1` | patch |  |
| `NBomber` | `6.5.0` | `6.6.0` | minor |  |
| `Newtonsoft.Json` | `13.0.4` | `13.0.5-beta1` | patch |  |
| `Radzen.Blazor` | `11.2.5` | `11.2.6` | patch |  |
| `Roslynator.Analyzers` | `4.16.0` | `4.16.1` | patch |  |
| `Roslynator.Formatting.Analyzers` | `4.16.0` | `4.16.1` | patch |  |
| `Verify` | `31.28.0` | `32.0.0-beta.11` | **MAJOR** |  |
| `Verify.XunitV3` | `31.28.0` | `32.0.0-beta.11` | **MAJOR** |  |
| `xunit.v3.extensibility.core` | `3.2.2` | `4.0.0` | **MAJOR** |  |

Two of these are MSBuild property edits rather than `PackageVersion` attributes:

```xml
OLD: <HexalithEventStoreVersion Condition="'$(HexalithEventStoreVersion)' == ''">3.95.0</HexalithEventStoreVersion>
NEW: <HexalithEventStoreVersion Condition="'$(HexalithEventStoreVersion)' == ''">3.96.2</HexalithEventStoreVersion>
```

Rationale: 13 `Hexalith.EventStore.*` entries resolve through this single property,
and it must match the `references/Hexalith.EventStore` gitlink at `a55b5bef`.

### 4.2 Deliberate exclusions — 46 packages refused

Each is a package where a higher version exists and is **not** being taken.

| Package | Held at | Latest available | Reason refused |
|---------|---------|------------------|----------------|
| `Microsoft.AspNetCore.Authentication.Facebook` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.AspNetCore.Authentication.Google` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.AspNetCore.Authentication.MicrosoftAccount` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.AspNetCore.Authentication.OpenIdConnect` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.AspNetCore.Authorization` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.AspNetCore.Components.Authorization` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.AspNetCore.Components.CustomElements` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.AspNetCore.Components.Web` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.AspNetCore.Components.WebAssembly` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.AspNetCore.Components.WebAssembly.Authentication` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.AspNetCore.Components.WebAssembly.DevServer` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.AspNetCore.Components.WebAssembly.Server` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.AspNetCore.DataProtection` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.AspNetCore.DataProtection.Abstractions` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.AspNetCore.Mvc.Testing` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.AspNetCore.OpenApi` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.AspNetCore.SignalR.Client` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.AspNetCore.SignalR.StackExchangeRedis` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.AspNetCore.TestHost` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.Extensions.Configuration` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.Extensions.Configuration.Abstractions` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.Extensions.Configuration.Binder` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.Extensions.Configuration.FileExtensions` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.Extensions.Configuration.UserSecrets` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.Extensions.DependencyInjection` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.Extensions.Diagnostics.Abstractions` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.Extensions.Hosting` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.Extensions.Hosting.Abstractions` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.Extensions.Http` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.Extensions.Identity.Stores` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.Extensions.Localization` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.Extensions.Localization.Abstractions` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.Extensions.Logging.Abstractions` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.Extensions.Options` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.Extensions.Options.DataAnnotations` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Microsoft.OpenApi` | `2.12.0` | `3.10.0` | documented pin: AspNetCore.OpenApi 10.x compiled against 2.x surface |
| `Microsoft.SourceLink.GitHub` | `10.0.400` | `11.0.100-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `Serilog` | `4.4.0` | `4.4.1-dev-02443` | higher SemVer but worse artifact (abandoned 2021 preview / branch build) |
| `Serilog.Sinks.File` | `7.0.0` | `8.0.0-nblumhardt-02322` | higher SemVer but worse artifact (abandoned 2021 preview / branch build) |
| `System.Collections.Immutable` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `System.CommandLine` | `2.0.11` | `3.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |
| `System.ComponentModel.Annotations` | `5.0.0` | `6.0.0-preview.4.21253.7` | higher SemVer but worse artifact (abandoned 2021 preview / branch build) |
| `System.Text.Json` | `10.0.11` | `11.0.0-preview.7.26381.103` | .NET 11 preview-7 band; framework family held at 10.0.x |

### 4.3 `Hexalith.Tenants` — gitlink and declaration

```
OLD: references/Hexalith.Builds       17b1c7aa  (v4.24.0)
NEW: references/Hexalith.Builds       <new tag after Builds merge>

OLD: references/Hexalith.EventStore   c21bd749  (v3.95.0-3)
NEW: references/Hexalith.EventStore   a55b5bef  (3.96.2 preflight)
```

Rationale: `domain-ci.yml` restores against the pinned gitlink, so this bump is
what actually adopts the sweep in Tenants. Both pointers **must** be declared as
File List entries with reasons; `scripts/validate-story-gitlinks.py` fails the
story otherwise.

### 4.4 `architecture.md` — 5 edits

**Sites L226, L257, L335, L368** — Fluent pin:

```
OLD: Fluent UI Blazor v5 pinned `5.0.0-rc.4-26180.1`
NEW: Fluent UI Blazor v5 pinned `5.0.0-rc.5-26219.1`
```

**Site L259–261** — platform baseline (also corrects pre-existing drift):

```
OLD: **Current centralized platform package baselines (2026-07-15):** Hexalith.FrontComposer `3.1.1`,
     Hexalith.EventStore `3.64.1`, and Hexalith.Memories `2.5.0`.

NEW: **Current centralized platform package baselines (2026-08-20):** Hexalith.FrontComposer `4.1.1`,
     Hexalith.EventStore `3.96.2`, and Hexalith.Memories `2.21.3`.
```

Rationale: the existing line understates all three by a wide margin and would
otherwise be wrong in a second, newer way after this sweep.

### 4.5 `_bmad-output/project-context.md` — stack section refresh

Apply the 14 corrections tabulated in §2.3, overlay this sweep's results, and
update `Last Updated:` from `2026-06-29` to `2026-08-20`. Add an explicit line
recording the .NET 10 framework hold:

```
NEW: - **Framework family held at .NET 10.** `Microsoft.AspNetCore.*`,
       `Microsoft.Extensions.*`, `System.Text.Json`, and `System.Collections.Immutable`
       stay on `10.0.x` stable. Their latest nuget.org versions are .NET 11
       preview-7 (`11.0.0-preview.7.26381.103`); 22 of those publish a `net11.0`
       asset only and cannot restore against `net10.0` / SDK `10.0.302`.
       Do not "update to latest" on this family without a platform migration.
```

Rationale: this is the single fact most likely to be re-litigated by the next
agent asked to "update all packages".

### 4.6 `sprint-status.yaml`

Register the W2–W4 stories, and one action item for the undeclared-gitlink
recurrence (third occurrence — see §1.2 F3).

---

## 5. Implementation Handoff

**Scope classification: MODERATE**, escalating to **MAJOR** for W2.

Moderate because no requirement, epic, or acceptance criterion changes — but it
needs backlog reorganisation (three new stories), it crosses a repository boundary
into a shared submodule, and it re-baselines four open review loops. W2 alone
warrants architect involvement: a test-framework major touching every CI tier and
both coverage gates is an architectural dependency decision.

| Recipient | Responsibility |
|---|---|
| **Jérôme Piquot** | Approve the submodule write to `Hexalith.Builds` (required by `CLAUDE.md`); confirm the Dapr SDK-preview-vs-runtime-1.18.0 pairing |
| **Winston (Architect)** | Own the W2 xunit v3->v4 + Shouldly 5 preview decision; ratify the .NET 10 framework hold as a recorded invariant |
| **Amelia (Developer)** | Execute W0 immediately; author and implement W1–W4 stories |
| **Murat (Test Architect)** | Re-prove all 5 tiers and both coverage gates after W2; own the Fluent conformance re-run in W3 |
| **Paige (Tech Writer)** | Apply §4.4 and §4.5 documentation edits |
| **Other Hexalith repos** | Validate W5 packages they own; Tenants cannot |

### Success criteria

1. `dotnet restore Hexalith.Tenants.slnx -p:Configuration=Release` -> exit 0 (matching the verified pre-change baseline).
2. Release build with `-warnaserror` -> **0 errors, 0 warnings**.
3. Tier 1 (`Contracts`, `Client`, `Testing`, `UI`, `Sample`) green, per project, matching CI shape.
4. Tier 2 (`Server.Tests`) green after `dapr init`.
5. Coverage gates hold: line >80% over the four package projects; branch 100% on `TenantAggregate.cs`, `GlobalAdministratorsAggregate.cs`, `ChangeUserRoleValidator.cs`.
6. Fluent conformance governance suite green; div+span budget not regressed.
7. `python3 scripts/validate-story-gitlinks.py <story>` passes with every moved pointer declared.
8. No `net11.0`-band package present in the resolved graph.

---

## Appendix A — Change Navigation Checklist

| Item | Status | Finding |
|---|---|---|
| 1.1 Triggering story | **[N/A]** | Not story-derived; direct maintenance request |
| 1.2 Core problem defined | **[x]** | Technical maintenance; F1/F2/F3 in §1.2 |
| 1.3 Evidence gathered | **[x]** | §1.3 — TFM manifests, restore baseline, submodule status |
| 2.1 Current epic completable | **[x]** | Yes, unchanged |
| 2.2 Epic-level changes | **[N/A]** | None |
| 2.3 Remaining epics reviewed | **[x]** | 3.3–3.6, 4.x, 5.x all backlog, unaffected |
| 2.4 Epics invalidated / new needed | **[N/A]** | None |
| 2.5 Epic order or priority | **[N/A]** | Unchanged |
| 3.1 PRD conflicts | **[N/A]** | No FR references a package version |
| 3.2 Architecture conflicts | **[!]** | 5 Fluent pin sites + 1 already-stale baseline block |
| 3.3 UI/UX conflicts | **[!]** | No spec text changes, but Fluent rc.5 requires conformance re-run |
| 3.4 Other artifacts | **[!]** | project-context.md (14 pre-existing drifts), sprint-status.yaml, gitlinks |
| 4.1 Option 1 Direct Adjustment | **[Viable]** | Effort Med–High, Risk Med — **SELECTED** |
| 4.2 Option 2 Rollback | **[Not viable]** | Nothing broken to roll back |
| 4.3 Option 3 MVP review | **[Not viable]** | No scope or goal touched |
| 4.4 Path selected | **[x]** | Option 1, staged W0–W5 |
| 5.1 Issue summary | **[x]** | §1 |
| 5.2 Epic + artifact impact | **[x]** | §2 |

## Appendix B — Method

Versions were resolved live against
`https://api.nuget.org/v3-flatcontainer/{id}/index.json` for all 284 centrally
managed packages, ranked by **SemVer-correct** prerelease ordering (dot-separated
identifiers, numeric segments compared numerically — a first pass using lexical
comparison mis-ranked `beta.556` below `beta.66` and was discarded). Target
framework groups were read from the `registration5-gz-semver2` catalog entry for
each candidate version.

**7 catalog entries returned HTTP 404** and were left untouched:
`Hexalith.Chatbot.Contracts`, `Hexalith.Parties.Server`,
`Hexalith.Parties.ServiceDefaults`, `Hexalith.Parties.UI`, `Hexalith.Tenants.UI`,
`Microsoft.Extensions.Identity.Http`, `Serilog.Sinks.Browser`.
`Hexalith.Tenants.UI` is expected — Tenants publishes five packages and the UI host
is not one of them. The other six are pre-existing catalog entries with no public
feed presence and are worth a separate audit.
