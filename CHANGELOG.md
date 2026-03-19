# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0] - YYYY-MM-DD

### Added

- Tenant lifecycle management (Create, Update, Disable, Enable) via event-sourced TenantAggregate
- User-role management with three roles (TenantOwner, TenantContributor, TenantReader)
- Global administrator management with bootstrap mechanism
- Tenant key-value configuration with namespace conventions and limits
- Event-driven integration via DAPR pub/sub (CloudEvents 1.0)
- Tenant discovery and query endpoints with cursor-based pagination
- In-memory testing fakes with production-parity domain logic
- .NET Aspire hosting extensions and AppHost topology
- OpenTelemetry instrumentation for command and event processing
- Comprehensive documentation: quickstart guide, event contract reference, cross-aggregate timing, compensating commands
- CI/CD pipeline with GitHub Actions (build, test, NuGet publish)
- Sample consuming service demonstrating event subscription and access enforcement

[Unreleased]: https://github.com/Hexalith/Hexalith.Tenants/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/Hexalith/Hexalith.Tenants/releases/tag/v0.1.0
