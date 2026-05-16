using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Tenants.Contracts.Enums;

using Microsoft.AspNetCore.DataProtection;

namespace Hexalith.Tenants.Queries;

/// <summary>
/// Encodes and validates protected tenant query cursors.
/// </summary>
public interface ITenantQueryCursorCodec {
    /// <summary>
    /// Creates an opaque cursor for the specified query, scope, and logical position.
    /// </summary>
    /// <param name="queryType">Query type that owns the cursor.</param>
    /// <param name="scope">Endpoint scope that owns the cursor.</param>
    /// <param name="position">Logical pagination position to protect.</param>
    /// <returns>A protected cursor string safe to return to clients.</returns>
    string Encode(string queryType, string scope, string position);

    /// <summary>
    /// Validates and decodes an optional cursor for the expected query and scope.
    /// </summary>
    /// <param name="cursor">Protected cursor submitted by the client.</param>
    /// <param name="queryType">Expected query type.</param>
    /// <param name="scope">Expected endpoint scope.</param>
    /// <param name="position">Decoded logical position when validation succeeds.</param>
    /// <returns><see langword="true"/> when the cursor is empty or valid; otherwise <see langword="false"/>.</returns>
    bool TryDecode(string? cursor, string queryType, string scope, out string? position);
}

/// <summary>
/// Data Protection backed implementation of <see cref="ITenantQueryCursorCodec"/>.
/// </summary>
/// <param name="dataProtectionProvider">Data Protection provider used to create the cursor protector.</param>
public sealed class TenantQueryCursorCodec(IDataProtectionProvider dataProtectionProvider) : ITenantQueryCursorCodec {
    internal const string Purpose = "Hexalith.Tenants.QueryCursor.v1";
    private const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions s_jsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(Purpose);

    public string Encode(string queryType, string scope, string position) {
        ArgumentException.ThrowIfNullOrWhiteSpace(queryType);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(position);

        var payload = new TenantQueryCursorPayload(
            CurrentVersion,
            queryType,
            scope,
            position,
            DateTimeOffset.UtcNow);

        return _protector.Protect(JsonSerializer.Serialize(payload, s_jsonOptions));
    }

    public bool TryDecode(string? cursor, string queryType, string scope, out string? position) {
        ArgumentException.ThrowIfNullOrWhiteSpace(queryType);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);

        position = null;
        if (string.IsNullOrWhiteSpace(cursor)) {
            return true;
        }

        try {
            string json = _protector.Unprotect(cursor);
            TenantQueryCursorPayload? payload = JsonSerializer.Deserialize<TenantQueryCursorPayload>(json, s_jsonOptions);
            if (payload is null
                || payload.Version != CurrentVersion
                || !string.Equals(payload.QueryType, queryType, StringComparison.Ordinal)
                || !string.Equals(payload.Scope, scope, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(payload.Position)) {
                return false;
            }

            position = payload.Position;
            return true;
        }
        catch (ArgumentException) {
            return false;
        }
        catch (CryptographicException) {
            return false;
        }
        catch (JsonException) {
            return false;
        }
    }

    private sealed record TenantQueryCursorPayload(
        int Version,
        string QueryType,
        string Scope,
        string Position,
        DateTimeOffset IssuedAt);
}

internal static class TenantQueryCursorScopes {
    public static string ListTenants(string userId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return $"user:{userId}";
    }

    public static string GetTenantUsers(string tenantId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return $"tenant:{tenantId}";
    }

    public static string GetUserTenants(string targetUserId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetUserId);
        return $"target-user:{targetUserId}";
    }

    public static string GetTenantAudit(
        string tenantId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        AuditEventCategory? category) {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"tenant:{tenantId}|from:{FormatInstant(from)}|to:{FormatInstant(to)}|category:{category?.ToString() ?? string.Empty}");
    }

    private static string FormatInstant(DateTimeOffset? value)
        => value?.UtcDateTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
}
