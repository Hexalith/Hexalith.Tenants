# Hexalith.Tenants

[![CI](https://github.com/Hexalith/Hexalith.Tenants/actions/workflows/ci.yml/badge.svg)](https://github.com/Hexalith/Hexalith.Tenants/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Hexalith.Tenants.Contracts)](https://www.nuget.org/packages/Hexalith.Tenants.Contracts)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Multi-tenant management for the Hexalith ecosystem. Built on event sourcing, DAPR, and .NET Aspire, this service provides a complete tenant lifecycle — from creation and user-role assignment to configuration and cross-tenant discovery — through a command-driven API that publishes domain events for downstream integration.

## Features

- **Tenant Lifecycle Management** — Create, update, enable, and disable tenants through commands that produce auditable domain events
- **User-Role Management** — Add and remove users from tenants with role-based access (Owner, Contributor, Reader) and role behavior enforcement
- **Global Administration** — Bootstrap a global administrator to authorize initial tenant operations
- **Tenant Configuration** — Set and manage per-tenant key-value configuration with domain events for every change
- **Tenant Query and Audit APIs** — Query tenant lists, tenant details, user memberships, and tenant audit history through protected cursor-paginated endpoints
- **Tenants Admin UI Foundation** — Blazor InteractiveServer UI host composed through FrontComposer for read-only tenant triage, detail, memberships, configuration, and support-safe copy
- **Event-Driven Integration** — Subscribe to tenant domain events (`TenantCreated`, `UserAddedToTenant`, etc.) in consuming services via DAPR pub/sub
- **In-Memory Testing Fakes** — Production-parity domain logic with in-memory stores for fast, reliable tests without infrastructure dependencies

## Quickstart

Get from clone to your first tenant command after local prerequisites are installed:

**[Quickstart Guide](docs/quickstart.md)** — prerequisite-validated path for .NET 10, Docker, full DAPR local runtime, AppHost startup, local auth, and the first EventStore command submission.

**[Deployment Readiness](docs/deployment-readiness.md)** — consolidated operator checklist and evidence template for production-like Tenants deployments, covering auth, DAPR, service invocation, health, command/query paths, and pub/sub recovery boundaries.

**[Sample Consuming Service Walkthrough](docs/sample-consuming-service-walkthrough.md)** — source-backed guide for copying the sample service's event subscription, local projection, access-check, and configuration-read patterns.

**[Cross-Aggregate Timing](docs/cross-aggregate-timing.md)** — timing-window and eventual-consistency guidance for command status, event publication, subscriber delivery, local projections, stale reads, diagnostics, and fail-closed consumers.

**[Compensating Commands](docs/compensating-commands.md)** — explicit correction patterns for mistaken access, role, configuration, and lifecycle changes without hidden undo or event mutation.

### See It In Action

Watch reactive cross-service access revocation in action: add a user to a tenant, then remove them — and see the consuming service automatically revoke access via DAPR pub/sub events, with zero custom integration code.

**["Aha Moment" Demo](docs/demo.md)** — Step-by-step walkthrough with automated scripts in [`scripts/`](scripts/).

## NuGet Packages

| Package | Description |
|---------|-------------|
| [`Hexalith.Tenants.Contracts`](https://www.nuget.org/packages/Hexalith.Tenants.Contracts) | Commands, events, enums, and identity types — the shared API surface |
| [`Hexalith.Tenants.Client`](https://www.nuget.org/packages/Hexalith.Tenants.Client) | DI registration, event handlers, and client abstractions for consuming services |
| [`Hexalith.Tenants.Server`](https://www.nuget.org/packages/Hexalith.Tenants.Server) | Aggregates, projections, and domain processing |
| [`Hexalith.Tenants.Testing`](https://www.nuget.org/packages/Hexalith.Tenants.Testing) | In-memory fakes and test helpers with production-parity domain logic |
| [`Hexalith.Tenants.Aspire`](https://www.nuget.org/packages/Hexalith.Tenants.Aspire) | .NET Aspire hosting extensions for consuming AppHosts |

## Project Structure

```text
src/
  Hexalith.Tenants.AppHost/          # .NET Aspire AppHost — orchestrates the full topology
  Hexalith.Tenants.Aspire/           # Aspire hosting extensions for consuming AppHosts
  Hexalith.Tenants.Client/           # Client DI registration and event handling
  Hexalith.Tenants/                  # REST API host, auth, validation, DAPR actors
  Hexalith.Tenants.Contracts/        # Commands, events, enums, identities
  Hexalith.Tenants.Server/           # Aggregates, projections, domain logic
  Hexalith.Tenants.ServiceDefaults/  # Shared service config, OpenTelemetry
  Hexalith.Tenants.Testing/          # In-memory fakes and test helpers
  Hexalith.Tenants.UI/               # Blazor InteractiveServer Tenants Admin UI host

tests/
  Hexalith.Tenants.Client.Tests/
  Hexalith.Tenants.Contracts.Tests/
  Hexalith.Tenants.IntegrationTests/
  Hexalith.Tenants.Server.Tests/
  Hexalith.Tenants.Testing.Tests/
  Hexalith.Tenants.UI.Tests/

samples/
  Hexalith.Tenants.Sample/           # Example consuming service with event subscription
  Hexalith.Tenants.Sample.Tests/

docs/
  quickstart.md                      # Getting started guide
  deployment-readiness.md            # Deployment checklist and evidence template
  production-auth-claim-contract.md  # Production IdP claim mapping contract
  production-auth-readiness.md       # Deployment auth checklist and smoke-test evidence
  demo.md                            # "Aha Moment" demo walkthrough
  event-contract-reference.md        # Event schemas and audit patterns
  compensating-commands.md           # Explicit correction command patterns
  cross-aggregate-timing.md          # Timing windows and eventual consistency
  idempotent-event-processing.md     # Event handling patterns
  sample-consuming-service-walkthrough.md # Sample service adoption walkthrough
  tenants-ui-*.md                    # Phase 2 Admin UI planning, dependency, and evidence specs

scripts/
  demo.ps1                           # PowerShell demo automation
  demo.sh                            # Bash demo automation
```

## Contributing

### Branch Naming

- `feat/<description>` — Features and enhancements
- `fix/<description>` — Bug fixes
- `docs/<description>` — Documentation changes

### Development Workflow

1. Fork and clone, then initialize root-declared submodules under `references/`: `git submodule update --init references/Hexalith.EventStore references/Hexalith.Commons references/Hexalith.AI.Tools references/Hexalith.Builds references/Hexalith.FrontComposer references/Hexalith.PolymorphicSerializations references/Hexalith.Memories`
2. Create a feature branch from `main`
3. Make changes following the code style defined in [`.editorconfig`](.editorconfig)
4. Build the solution with `dotnet build Hexalith.Tenants.slnx -c Release -warnaserror`
5. Run test projects individually; do not use solution-level `dotnet test`
6. Submit a pull request against `main`

`Hexalith.Tenants.slnx` remains the canonical development and topology solution.
Dependency governance builds `Hexalith.Tenants.Standalone.slnx` in Release package
mode (`-p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false`); that solution contains all 17 owned projects and no
project or file entries under `references/`.

### Test Requirements

All pull requests must pass the relevant existing test projects. Run test projects individually. With the current .NET 10 SDK, `dotnet test` can hit the Microsoft.Testing.Platform/VSTest incompatibility recorded in the Epic 2 story evidence; when that happens, build the test project and run its generated xUnit v3 executable from `bin/Release/net10.0`.

```bash
dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Release
dotnet test tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj -c Release
dotnet test tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj -c Release
dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release

dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore
tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none
```

Integration and server tests require DAPR initialization (`dapr init`) and the local runtime prerequisites documented in the quickstart.

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
