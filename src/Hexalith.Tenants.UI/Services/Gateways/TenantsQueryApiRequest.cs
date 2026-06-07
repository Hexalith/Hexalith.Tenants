using System.Text.Json;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed record TenantsQueryApiRequest(
    string Path,
    string QueryType,
    JsonElement? Payload = null,
    string? Tenant = null,
    string? Domain = null,
    string? AggregateId = null,
    string? EntityId = null,
    string? ProjectionType = null);
