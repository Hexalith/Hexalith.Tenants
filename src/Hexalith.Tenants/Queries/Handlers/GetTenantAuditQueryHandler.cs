using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Telemetry;

namespace Hexalith.Tenants.Queries.Handlers;

/// <summary>
/// Serves the <c>get-tenant-audit</c> query: the paginated audit trail for a tenant, restricted to
/// global administrators and defended in depth against cross-tenant row leakage.
/// </summary>
public sealed class GetTenantAuditQueryHandler(
    IReadModelStore store,
    IQueryCursorCodec cursorCodec,
    TenantTelemetry telemetry,
    ILogger<GetTenantAuditQueryHandler> logger)
    : TenantQueryHandlerBase(store, cursorCodec, telemetry, logger) {
    /// <inheritdoc/>
    public override string QueryType => GetTenantAuditQuery.QueryType;

    /// <inheritdoc/>
    protected override async Task<QueryResult> ExecuteCoreAsync(QueryEnvelope envelope, CancellationToken cancellationToken) {
        // CRITICAL: Check GlobalAdmin FIRST — non-admins must get 403, not 501
        if (!await IsGlobalAdminAsync(envelope.UserId, cancellationToken).ConfigureAwait(false)) {
            return new QueryResult(false, default, ErrorMessage: "Forbidden");
        }

        // QueryEnvelope.ctor enforces non-empty AggregateId, so the "audit:" shared-key vector
        // raised in review is unreachable through any constructor path.

        TenantAuditQueryPayload query = DeserializeAuditPayload(envelope.Payload);
        if (query.ErrorMessage is not null) {
            return new QueryResult(false, default, ErrorMessage: query.ErrorMessage);
        }

        string scope = TenantQueryCursorScopes.GetTenantAudit(envelope.AggregateId, query.From, query.To, query.Category);
        if (!CursorCodec.TryDecode(query.Cursor, GetTenantAuditQuery.QueryType, scope, out string? cursor, out string? failureReason)) {
            return InvalidCursorResult(GetTenantAuditQuery.QueryType, "get-tenant-audit", envelope.AggregateId, envelope.UserId, failureReason);
        }

        cancellationToken.ThrowIfCancellationRequested();
        ReadModelEntry<TenantAuditReadModel>? auditEntry = await GetStateEntryAsync<TenantAuditReadModel>(
            TenantAuditProjectionKeyPrefix + envelope.AggregateId, cancellationToken).ConfigureAwait(false);
        TenantAuditReadModel? model = auditEntry?.Value;
        cancellationToken.ThrowIfCancellationRequested();

        // NFR5 defense-in-depth: a projection bug must not leak rows from another tenant.
        IEnumerable<TenantAuditEntry> entries = (model?.Entries ?? [])
            .Where(e => string.Equals(e.TenantId, envelope.AggregateId, StringComparison.Ordinal));
        if (query.From is not null) {
            entries = entries.Where(e => e.Timestamp >= query.From.Value);
        }

        if (query.To is not null) {
            entries = entries.Where(e => e.Timestamp <= query.To.Value);
        }

        if (query.Category is not null) {
            entries = entries.Where(e => e.Category == query.Category.Value);
        }

        cancellationToken.ThrowIfCancellationRequested();
        PaginatedResult<TenantAuditEntry> result = ProtectCursor(
            PaginateAuditEntries(entries, cursor, query.PageSize, cancellationToken),
            GetTenantAuditQuery.QueryType,
            scope);
        cancellationToken.ThrowIfCancellationRequested();
        return CreateSuccessResult(SerializeToElement(result), "tenants", auditEntry?.ETag);
    }
}
