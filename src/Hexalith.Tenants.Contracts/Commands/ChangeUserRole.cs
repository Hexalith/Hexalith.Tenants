using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.Contracts.Commands;

public record ChangeUserRole(string TenantId, string UserId, TenantRole NewRole);
