---
stepsCompleted: [1, 2, 3, 4, 5]
inputDocuments:
  - Hexalith.EventStore/README.md
  - Hexalith.EventStore/docs/concepts/architecture-overview.md
  - Hexalith.EventStore/docs/concepts/command-lifecycle.md
date: 2026-03-06
author: Jerome
---

# Product Brief: Hexalith.Tenants

## Executive Summary

Hexalith.Tenants is a standalone, event-sourced microservice that provides provable multi-tenant management for applications built on Hexalith.EventStore. It bridges the gap between identity providers (Keycloak) and domain-level authorization — a layer no existing tool provides for event-sourced architectures. Built as a first-class Hexalith domain service using DDD, CQRS, and event sourcing patterns, it delivers tenant lifecycle management, tenant-scoped RBAC with complete audit history, tenant-specific configuration, and reactive event-driven integration — all through the same DAPR-native, infrastructure-portable pipeline that Hexalith.EventStore provides.

---

## Core Vision

### Problem Statement

Every application built on Hexalith.EventStore requires tenant management and user access control, yet no existing solution integrates natively with the event-sourced, DAPR-native architecture. Developers are forced to rebuild tenant management from scratch for each new project. The deeper issue is not just duplicated effort — it's that tenant-user-role relationships are domain state that must participate in the event-sourced lifecycle. CRUD-based tenant tables cannot produce domain events, cannot provide temporal audit queries, and cannot enable reactive cross-service integration.

### Problem Impact

- **Wasted development time** — teams repeatedly build the same tenant plumbing (2-4 weeks per project) instead of focusing on their core domain logic
- **Inconsistent security** — ad-hoc access control implementations are prone to errors that can lead to cross-tenant data leaks and role escalation vulnerabilities
- **No reactive integration** — without tenant events flowing through the event store, other services cannot react to tenant changes (user removed, tenant disabled, config changed) in real-time
- **No audit trail** — CRUD-based approaches cannot answer temporal queries like "who had access to this tenant last Tuesday?"
- **Onboarding friction** — consuming services cannot bootstrap tenant-specific data without rich tenant lifecycle events

### Why Existing Solutions Fall Short

Existing multi-tenant solutions in the .NET ecosystem (identity providers, CRUD-based tenant libraries) solve authentication but not domain-level authorization. They cannot produce domain events that flow through the Hexalith.EventStore pipeline. They cannot provide provable audit history through event sourcing. And they cannot enable the reactive, event-driven model that Hexalith applications depend on — where a role change in the tenant service automatically triggers access revocation in downstream services.

### Proposed Solution

A standalone microservice that provides:

- **Tenant aggregate** — manages tenant lifecycle (create, update, disable/suspend) with full event history. Tenant status integrates with EventStore's command pipeline — a disabled tenant causes commands to be rejected before reaching any domain service
- **Tenant-scoped RBAC** — role assignments (TenantOwner, TenantContributor, TenantReader) managed as domain events within the tenant aggregate, providing complete audit trail and cross-service reactivity. Role escalation boundaries enforced at the domain level (TenantOwner cannot self-escalate to GlobalAdministrator)
- **Global administrator** — a cross-tenant role for platform-wide access that bypasses tenant-scoped checks but still produces auditable events
- **Tenant configuration** — key-value settings per tenant (feature flags, branding, limits) with configuration change events, enabling consuming services to react without polling
- **Hybrid authentication model** — Keycloak handles authentication and token issuance; the tenant aggregate owns tenant-scoped role assignments as the source of truth, with projection sync to JWT claims. Services can fall back to direct tenant service queries when projection sync lags
- **Event-driven integration** — all tenant changes published via DAPR pub/sub as rich domain events, enabling consuming services to bootstrap tenant data, enforce access, and react to lifecycle changes

### Key Differentiators

- **Bridge between identity and domain** — fills the gap no existing tool covers: domain-level authorization with event-sourced audit for multi-tenant architectures
- **Provable tenant isolation** — complete, immutable event history of every access change, answerable to temporal queries
- **Native Hexalith.EventStore integration** — follows the same DDD/CQRS/ES patterns, not a bolted-on CRUD module
- **Reactive tenant lifecycle** — rich domain events enable consuming services to react in real-time to tenant changes
- **Drop-in microservice** — consuming applications subscribe to tenant events and call the API, zero custom tenant code needed. Inherits DAPR-native infrastructure portability with zero database lock-in, and tenant data is inherently portable through event replay across deployments

## Target Users

### Primary Users

#### 1. The SaaS Developer — "Alex"
**Role:** .NET developer building multi-tenant applications on Hexalith.EventStore
**Context:** Works on a team delivering SaaS products. Every new project requires tenant management, and they've built it from scratch at least twice before.

**Problem Experience:**
- Spends 2-4 weeks per project wiring up tenant management, user-role assignments, and access control
- Copy-pastes tenant code between projects, each version slightly different and inconsistently secured
- Has no standard way to react to tenant lifecycle events across services
- Integration testing tenant behavior requires spinning up custom infrastructure every time

**Success Vision:**
- Adds the Hexalith.Tenants NuGet packages, deploys the tenant microservice, and gets tenant management out of the box
- Subscribes to tenant events in their domain services to react to user/role/config changes
- Uses `Hexalith.Tenants.Testing` for fast, in-memory integration tests without a live tenant service
- Never writes tenant management code again

**Key Interactions:** NuGet packages (especially Contracts and Testing), REST/gRPC API, DAPR pub/sub event subscriptions, tenant event contract documentation

#### 2. The Platform Operator — "Priya"
**Role:** DevOps/platform engineer responsible for deploying and operating Hexalith-based applications
**Context:** Manages multi-tenant SaaS infrastructure. Needs visibility into tenant state, provisioning workflows, and operational health.

**Problem Experience:**
- Each application has its own tenant storage and management, making it hard to get a unified view
- Tenant provisioning is manual or custom-scripted per application
- No centralized audit trail for tenant access changes

**Success Vision:**
- Deploys Hexalith.Tenants as a shared microservice across all applications
- Has a single source of truth for tenant state, user assignments, and configuration
- Can monitor tenant lifecycle events and set up alerts for suspicious activity

**Key Interactions:** Deployment configuration (DAPR components, Aspire), monitoring dashboards, tenant provisioning commands, event stream inspection

### Secondary Users

#### 3. The Global Administrator — "Sofia"
**Role:** Platform-wide administrator with cross-tenant access
**Context:** Responsible for platform governance, security oversight, and cross-tenant operations.

**Problem Experience:**
- Must log into individual tenant contexts to troubleshoot or audit
- No unified view of access patterns across tenants
- Disabling a tenant with active sessions and in-flight commands is risky without clear lifecycle semantics
- Bulk operations (provisioning, suspension) require custom scripts per application

**Success Vision:**
- Can access any tenant without per-tenant role assignments
- All actions are still fully audited despite elevated access
- Can disable/suspend tenants with well-defined handling of active sessions and in-flight commands
- Has a single operational view across all tenants for lifecycle management

**Key Interactions:** Platform admin tooling, audit event streams, tenant lifecycle commands (DisableTenant, EnableTenant)

### Key Stakeholders (Inform Requirements, Not Direct Users)

#### Tenant Administrators (e.g., "Marc")
TenantOwners who manage their tenant's users and settings through consuming application UIs. They never interact with Hexalith.Tenants directly — their experience is shaped by the consuming app developer. Their needs inform the command design (AddUserToTenant, SetTenantConfiguration) and the richness of tenant events.

#### Security Auditors (e.g., "Kenji")
Auditors reviewing access controls and compliance. They consume read-model projections and event history built by developers, not the tenant service itself. Their needs drive the event schema design — every role change, config change, and admin action must be an immutable, queryable event with full context.

### User Journey

**Alex (SaaS Developer) — Primary Journey:**
1. **Discovery:** Finds Hexalith.Tenants in the Hexalith documentation or NuGet while building a new EventStore-based application
2. **Onboarding:** Adds the NuGet packages, deploys the tenant microservice alongside their EventStore deployment, follows the quickstart guide
3. **Core Usage:** Sends tenant commands (CreateTenant, AddUserToTenant, SetTenantConfiguration) via the API; subscribes to tenant events in their domain services to enforce access and react to changes
4. **Testing:** Uses `Hexalith.Tenants.Testing` package with in-memory fakes for integration tests — no live tenant service needed during development
5. **Aha Moment:** Realizes that tenant user removal automatically triggers access revocation in their downstream services through event subscriptions — no custom integration code needed
6. **Long-term:** Hexalith.Tenants becomes standard infrastructure in every new project, never builds tenant management again

## Success Metrics

### User Success Metrics

- **Adoption rate** — NuGet download count for Hexalith.Tenants.Contracts and Hexalith.Tenants.Client packages, tracked monthly
- **Time-to-first-tenant-command** — a developer can send their first CreateTenant command within 30 minutes of adding the NuGet packages, following the quickstart guide
- **Integration simplicity** — consuming services can subscribe to tenant events and enforce access with under 20 lines of boilerplate code
- **Testing experience** — developers can write integration tests using Hexalith.Tenants.Testing without any external infrastructure dependency

### Business Objectives

- **Ecosystem growth** — Hexalith.Tenants removes a key friction point (tenant management) that currently discourages adoption of Hexalith.EventStore for multi-tenant scenarios. Success means more projects choosing EventStore because tenant management is solved
- **Elimination of duplicated effort** — teams no longer spend 2-4 weeks per project building custom tenant management. Each new project starts with tenant infrastructure ready on day one
- **Community traction** — GitHub stars, community contributions, and discussions around tenant-related use cases grow alongside EventStore adoption

### Key Performance Indicators

| KPI | Target | Measurement |
|-----|--------|-------------|
| NuGet downloads (Contracts) | Growing month-over-month | NuGet.org statistics |
| Quickstart completion rate | < 30 minutes to first command | Quickstart guide validation |
| Cross-tenant isolation | Zero cross-tenant data leaks | Automated integration tests (Tier 3) |
| Event processing latency | < 50ms for tenant commands (p95) | OpenTelemetry metrics |
| Test coverage | > 80% line coverage across all packages | coverlet.collector in CI |
| Repeat adoption rate | >50% of new EventStore projects also adopt Tenants | NuGet co-install correlation |
| Tenant event contract stability | Zero breaking changes after v1.0 | CI contract tests |

## MVP Scope

### Core Features

#### 1. Tenant Aggregate
- **Commands:** CreateTenant, UpdateTenant, DisableTenant, EnableTenant
- **Events:** TenantCreated, TenantUpdated, TenantDisabled, TenantEnabled
- **State:** TenantId, Name, Status (Active/Disabled), creation metadata
- Follows Hexalith.EventStore aggregate patterns (Handle/Apply, pure functions)

#### 2. User-Role Management
- **Commands:** AddUserToTenant, RemoveUserFromTenant, ChangeUserRole
- **Events:** UserAddedToTenant, UserRemovedFromTenant, UserRoleChanged
- **Roles:** TenantOwner, TenantContributor, TenantReader
- Role escalation boundaries enforced at the domain level (no self-escalation to GlobalAdministrator)

#### 3. Global Administrator
- **Commands:** SetGlobalAdministrator, RemoveGlobalAdministrator
- **Events:** GlobalAdministratorSet, GlobalAdministratorRemoved
- Bypasses tenant-scoped access checks but all actions remain auditable

#### 4. Tenant Configuration
- **Commands:** SetTenantConfiguration, RemoveTenantConfiguration
- **Events:** TenantConfigurationSet, TenantConfigurationRemoved
- Key-value pairs per tenant, schema-free (each consuming service reads only its relevant keys)

#### 5. NuGet Packages
- **Hexalith.Tenants.Contracts** — commands, events, result types, identities
- **Hexalith.Tenants.Client** — client abstractions and DI registration for consuming services
- **Hexalith.Tenants.Server** — domain service with aggregate, processors, and DAPR integration
- **Hexalith.Tenants.Testing** — in-memory fakes for integration testing without live infrastructure
- **Hexalith.Tenants.Aspire** — .NET Aspire hosting extensions

#### 6. Event-Driven Integration
- All tenant events published via DAPR pub/sub as CloudEvents 1.0
- Tenant-isolated topic naming (e.g., `{tenant}.tenants.events`)
- Consuming services subscribe to react to tenant lifecycle changes

#### 7. Documentation & Quickstart
- Quickstart guide: first CreateTenant command in under 30 minutes
- Event contract reference documentation
- Sample consuming service showing event subscription

### Out of Scope for MVP

- **Keycloak JWT projection sync** — v2: projection that synchronizes tenant roles to JWT claims
- **Admin UI / dashboard** — v2: visual management interface for tenants and users
- **Bulk tenant provisioning** — v2: batch import/migration tooling for onboarding at scale
- **Custom/extensible roles** — v2: tenant-defined roles beyond Owner/Contributor/Reader
- **Hierarchical sub-tenants** — v2: parent-child tenant relationships
- **Tenant migration tooling** — v2: cross-deployment tenant replay utilities
- **EventStore tenant authorization plugin** — v2: `Hexalith.Tenants.EventStore` NuGet package providing a MediatR `TenantAuthorizationBehavior` that filters commands based on tenant roles. Deployment-level opt-in via `services.AddTenantAuthorization()`. Uses a local projection of tenant-user-role mappings (subscribed via DAPR pub/sub) for fast authorization without network hops. Requires TenantOwner, TenantContributor, or GlobalAdministrator to execute commands. TenantReader excluded from write path. Also rejects commands for disabled tenants

### MVP Success Criteria

- A developer can deploy Hexalith.Tenants and send a CreateTenant command within 30 minutes
- All tenant commands produce correct domain events, published via DAPR pub/sub
- A consuming service can subscribe to tenant events and enforce access control based on UserAddedToTenant/UserRemovedFromTenant events
- Zero cross-tenant data leaks verified by Tier 3 integration tests
- Hexalith.Tenants.Testing enables integration tests without live infrastructure
- All packages build, pass tests, and publish to NuGet.org via CI/CD

### Future Vision

- **Identity integration layer** — Keycloak projection sync, JWT claim enrichment, and potentially other identity providers
- **Platform management** — Admin dashboard for tenant provisioning, monitoring, and configuration management at scale
- **EventStore tenant authorization plugin** — `Hexalith.Tenants.EventStore` NuGet package, deployment-level opt-in via `services.AddTenantAuthorization()`. Registers a `TenantAuthorizationBehavior` in the MediatR pipeline (after JWT auth, before validation) that enforces tenant-scoped RBAC on all commands. Uses a local projection subscribed to tenant events via DAPR pub/sub for fast, no-network-hop authorization. Authorization logic: user must be GlobalAdministrator, TenantOwner, or TenantContributor to execute commands. TenantReader is excluded from the write path. Commands targeting disabled tenants are auto-rejected. V1 event contracts (UserAddedToTenant, UserRemovedFromTenant, UserRoleChanged, GlobalAdministratorSet/Removed, TenantDisabled/Enabled) are designed to carry all fields needed for this projection
- **Extensible authorization** — custom roles, permission sets, and fine-grained access control per tenant
- **Multi-deployment** — tenant migration and replication across EventStore deployments via event replay
- **Service enablement** — per-tenant service registry controlling which domain services are available to which tenants
