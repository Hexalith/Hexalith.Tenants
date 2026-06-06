namespace Hexalith.Tenants.UI.Services.SupportSafety;

public enum SupportSafeCopyValueKind
{
    TenantId,
    UserId,
    ConfigurationKey,
    SafeConfigurationValue,
    ApprovedReference,
}

public enum SupportSafeCopyEligibility
{
    Allowed,
    Empty,
    Unsafe,
}

public static class SupportSafeCopyClassifier
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

    public static SupportSafeCopyEligibility Classify(string? value, SupportSafeCopyValueKind kind)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return SupportSafeCopyEligibility.Empty;
        }

        string[] fragments = kind is SupportSafeCopyValueKind.TenantId or SupportSafeCopyValueKind.UserId
            ? IdentifierUnsafeFragments
            : StrictUnsafeFragments;

        return fragments.Any(fragment => value.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            ? SupportSafeCopyEligibility.Unsafe
            : SupportSafeCopyEligibility.Allowed;
    }

    public static bool IsAllowed(string? value, SupportSafeCopyValueKind kind)
        => Classify(value, kind) == SupportSafeCopyEligibility.Allowed;
}
