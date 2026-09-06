using Hexalith.Tenants.UI.Services.SupportSafety;

using System.Text;

namespace Hexalith.Tenants.UI.State.TenantAudit;

/// <summary>
/// Preserves the existing audit-field display and receipt safety policy independently from clipboard approval.
/// </summary>
internal static class TenantAuditSupportSafety
{
    private static readonly string[] IdentifierUnsafeFragments =
    [
        "bearer ",
        "accesstoken",
        "authorization",
        "secret",
        "password",
        "token",
        "credential",
        "connectionstring",
        "jwt",
        "eyj",
        "metadata",
        "correlation",
        "stack trace",
        "exception",
        "cursor",
        "etag",
        "messageid",
        "payload",
        "problem detail",
    ];

    private static readonly string[] StrictUnsafeFragments =
    [
        "secret",
        "password",
        "token",
        "credential",
        "connectionstring",
        "authorization",
        "accesstoken",
        "bearer ",
        "metadata",
        "correlation",
        "stack trace",
        "exception",
        "jwt",
        "eyj",
        "cursor",
        "etag",
        "messageid",
        "payload",
        "problem detail",
        "infrastructure",
        "@",
    ];

    /// <summary>
    /// Returns an identifier only when it satisfies the established audit-field policy.
    /// </summary>
    /// <param name="value">The projected audit identifier.</param>
    /// <param name="kind">The identifier contract.</param>
    /// <returns>The original literal when safe; otherwise an empty string.</returns>
    internal static string SafeIdentifier(string? value, SupportSafeCopyValueKind kind)
        => IsSafe(value, kind) ? value! : string.Empty;

    /// <summary>
    /// Returns a reference only when it satisfies the established audit-field policy.
    /// </summary>
    /// <param name="value">The projected audit reference.</param>
    /// <returns>The original literal when safe; otherwise <see langword="null"/>.</returns>
    internal static string? SafeApprovedReference(string? value)
        => IsSafe(value, SupportSafeCopyValueKind.ApprovedReference) ? value : null;

    /// <summary>
    /// Determines whether an audit field satisfies the established display and receipt policy.
    /// </summary>
    /// <param name="value">The projected audit field.</param>
    /// <param name="kind">The field contract.</param>
    /// <returns><see langword="true"/> when the field is safe for audit display and receipt use.</returns>
    internal static bool IsSafe(string? value, SupportSafeCopyValueKind kind)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.Any(char.IsControl))
        {
            return false;
        }

        string[] fragments = kind is SupportSafeCopyValueKind.TenantId or SupportSafeCopyValueKind.UserId
            ? IdentifierUnsafeFragments
            : StrictUnsafeFragments;
        string candidate = CanonicalizeForInspection(value);
        if (candidate.Contains('%', StringComparison.Ordinal)
            || (kind is SupportSafeCopyValueKind.TenantId
                or SupportSafeCopyValueKind.UserId
                or SupportSafeCopyValueKind.ConfigurationKey
                && candidate.Contains(';', StringComparison.Ordinal)))
        {
            return false;
        }

        string normalized = new(candidate
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
        return !fragments.Any(fragment =>
        {
            string normalizedFragment = Normalize(fragment);
            return candidate.Contains(fragment, StringComparison.OrdinalIgnoreCase)
                || (normalizedFragment.Length > 0
                    && normalized.Contains(normalizedFragment, StringComparison.Ordinal));
        });
    }

    private static string CanonicalizeForInspection(string value)
    {
        string candidate = value;
        for (int attempt = 0; attempt < 3 && candidate.Contains('%', StringComparison.Ordinal); attempt++)
        {
            try
            {
                string decoded = Uri.UnescapeDataString(candidate);
                if (string.Equals(decoded, candidate, StringComparison.Ordinal))
                {
                    break;
                }

                candidate = decoded;
            }
            catch (UriFormatException)
            {
                return value;
            }
        }

        return candidate;
    }

    private static string Normalize(string value)
    {
        var result = new StringBuilder(value.Length);
        foreach (char character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                result.Append(char.ToLowerInvariant(character));
            }
        }

        return result.ToString();
    }
}
