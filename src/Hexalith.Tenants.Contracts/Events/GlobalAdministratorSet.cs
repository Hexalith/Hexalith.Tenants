namespace Hexalith.Tenants.Contracts.Events;

public record GlobalAdministratorSet(string TenantId, string UserId) : IEventPayload;
