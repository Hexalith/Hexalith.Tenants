using Hexalith.EventStore.Contracts.Rest;

// Tenants external API endpoints operate on the fixed EventStore system tenant; per-user authorization
// remains enforced by EventStore from the forwarded caller bearer.
[assembly: RestApi("api/tenants", "tenants", RestTenantSource.System)]
