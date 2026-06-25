using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Telemetry;

using Microsoft.Extensions.Options;

namespace Hexalith.Tenants.Queries.Handlers;

/// <summary>
/// Serves the <c>get-tenant</c> query: full details for a single tenant, gated by tenant membership
/// or global-administrator role.
/// </summary>
public sealed class GetTenantQueryHandler(
    IReadModelStore store,
    IQueryCursorCodec cursorCodec,
    TenantTelemetry telemetry,
    ILogger<GetTenantQueryHandler> logger,
    IOptions<ReadModelFreshnessOptions>? freshnessOptions = null,
    TimeProvider? timeProvider = null)
    : TenantQueryHandlerBase(store, cursorCodec, telemetry, logger, freshnessOptions, timeProvider) {
    /// <inheritdoc/>
    public override string QueryType => GetTenantQuery.QueryType;

    /// <inheritdoc/>
    protected override async Task<QueryResult> ExecuteCoreAsync(QueryEnvelope envelope, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        ReadModelEntry<TenantReadModel>? tenantEntry = await GetStateEntryAsync<TenantReadModel>(
            TenantProjectionKeyPrefix + envelope.AggregateId, cancellationToken).ConfigureAwait(false);
        TenantReadModel? model = tenantEntry?.Value;
        cancellationToken.ThrowIfCancellationRequested();

        if (model is null) {
            return await IsGlobalAdminAsync(envelope, cancellationToken).ConfigureAwait(false)
                ? new QueryResult(false, default, ErrorMessage: "Tenant not found")
                : new QueryResult(false, default, ErrorMessage: QueryAdapterFailureReason.Forbidden);
        }

        if (!await IsAuthorizedForTenantAsync(envelope, model, cancellationToken).ConfigureAwait(false)) {
            return new QueryResult(false, default, ErrorMessage: "Forbidden");
        }

        cancellationToken.ThrowIfCancellationRequested();
        TenantDetail detail = new(
            model.TenantId,
            model.Name,
            model.Description,
            model.Status,
            GetConcreteMembers(model).Select(m => new TenantMember(m.Key, m.Value)).ToList(),
            model.Configuration.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            model.CreatedAt);

        JsonElement payload = SerializeToElement(detail);
        return CreateSuccessResult(payload, "tenants", model, tenantEntry?.ETag);
    }
}
