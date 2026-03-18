using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.Server.Projections;

public sealed record TenantIndexEntry(string Name, TenantStatus Status);
