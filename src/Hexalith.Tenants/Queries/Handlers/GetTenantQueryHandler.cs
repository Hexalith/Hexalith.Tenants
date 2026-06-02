using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;

using Microsoft.Extensions.Logging;

namespace Hexalith.Tenants.Queries.Handlers;

/// <summary>
/// Serves the <c>get-tenant</c> query: full details for a single tenant, gated by tenant membership
/// or global-administrator role.
/// </summary>
public sealed class GetTenantQueryHandler(
    IReadModelStore store,
    IQueryCursorCodec cursorCodec,
    ILogger<GetTenantQueryHandler> logger)
    : TenantQueryHandlerBase(store, cursorCodec, logger) {
    /// <inheritdoc/>
    public override string QueryType => GetTenantQuery.QueryType;

    /// <inheritdoc/>
    protected override async Task<QueryResult> ExecuteCoreAsync(QueryEnvelope envelope, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        TenantReadModel? model = await GetStateAsync<TenantReadModel>(
            TenantProjectionKeyPrefix + envelope.AggregateId, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (model is null) {
            return await IsGlobalAdminAsync(envelope.UserId, cancellationToken).ConfigureAwait(false)
                ? new QueryResult(false, default, ErrorMessage: "Tenant not found")
                : new QueryResult(false, default, ErrorMessage: QueryAdapterFailureReason.Forbidden);
        }

        if (!await IsAuthorizedForTenantAsync(envelope.UserId, model, cancellationToken).ConfigureAwait(false)) {
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
        return CreateSuccessResult(payload, "tenants");
    }
}
