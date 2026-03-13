namespace Hexalith.Tenants.Contracts.Events.Rejections;

public record UserAlreadyInTenantRejection(string TenantId, string UserId) : IRejectionEvent;
