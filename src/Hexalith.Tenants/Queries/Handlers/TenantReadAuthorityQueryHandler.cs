using System.Text.Json;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Tenants.Queries.Handlers;

/// <summary>Folds the exact native Tenant prefix for the fixed owner metadata Read purpose without returning roster or grants.</summary>
/// <param name="reader">The existing authenticated source gateway reader, explicitly requiring native proof.</param>
/// <param name="metadata">The native metadata seam for the final source sample.</param>
/// <param name="accessor">The SDK-established exact caller.</param>
/// <param name="clock">The server observation clock.</param>
public sealed class TenantReadAuthorityQueryHandler([FromKeyedServices("native-tenant-source")] IAuthoritativeEventStreamReader reader,
    INativeSourceMetadataReader metadata, IHttpContextAccessor accessor, TimeProvider clock) : IDomainQueryHandler
{
    // Stable EventStore wire value: the current source symbol is absent from the pinned package.
    private const string QueryExecutionFailure = "actor-exception";

    /// <inheritdoc/>
    public string Domain => "tenants";
    /// <inheritdoc/>
    public string QueryType => ResolveTenantReadAuthority.QueryType;

    /// <inheritdoc/>
    public async Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        VerifiedQueryCaller? caller = VerifiedQueryCaller.Get(accessor.HttpContext);
        OriginatingQueryContext? origin = caller?.OriginatingContext;
        if (caller is null || !ReferenceEquals(caller.Query, query) || origin is null
            || query.CallerAuthority?.Origin != "delegated-owner-query" || !query.IsDelegated
            || query.AuthenticatedWorkloadId is not ("projects" or "conversations" or "folders")
            || query.Domain != Domain || query.QueryType != QueryType || query.TenantId == "system"
            || query.AggregateId != query.TenantId || !Canonical(query.TenantId) || !Canonical(query.UserId)
            || query.OriginalActorId != query.UserId || origin.OriginalActorId != query.UserId
            || query.Payload is not { Length: <= 8192 }
            || query.IsGlobalAdmin || query.Paging is not null
            || query.EntityId is not null && query.EntityId != query.AggregateId
            || origin.IsDelegated && !Canonical(origin.DelegationId)
            || !origin.IsDelegated && origin.DelegationId is not null
            || !Compatible(origin.Scopes, "projects.read") || !Compatible(origin.Audience, "hexalith-eventstore")
            || !Empty(query.Payload)) return QueryResult.Failure("safe-denial");
        try
        {
            // This is an owner-internal source identity. The managed caller Tenant remains unchanged.
            var identity = new AggregateIdentity("system", Domain, query.TenantId);
            var read = await reader.ReadAsync(identity, cancellationToken).ConfigureAwait(false);
            if (!read.IsAuthoritative || read.Stream is not { NativeProof: { Erased: false } proof } source
                || !NativeSourceProofCodec.IsValid(proof, identity, source.Head, 1)
                || source.Head <= 0 || source.ObservedAt > clock.GetUtcNow()
                || !TenantOwnerAuthorityQueryHandler.TryFold(source, query.TenantId, out var state))
                return QueryResult.Failure(QueryExecutionFailure);
            bool allowed = state!.Status == TenantStatus.Active && state.HasMembershipHistory
                && state.Users.TryGetValue(query.UserId, out TenantRole role)
                && role is TenantRole.TenantReader or TenantRole.TenantContributor or TenantRole.TenantOwner;
            var final = await metadata.ReadAsync(identity, cancellationToken).ConfigureAwait(false);
            if (final is null || final.Proof != proof || final.ObservedAt > clock.GetUtcNow())
                return QueryResult.Failure(QueryExecutionFailure);
            cancellationToken.ThrowIfCancellationRequested();
            return QueryResult.FromPayload(JsonSerializer.SerializeToElement(new TenantReadAuthorityResult(
                query.TenantId, query.UserId, allowed, proof, final.ObservedAt), JsonSerializerOptions.Web), QueryType);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return QueryResult.Failure(QueryExecutionFailure); }
    }

    private static bool Canonical(string? value) => !string.IsNullOrWhiteSpace(value) && value == value.Trim();
    private static bool Compatible(IReadOnlyList<string>? values, string expected)
        => values is null || values.Count == 0 || values.Contains(expected, StringComparer.Ordinal);
    private static bool Empty(byte[] payload)
    {
        try { using var document = JsonDocument.Parse(payload);
            return document.RootElement.ValueKind == JsonValueKind.Object && !document.RootElement.EnumerateObject().Any(); }
        catch (Exception exception) when (exception is JsonException or ArgumentException) { return false; }
    }
}
