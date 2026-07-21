namespace Hexalith.Tenants.UI.Components.Tenants.Configuration;

/// <summary>
/// Preserves the transitional configuration display-redaction policy until a positive safe-value model exists.
/// </summary>
/// <remarks>This policy is only for on-screen display and never constitutes clipboard approval.</remarks>
internal static class LegacyConfigurationDisplaySanitizer
{
    private static readonly string[] UnsafeFragments =
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
    /// Determines whether a configuration key and value can remain visible under the legacy display policy.
    /// </summary>
    /// <param name="key">The projected configuration key.</param>
    /// <param name="value">The projected configuration value.</param>
    /// <returns><see langword="true"/> only when both literals satisfy the display policy.</returns>
    internal static bool IsDisplayable(string? key, string? value)
        => IsDisplayable(value) && IsDisplayable(key);

    /// <summary>
    /// Determines whether one configuration literal can remain visible under the legacy display policy.
    /// </summary>
    /// <param name="value">The projected configuration literal.</param>
    /// <returns><see langword="true"/> when the literal satisfies the display policy.</returns>
    internal static bool IsDisplayable(string? value)
        => !string.IsNullOrWhiteSpace(value)
            && !UnsafeFragments.Any(fragment => value.Contains(fragment, StringComparison.OrdinalIgnoreCase));
}
