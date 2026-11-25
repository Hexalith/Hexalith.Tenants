namespace Hexalith.Tenants.Contracts.Events.Rejections;

/// <summary>Rejects a synthetic tenant command addressed to an invalid tenant identifier.</summary>
/// <param name="TenantId">The supplied aggregate identifier.</param>
public record SyntheticTenantIdInvalidRejection(string TenantId) : IRejectionEvent;
