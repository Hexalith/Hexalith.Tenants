namespace Hexalith.Tenants.Contracts.Events;

public record GlobalAdministratorRemoved(string TenantId, string UserId) : IEventPayload;
