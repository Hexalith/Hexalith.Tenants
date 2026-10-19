using Hexalith.McpCli.Abstractions;
using Hexalith.Tenants.Contracts.Identity;

// Declares this Contracts assembly as the McpCli "tenants" module. Every operation is submitted under the
// fixed platform tenant "system"; managed tenants are aggregates, never envelope tenants. Payload
// identifiers are opaque strings.
[assembly: HexalithModule(
    "tenants",
    "Manage tenants: their lifecycle, user memberships and roles, configuration settings, and audit trail.",
    IdentifierKind.String,
    FixedTenant = TenantIdentity.DefaultTenantId)]
