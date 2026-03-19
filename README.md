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
- **Event-Driven Integration** — Subscribe to tenant domain events (`TenantCreated`, `UserAddedToTenant`, etc.) in consuming services via DAPR pub/sub
- **In-Memory Testing Fakes** — Production-parity domain logic with in-memory stores for fast, reliable tests without infrastructure dependencies

## Quickstart

Get from clone to your first tenant in about 15 minutes:

**[Quickstart Guide](docs/quickstart.md)** — ~15 minutes with prerequisites installed, ~45 minutes including first-time prerequisite setup.

<!-- TODO: Story 8.3 — Add "aha moment" demo GIF/link here -->

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
  Hexalith.Tenants.CommandApi/       # REST API, auth, validation, DAPR actors
  Hexalith.Tenants.Contracts/        # Commands, events, enums, identities
  Hexalith.Tenants.Server/           # Aggregates, projections, domain logic
  Hexalith.Tenants.ServiceDefaults/  # Shared service config, OpenTelemetry
  Hexalith.Tenants.Testing/          # In-memory fakes and test helpers

tests/
  Hexalith.Tenants.Client.Tests/
  Hexalith.Tenants.Contracts.Tests/
  Hexalith.Tenants.IntegrationTests/
  Hexalith.Tenants.Server.Tests/
  Hexalith.Tenants.Testing.Tests/

samples/
  Hexalith.Tenants.Sample/           # Example consuming service with event subscription
  Hexalith.Tenants.Sample.Tests/

docs/
  quickstart.md                      # Getting started guide
  idempotent-event-processing.md     # Event handling patterns
```

## Contributing

### Branch Naming

- `feat/<description>` — Features and enhancements
- `fix/<description>` — Bug fixes
- `docs/<description>` — Documentation changes

### Development Workflow

1. Fork and clone with `--recurse-submodules` (the `Hexalith.EventStore` submodule is required)
2. Create a feature branch from `main`
3. Make changes following the code style defined in [`.editorconfig`](.editorconfig)
4. Ensure all tests pass: `dotnet test Hexalith.Tenants.slnx`
5. Submit a pull request against `main`

### Test Requirements

All pull requests must pass the existing test suite. New functionality should include appropriate unit tests. Integration tests require DAPR initialization (`dapr init`).

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
