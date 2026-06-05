namespace Hexalith.Tenants.UI.State.TenantDetail;

public sealed record TenantDetailRequest(string TenantId, string? ETag = null);
