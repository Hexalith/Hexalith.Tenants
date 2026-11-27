namespace Hexalith.Tenants.Contracts.Events.Rejections;

/// <summary>Rejects a recovery atomically when authoritative tenant state contains malformed access fields.</summary>
/// <param name="TenantId">The managed tenant identifier.</param>
/// <param name="Field">The malformed access snapshot field name (<c>status</c>, <c>members</c>, or <c>configurationKeys</c>).</param>
public sealed record TenantAccessStateMalformedRejection(string TenantId, string Field) : IRejectionEvent;
