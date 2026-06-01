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
    /// <param name="failureReason">
    /// Short, log-safe reason code when validation fails (e.g. <c>"malformed"</c>,
    /// <c>"wrong-query-type"</c>, <c>"wrong-scope"</c>, <c>"wrong-version"</c>,
    /// <c>"empty-position"</c>, <c>"too-large"</c>, <c>"tamper-or-key-rotation"</c>).
    /// <see langword="null"/> on success or when <paramref name="cursor"/> is empty.
    /// </param>
    /// <returns><see langword="true"/> when the cursor is empty or valid; otherwise <see langword="false"/>.</returns>
    bool TryDecode(string? cursor, string queryType, string scope, out string? position, out string? failureReason);
}

/// <summary>
/// Data Protection backed implementation of <see cref="ITenantQueryCursorCodec"/>.
/// </summary>
/// <param name="dataProtectionProvider">Data Protection provider used to create the cursor protector.</param>
public sealed class TenantQueryCursorCodec(IDataProtectionProvider dataProtectionProvider) : ITenantQueryCursorCodec {
    internal const string Purpose = "Hexalith.Tenants.QueryCursor.v1";
    private const int CurrentVersion = 1;

    // Protected tokens are short (a few hundred bytes for the expected payload shape). 4 KB is well
    // above the realistic ceiling and bounds memory/CPU spent on Unprotect for attacker-supplied input.
    private const int MaxCursorLength = 4096;

    private static readonly JsonSerializerOptions s_jsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(Purpose);

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public bool TryDecode(string? cursor, string queryType, string scope, out string? position, out string? failureReason) {
        ArgumentException.ThrowIfNullOrWhiteSpace(queryType);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);

        position = null;
        failureReason = null;
        if (string.IsNullOrWhiteSpace(cursor)) {
            return true;
        }

        if (cursor.Length > MaxCursorLength) {
            failureReason = "too-large";
            return false;
        }

        try {
            string json = _protector.Unprotect(cursor);
            TenantQueryCursorPayload? payload = JsonSerializer.Deserialize<TenantQueryCursorPayload>(json, s_jsonOptions);
            if (payload is null) {
                failureReason = "malformed";
                return false;
            }

            if (payload.Version != CurrentVersion) {
                failureReason = "wrong-version";
                return false;
            }

            if (!string.Equals(payload.QueryType, queryType, StringComparison.Ordinal)) {
                failureReason = "wrong-query-type";
                return false;
            }

            if (!string.Equals(payload.Scope, scope, StringComparison.Ordinal)) {
                failureReason = "wrong-scope";
                return false;
            }

            if (string.IsNullOrWhiteSpace(payload.Position)) {
                failureReason = "empty-position";
                return false;
            }

            position = payload.Position;
            return true;
        }
        catch (CryptographicException) {
            // Unprotect failure: payload was tampered with, produced for a different protector, or signed with a rotated-out key.
            failureReason = "tamper-or-key-rotation";
            return false;
        }
        catch (JsonException) {
            failureReason = "malformed";
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
        return $"user:{EscapeSegment(userId)}";
    }

    public static string GetTenantUsers(string tenantId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return $"tenant:{EscapeSegment(tenantId)}";
    }

    public static string GetUserTenants(string requesterUserId, string targetUserId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(requesterUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetUserId);
        return $"requester:{EscapeSegment(requesterUserId)}|target-user:{EscapeSegment(targetUserId)}";
    }

    public static string GetTenantAudit(
        string tenantId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        AuditEventCategory? category) {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"tenant:{EscapeSegment(tenantId)}|from:{FormatInstant(from)}|to:{FormatInstant(to)}|category:{EscapeSegment(category?.ToString())}");
    }

    // The cursor scope uses '|' as a segment separator and ':' as a key/value separator.
    // Escape both inside caller-supplied segments so an attacker-controlled id cannot collide
    // with another tenant's scope by injecting '|' or ':'.
    private static string EscapeSegment(string? value)
        => string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("\\", "\\\\", StringComparison.Ordinal)
                   .Replace("|", "\\p", StringComparison.Ordinal)
                   .Replace(":", "\\c", StringComparison.Ordinal);

    private static string FormatInstant(DateTimeOffset? value)
        => value?.UtcDateTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
}
