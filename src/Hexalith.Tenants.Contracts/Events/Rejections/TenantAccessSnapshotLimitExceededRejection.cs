namespace Hexalith.Tenants.Contracts.Events.Rejections;

/// <summary>Rejects a recovery atomically when a complete snapshot exceeds its supported budget.</summary>
/// <param name="TenantId">The managed tenant identifier.</param>
/// <param name="Limit">The exceeded budget name.</param>
/// <param name="Actual">The observed cost, possibly a lower bound for encoded bytes.</param>
/// <param name="Maximum">The supported maximum.</param>
public sealed record TenantAccessSnapshotLimitExceededRejection(string TenantId, string Limit, long Actual, long Maximum) : IRejectionEvent;
