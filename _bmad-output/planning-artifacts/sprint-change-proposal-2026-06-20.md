# Sprint Change Proposal - DAPR Baseline Availability

Date: 2026-06-20
Project: tenants
Workflow: bmad-correct-course
Mode: Batch
Status: SUPERSEDED (2026-06-20) by `sprint-change-proposal-2026-06-20-eventstore-test-harness-extraction.md`
— the fixture/port-resolution portion is absorbed into that relocation (the port fix lands once in the
EventStore platform via `DaprLocalEndpoints`); the documentation portion is carried forward there as
Workstream C and remains valid for execution.

## 1. Issue Summary

The sprint assumption has changed: full `dapr init` is always completed before Tenants development, CI, or integration-test execution begins, so DAPR runtime services are treated as available baseline infrastructure.

The immediate implementation issue is not that DAPR is absent. The fixtures assumed Linux hosts expose placement and scheduler on the container-internal ports `50005` and `50006`. Modern Docker-based `dapr init` publishes those services on host ports `6050` and `6060` on Linux/WSL2 as well as Windows. The OS-based port guess can therefore misclassify an initialized DAPR runtime as unavailable and skip DAPR-backed tests.

There is also stale documentation and evidence language that presents DAPR initialization as a normal repo-local step or routine skip condition. That should be reframed around baseline availability and verification.

Evidence found:

- `docs/quickstart.md` tells users to run `dapr init` in prerequisites and troubleshooting.
- `README.md`, `CONTRIBUTING.md`, and `deploy/dapr/README.md` describe DAPR initialization as a local prerequisite step.
- `.github/workflows/ci.yml` and `.github/workflows/release.yml` already run `dapr init` before DAPR-backed lanes, which supports the new assumption for ephemeral CI runners.
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/DaprFactAttribute.cs`, `TenantsDaprTestFixture.cs`, and `AspireTopologyFixture.cs` used OS-based placement/scheduler ports.
- Current worktree changes add `tests/Hexalith.Tenants.IntegrationTests/Fixtures/DaprLocalEndpoints.cs`, which resolves `6050/6060` first and falls back to `50005/50006`, with environment-variable overrides.
- Historical implementation evidence records DAPR-prerequisite skips as expected environment behavior.

## 2. Impact Analysis

### Epic Impact

No PRD feature scope changes are needed. The PRD, epics, architecture, and UX artifacts define tenant-management behavior and remain valid.

Affected epic/stories by operational assumption:

- Epic 1 through Epic 5: no acceptance criteria changes to product behavior.
- Integration and hosted smoke tests attached to UI stories: use resolved DAPR host endpoints instead of OS guesses.
- Test harness and CI documentation: update from "DAPR may be missing" to "DAPR baseline is initialized; probes verify the initialized services are reachable."

### Story Impact

Existing completed story files that mention "DAPR prerequisites unavailable" are historical evidence. Do not rewrite old records unless the team wants archival cleanup. New story records must not classify missing DAPR as a routine skip for DAPR-backed lanes.

Current and future implementation stories should use this rule:

```text
DAPR baseline rule: full DAPR runtime initialization is completed before Tenants work starts. DAPR-backed tests must probe the actual host ports exposed by the initialized runtime, including modern Docker-based `dapr init` ports `6050`/`6060` and legacy slim-mode ports `50005`/`50006`.
```

### Artifact Conflicts

Primary conflicts:

- Setup docs still instruct repo-local `dapr init`.
- Deployment readiness still has "prerequisite missing" pass/skip vocabulary.
- Integration-test attributes and fixtures used OS-based placement/scheduler ports that can be wrong on Linux/WSL2 with Docker-based `dapr init`.
- Documentation tests assert old `dapr init` wording.
- Project context says Tier 2 "needs `dapr init`" and CI runs `dapr init`; this should be reframed as "CI bootstrap ensures DAPR is initialized before Tier 2."

Non-conflicts:

- CI `Initialize Dapr` steps can remain as the mechanism that satisfies the baseline on ephemeral runners.
- Production deployment docs may still document required DAPR controls; the change is about verifying those controls accurately.
- Performance tests may remain opt-in by `HEXALITH_TENANTS_RUN_PERFORMANCE_TESTS`.

### Technical Impact

Expected implementation touches:

- Docs: `README.md`, `CONTRIBUTING.md`, `docs/quickstart.md`, `docs/demo.md`, `docs/deployment-readiness.md`, `deploy/dapr/README.md`.
- Tests: DAPR local endpoint resolution, documentation assertion tests, and DAPR/Aspire fixture diagnostics.
- Optional generated/context artifacts: `_bmad-output/project-context.md` and future story templates or test summaries if the team keeps these synchronized.

No backend domain behavior, commands, events, projections, or UI feature contracts should change.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Rationale:

- The change is an operational baseline correction, not a product replan.
- Existing CI already initializes DAPR before DAPR-backed lanes, so the main correction is accurate readiness detection.
- Centralized endpoint probing removes false DAPR-prerequisite skips without changing domain behavior.
- No rollback or MVP review is justified.

Effort estimate: Low to Medium.

Risk level: Low to Medium. The port-resolution fix is low risk and directly addresses false negatives. Changing skip gates into hard failures is a separate medium-risk hardening choice and should not block the endpoint correction.

## 4. Detailed Change Proposals

### Documentation: README test requirements

File: `README.md`
Section: Test Requirements

OLD:

```markdown
Integration and server tests require DAPR initialization (`dapr init`) and the local runtime prerequisites documented in the quickstart.
```

NEW:

```markdown
Integration and server tests assume the full DAPR runtime has already been initialized and is reachable before the test lane starts. Missing Redis, placement, scheduler, or DAPR sidecar readiness is an environment failure, not a routine skip condition.
```

Rationale: Aligns contributor guidance with the new baseline.

### Documentation: CONTRIBUTING prerequisites

File: `CONTRIBUTING.md`
Section: Prerequisites

OLD:

```markdown
- **DAPR CLI + Runtime** - [Getting Started](https://docs.dapr.io/getting-started/). Run `dapr init` (**full init, NOT `--slim`** - the Aspire topology requires the full DAPR runtime with placement service for actors). Verify: `dapr --version`
```

NEW:

```markdown
- **DAPR CLI + Runtime** - [Getting Started](https://docs.dapr.io/getting-started/). Supported Tenants environments already have full DAPR runtime initialization completed before repository work starts. Verify with `dapr --version` and the expected Redis, placement, and scheduler probes; missing DAPR runtime services are environment failures.
```

Rationale: Moves initialization out of per-repo task flow and keeps verification.

### Documentation: Quickstart DAPR section

File: `docs/quickstart.md`
Section: DAPR CLI and Runtime

OLD:

```markdown
```bash
dapr init
```

> **Note:** Run `dapr init` (full init, not `--slim`) for this local quickstart. Full init provides Redis, actor placement, and scheduler. `dapr init --slim` excludes those local services; use slim mode only when you provide placement, scheduler, `statestore`, and `pubsub` separately.
```

NEW:

```markdown
The Tenants quickstart assumes full DAPR runtime initialization has already been completed before you start repository work. Verify that the CLI reports a runtime version and that Redis, placement, and scheduler are reachable on the expected local ports. Do not treat missing DAPR runtime services as an expected quickstart branch; fix the environment before continuing.
```

Rationale: Keeps quickstart focused on verification and AppHost flow.

### Documentation: Quickstart troubleshooting

File: `docs/quickstart.md`
Section: Troubleshooting

OLD:

```markdown
**DAPR not initialized**

If you see DAPR-related errors, ensure you've run the full initialization:

```bash
dapr init
```

Use `dapr init` (not `--slim`) - the Aspire topology requires Redis, placement, and scheduler.
```

NEW:

```markdown
**DAPR runtime unavailable**

If you see DAPR-related errors, treat the missing Redis, placement, scheduler, or sidecar readiness as an environment failure. The Tenants quickstart assumes full DAPR initialization was already completed before this repository workflow starts. Verify the expected ports and sidecar health, then fix the shared environment before retrying.
```

Rationale: Avoids instructing normal users to re-run initialization during repo work.

### Documentation: Deployment readiness

File: `docs/deployment-readiness.md`
Section: DAPR Components

OLD:

```markdown
If live DAPR prerequisites are missing, classify the evidence as `environment-blocker` or `not-claimable` for the live row. Do not convert prerequisite skips into passing deployment proof.
```

NEW:

```markdown
If required DAPR controls are missing or unreachable, classify the evidence as `environment-blocker` or `not-claimable` for the live row. DAPR availability is a mandatory baseline for live proof; do not convert DAPR skips or unavailable-prerequisite probes into passing deployment proof.
```

Rationale: Keeps production evidence strict while removing optional-prerequisite framing.

### Documentation: DAPR deployment templates

File: `deploy/dapr/README.md`
Section: Local Development Mode

OLD:

```markdown
Normal local development should run full `dapr init` before the Aspire AppHost starts.
```

NEW:

```markdown
Normal local development assumes full DAPR initialization has already been completed before the Aspire AppHost starts.
```

Rationale: Matches the new "always done" baseline.

### Tests: DAPR local endpoint resolution

Files:

- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/DaprLocalEndpoints.cs`
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/DaprFactAttribute.cs`
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs`
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs`

Proposal:

- Add a shared `DaprLocalEndpoints` helper that probes placement host ports in this order: `6050`, then `50005`.
- Probe scheduler host ports in this order: `6060`, then `50006`.
- Allow explicit overrides through `HEXALITH_TENANTS_TEST_PLACEMENT_PORT` and `HEXALITH_TENANTS_TEST_SCHEDULER_PORT`.
- Use the helper everywhere the integration fixtures need placement or scheduler ports.

OLD:

```csharp
private static readonly int PlacementPort = OperatingSystem.IsWindows() ? 6050 : 50005;
private static readonly int SchedulerPort = OperatingSystem.IsWindows() ? 6060 : 50006;
```

NEW:

```csharp
private static readonly int PlacementPort = DaprLocalEndpoints.PlacementPort;
private static readonly int SchedulerPort = DaprLocalEndpoints.SchedulerPort;
```

Rationale: `dapr init` can be complete and healthy while the old Linux/WSL2 probe checks the wrong host ports. Endpoint resolution must follow actual DAPR exposure, not OS.

### Tests: optional skip-to-fail hardening

Files:

- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/DaprFactAttribute.cs`
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs`
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs`

Proposal:

- After the endpoint-resolution fix, decide whether DAPR-backed lanes should continue to use `Assert.Skip` when all candidate endpoints are unavailable, or fail fast as a broken baseline.
- Recommended immediate scope: fix endpoint detection first, keep support-safe diagnostics, and measure whether false skips disappear.
- Recommended follow-up if desired: convert required DAPR lanes from skip to hard failure once CI/local evidence confirms the probes are stable.

Rationale: The trigger says DAPR is available; the discovered defect is a false negative. Changing skip semantics is defensible, but it is a broader test-policy change than the endpoint bug fix.

OLD:

```csharp
PrerequisitesAvailable = false;
SkipReason = BuildPrerequisiteFailureMessage(prerequisiteFailures);
return;
```

FOLLOW-UP NEW, if hard-fail policy is approved:

```csharp
throw new InvalidOperationException(BuildPrerequisiteFailureMessage(prerequisiteFailures));
```

OLD:

```csharp
Assert.Skip(SkipReason ?? DaprTestPrerequisites.SkipReason);
```

NEW:

```csharp
throw new InvalidOperationException(SkipReason ?? DaprTestPrerequisites.SkipReason);
```

### Tests: diagnostics wording

File: `tests/Hexalith.Tenants.IntegrationTests/Fixtures/DaprTestPrerequisiteDiagnosticsTests.cs`

Optional follow-up OLD:

```csharp
reason.ShouldContain("DAPR integration prerequisites are unavailable");
reason.ShouldContain("dapr init");
```

Optional follow-up NEW:

```csharp
reason.ShouldContain("DAPR integration baseline is unavailable");
reason.ShouldContain("Redis");
reason.ShouldContain("placement");
reason.ShouldContain("scheduler");
```

Rationale: Diagnostic tests should still verify support-safe messages. If skip-to-fail hardening is deferred, keep the current support-safe wording but update port expectations to use `DaprLocalEndpoints`.

### Tests: documentation assertions

Files:

- `tests/Hexalith.Tenants.Server.Tests/Documentation/QuickstartDocumentationTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Documentation/DeploymentReadinessDocumentationTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs`

Proposal:

- Replace assertions that require repo-local `dapr init` instructions with assertions that require DAPR baseline verification language.
- Preserve assertions for Redis, placement, scheduler, `statestore`, and `pubsub`.
- Preserve support-safety assertions.

Rationale: Keeps documentation coverage but updates the required contract.

### CI: keep bootstrap, clarify meaning

Files:

- `.github/workflows/ci.yml`
- `.github/workflows/release.yml`

Proposal:

- Keep `dapr/setup-dapr` and retry-wrapped `dapr init` in ephemeral GitHub-hosted runner jobs unless the runner image is proven to be pre-initialized.
- Rename or comment the step as CI environment bootstrap that satisfies the DAPR baseline before Tier 2/Tier 3 lanes.
- Do not let DAPR-backed tests silently skip after this bootstrap.

Rationale: CI is the mechanism proving the baseline on fresh runners; removing it would contradict "always available" in GitHub Actions.

### Generated/context artifacts

File: `_bmad-output/project-context.md`

OLD:

```markdown
**Tier 2 - `Server.Tests`** (needs `dapr init`, **blocking**)
```

NEW:

```markdown
**Tier 2 - `Server.Tests`** (DAPR baseline already initialized and reachable, **blocking**)
```

OLD:

```markdown
Tier 1, `dapr init`, Tier 2, coverage gates
```

NEW:

```markdown
Tier 1, CI DAPR bootstrap, Tier 2, coverage gates
```

Rationale: Keeps the agent context from reintroducing optional-DAPR assumptions.

## 5. Checklist Results

- [x] 1.1 Triggering story identified: no single story; cross-cutting operational/test assumption discovered during DAPR evidence review.
- [x] 1.2 Core problem defined: misunderstanding of environment baseline.
- [x] 1.3 Evidence gathered: setup docs, CI workflows, integration fixtures, documentation tests, and historical story evidence.
- [x] 2.1 Current epic assessment: product epics remain valid.
- [x] 2.2 Epic-level changes: no new epic required.
- [x] 2.3 Remaining planned epics: no product dependency changes.
- [x] 2.4 Future epic invalidation: none.
- [x] 2.5 Epic priority/order: unchanged.
- [x] 3.1 PRD conflicts: none requiring PRD edits.
- [x] 3.2 Architecture conflicts: no architecture decision change; BFF/DAPR service invocation remains valid.
- [x] 3.3 UX conflicts: none.
- [x] 3.4 Other artifacts: docs, test fixtures, CI wording, and generated project context require updates.
- [x] 4.1 Direct Adjustment: viable, low-medium effort, medium risk.
- [x] 4.2 Rollback: not viable; no completed feature work needs rollback.
- [x] 4.3 PRD MVP Review: not viable; MVP scope unchanged.
- [x] 4.4 Recommended path: Direct Adjustment.
- [x] 5.1 Issue summary: included.
- [x] 5.2 Impact and artifact adjustment needs: included.
- [x] 5.3 Recommendation: included.
- [x] 5.4 MVP impact/action plan: MVP unchanged; action plan included.
- [x] 5.5 Handoff plan: below.
- [x] 6.1 Checklist completion: complete, pending approval.
- [x] 6.2 Proposal accuracy: reviewed against discovered files.
- [!] 6.3 User approval: pending.
- [N/A] 6.4 Sprint status update: no epic/story additions, removals, or reordering proposed.
- [!] 6.5 Next steps and handoff: pending user approval.

## 6. Implementation Handoff

Scope classification: Minor.

Route to: Developer agent for direct implementation after approval.

Implementation tasks:

1. Update docs and documentation tests to state DAPR baseline availability and verification, not repo-local initialization as a normal branch.
2. Update DAPR/Aspire integration fixtures to use `DaprLocalEndpoints` for placement and scheduler port resolution.
3. Keep performance tests opt-in; after endpoint detection is fixed, decide separately whether unavailable DAPR should remain a skip or become a hard failure.
4. Preserve CI DAPR bootstrap on ephemeral runners unless pre-initialized runners are introduced.
5. Run focused docs/test validation:
   - `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Release --filter "FullyQualifiedName~QuickstartDocumentationTests|FullyQualifiedName~DeploymentReadinessDocumentationTests|FullyQualifiedName~EventPublicationConfigurationTests"`
   - `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DaprTestPrerequisiteDiagnosticsTests|FullyQualifiedName~DaprEndToEndTests|FullyQualifiedName~GracefulDegradationTests|FullyQualifiedName~StatelessRestartTests"`

Success criteria:

- No current docs instruct ordinary Tenants repo workflows to run `dapr init` as a step.
- DAPR runtime availability is documented as a baseline with verification probes.
- DAPR-backed tests do not falsely skip when DAPR is initialized and reachable through modern Docker-based host ports.
- CI still provides DAPR bootstrap before DAPR-backed lanes, or an equivalent pre-initialized runner contract is documented.
- Integration fixtures probe modern Docker-based `dapr init` host ports and legacy slim-mode ports before declaring DAPR unavailable.
- Support-safe diagnostic guarantees remain intact.

## 7. Approval Request

Approve this Sprint Change Proposal for implementation?

Options:

- `yes` - route to Developer agent for direct implementation.
- `revise` - adjust proposed edits before implementation.
- `no` - stop this correction.
