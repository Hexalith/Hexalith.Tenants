using System.Reflection;
using System.Text.Json;

using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.Tenants.Server.Aggregates;
using Hexalith.Tenants.Contracts.Events.Rejections;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Tenants.Authorization;

/// <summary>Folds the bounded native administrator stream and verifies its final incarnation and prefix.</summary>
/// <param name="reader">The authenticated reader requiring native proof.</param>
/// <param name="metadata">The final native source observation.</param>
/// <param name="clock">The observation clock.</param>
public sealed class TenantsGlobalAdministratorAuthority(
    [FromKeyedServices("native-tenant-source")] IAuthoritativeEventStreamReader reader,
    INativeSourceMetadataReader metadata,
    TimeProvider clock) : ITenantsGlobalAdministratorAuthority
{
    private static readonly AggregateIdentity Identity = new("system", "global-administrators", "global-administrators");
    private static readonly IReadOnlyDictionary<string, (Type Type, MethodInfo Method)> EventTypes =
        typeof(GlobalAdministratorsState).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.Name == nameof(GlobalAdministratorsState.Apply) && method.GetParameters().Length == 1)
            .Select(method => (Type: method.GetParameters()[0].ParameterType, Method: method))
            .ToDictionary(item => item.Type.FullName!, item => item, StringComparer.Ordinal);

    // This service is scoped to one request. Only a successfully verified observation is reused.
    private HashSet<string>? _verifiedAdministrators;

    /// <inheritdoc/>
    public async Task<bool> IsCurrentGlobalAdministratorAsync(string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(userId)) return false;
        if (_verifiedAdministrators is { } verified) return verified.Contains(userId);
        AuthoritativeStreamReadResult read = await reader.ReadAsync(Identity, cancellationToken).ConfigureAwait(false);
        if (!read.IsAuthoritative || read.Stream is not { NativeProof: { Erased: false } proof } source
            || source.Identity != Identity || source.Head <= 0 || source.Head > 10_000
            || source.Events.Count != source.Head || string.IsNullOrWhiteSpace(source.ObservationId)
            || source.ObservedAt > clock.GetUtcNow()
            || !NativeSourceProofCodec.IsValid(proof, Identity, source.Head, 1)) throw new TenantsGlobalAdministratorAuthorityUnavailableException();

        var state = new GlobalAdministratorsState();
        long sequence = 0;
        long bytes = 0;
        foreach (StreamReadEvent item in source.Events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (item is null || item.SequenceNumber != ++sequence || item.Payload is null
                || item.SerializationFormat != "json" || item.MetadataVersion != 1 || item.PayloadVersion is < 1 or > 1
                || string.IsNullOrWhiteSpace(item.EventTypeName)
                || (bytes += item.Payload.Length) > 16 * 1024 * 1024
                || !EventTypes.TryGetValue(item.EventTypeName.Split(',', 2)[0].Trim(), out var known)) throw new TenantsGlobalAdministratorAuthorityUnavailableException();
            try
            {
                using JsonDocument document = JsonDocument.Parse(item.Payload);
                JsonElement value = document.RootElement;
                if (value.ValueKind != JsonValueKind.Object) throw new TenantsGlobalAdministratorAuthorityUnavailableException();
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (value.EnumerateObject().Any(property => !names.Add(property.Name)))
                    throw new TenantsGlobalAdministratorAuthorityUnavailableException();
                if (value.ValueKind != JsonValueKind.Object
                    || (!value.TryGetProperty("tenantId", out var tenant) && !value.TryGetProperty("TenantId", out tenant))
                    || tenant.GetString() != Identity.TenantId
                    || ((!value.TryGetProperty("userId", out var user) && !value.TryGetProperty("UserId", out user))
                        ? known.Type.Name is "GlobalAdministratorSet" or "GlobalAdministratorRemoved"
                            or nameof(GlobalAdministratorAlreadyExistsRejection) or nameof(GlobalAdministratorNotFoundRejection)
                            or nameof(LastGlobalAdministratorRejection)
                        : string.IsNullOrWhiteSpace(user.GetString()))) throw new TenantsGlobalAdministratorAuthorityUnavailableException();
                if (known.Type == typeof(InsufficientPermissionsRejection)
                    && (!HasRequiredIdentifier(value, "actorUserId", "ActorUserId")
                        || !HasRequiredIdentifier(value, "commandName", "CommandName")))
                    throw new TenantsGlobalAdministratorAuthorityUnavailableException();
                object? payload = JsonSerializer.Deserialize(item.Payload, known.Type, JsonSerializerOptions.Web);
                if (payload is null) throw new TenantsGlobalAdministratorAuthorityUnavailableException();
                _ = known.Method.Invoke(state, [payload]);
            }
            catch (Exception exception) when (exception is JsonException or ArgumentException or InvalidOperationException)
            {
                throw new TenantsGlobalAdministratorAuthorityUnavailableException();
            }
        }

        NativeSourceObservation? final = await metadata.ReadAsync(Identity, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (final is null || final.Proof != proof || final.ObservedAt < source.ObservedAt || final.ObservedAt > clock.GetUtcNow()) throw new TenantsGlobalAdministratorAuthorityUnavailableException();
        _verifiedAdministrators = state.Administrators.ToHashSet(StringComparer.Ordinal);
        return _verifiedAdministrators.Contains(userId);
    }

    private static bool HasRequiredIdentifier(JsonElement value, string camelCase, string pascalCase)
        => (value.TryGetProperty(camelCase, out JsonElement identifier) || value.TryGetProperty(pascalCase, out identifier))
            && identifier.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(identifier.GetString());
}
