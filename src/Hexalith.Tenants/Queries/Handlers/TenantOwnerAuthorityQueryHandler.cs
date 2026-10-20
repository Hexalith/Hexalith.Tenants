using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Aggregates;

namespace Hexalith.Tenants.Queries.Handlers;

/// <summary>Folds the current tenant source to prove TenantOwner without returning member data.</summary>
public sealed class TenantOwnerAuthorityQueryHandler(IAuthoritativeEventStreamReader reader, TimeProvider clock) : IDomainQueryHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly IReadOnlyDictionary<string, (Type Type, MethodInfo Method)> EventTypes =
        typeof(TenantState).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.Name == nameof(TenantState.Apply) && method.GetParameters().Length == 1)
            .Select(method => (Type: method.GetParameters()[0].ParameterType, Method: method))
            .ToDictionary(item => item.Type.FullName!, item => item, StringComparer.Ordinal);

    public string Domain => "tenants";

    public string QueryType => ResolveTenantOwnerAuthority.QueryType;

    public async Task<QueryResult> ExecuteAsync(QueryEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ResolveTenantOwnerAuthority? request;
        try
        {
            request = JsonSerializer.Deserialize<ResolveTenantOwnerAuthority>(envelope.Payload, JsonOptions);
        }
        catch (JsonException)
        {
            return QueryResult.Failure(QueryAdapterFailureReason.Forbidden);
        }

        if (request is null || envelope.Domain != Domain || envelope.QueryType != QueryType ||
            envelope.AuthenticatedWorkloadId is not ("chatbot" or "projects") || !envelope.IsDelegated ||
            envelope.OriginalActorId != envelope.UserId ||
            envelope.TenantId != "system" || request.TenantId != envelope.AggregateId ||
            request.PrincipalId != envelope.UserId || request.ActorClass != "human" ||
            string.IsNullOrWhiteSpace(request.Operation) || string.IsNullOrWhiteSpace(request.Origin))
        {
            return QueryResult.Failure(QueryAdapterFailureReason.Forbidden);
        }

        AuthoritativeStreamReadResult read = await reader.ReadAsync(
            new(envelope.TenantId, Domain, envelope.AggregateId), cancellationToken).ConfigureAwait(false);
        if (!read.IsAuthoritative || read.Stream is not { } source || source.Head <= 0 ||
            source.ObservedAt > clock.GetUtcNow() || !TryFold(source, request.TenantId, out TenantState? state))
        {
            return QueryResult.Failure(QueryAdapterFailureReason.Forbidden);
        }

        bool allowed = state!.Status == TenantStatus.Active && state.HasMembershipHistory &&
            state.Users.TryGetValue(request.PrincipalId, out TenantRole role) && role == TenantRole.TenantOwner;
        var result = new TenantOwnerAuthorityResult(request.TenantId, request.PrincipalId, request.Operation,
            request.ActorClass, request.Origin, allowed, source.Head, source.ObservationId, source.ObservedAt);
        return QueryResult.FromPayload(JsonSerializer.SerializeToElement(result, JsonOptions), null,
            new QueryResponseMetadata { Provenance = QueryResponseProvenance.HandlerComputed });
    }

    internal static bool TryFold(AuthoritativeEventStream source, string tenantId, out TenantState? state)
    {
        state = null;
        if (source.Identity.TenantId != "system" || source.Identity.Domain != "tenants" ||
            source.Identity.AggregateId != tenantId || source.Events.Count != source.Head ||
            string.IsNullOrWhiteSpace(source.ObservationId))
        {
            return false;
        }

        var current = new TenantState();
        long sequence = 0;
        foreach (StreamReadEvent item in source.Events)
        {
            if (item is null || item.SequenceNumber != ++sequence || item.Payload is null ||
                item.SerializationFormat != "json" ||
                !EventTypes.TryGetValue(item.EventTypeName.Split(',', 2)[0].Trim(), out var known))
            {
                return false;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(item.Payload);
                if (!document.RootElement.TryGetProperty("tenantId", out JsonElement eventTenant) &&
                    !document.RootElement.TryGetProperty("TenantId", out eventTenant) ||
                    eventTenant.GetString() != tenantId)
                {
                    return false;
                }

                object? payload = JsonSerializer.Deserialize(item.Payload, known.Type, JsonOptions);
                if (payload is null) { return false; }
                _ = known.Method.Invoke(current, [payload]);
            }
            catch (Exception exception) when (exception is JsonException or ArgumentException or TargetInvocationException or InvalidOperationException)
            {
                return false;
            }
        }

        if (current.TenantId != tenantId) { return false; }
        state = current;
        return true;
    }
}
