using Hexalith.Tenants.UI.Services.SupportSafety;

namespace Hexalith.Tenants.UI.State.TenantAudit;

/// <summary>
/// Preserves the existing audit-field display and receipt safety policy independently from clipboard approval.
/// </summary>
internal static class TenantAuditSupportSafety
{
    private static readonly string[] IdentifierUnsafeFragments =
    [
        "bearer ",
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

        string[] fragments = kind is SupportSafeCopyValueKind.TenantId or SupportSafeCopyValueKind.UserId
            ? IdentifierUnsafeFragments
            : StrictUnsafeFragments;
        return !fragments.Any(fragment => value.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }
}
