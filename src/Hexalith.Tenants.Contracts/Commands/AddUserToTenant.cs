using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.Contracts.Commands;

public record AddUserToTenant(string TenantId, string UserId, TenantRole Role);
