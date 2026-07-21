namespace Hexalith.Tenants.UI.Services.SupportSafety;

/// <summary>
/// Applies the final fail-closed copy eligibility check immediately before clipboard interop.
/// </summary>
public static class SupportSafeCopyClassifier
{
    /// <summary>
    /// Classifies a caller-supplied literal using its explicit surface approval and data kind.
    /// </summary>
    /// <param name="value">The exact literal that would be written to the clipboard.</param>
    /// <param name="kind">The approved data contract represented by the literal.</param>
    /// <param name="isApproved">Whether the authorized outer surface explicitly approved the literal.</param>
    /// <returns>The fail-closed clipboard eligibility.</returns>
    public static SupportSafeCopyEligibility Classify(
        string? value,
        SupportSafeCopyValueKind kind,
        bool isApproved)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return SupportSafeCopyEligibility.Empty;
        }

        bool isKnownKind = kind is SupportSafeCopyValueKind.TenantId
            or SupportSafeCopyValueKind.UserId
            or SupportSafeCopyValueKind.ConfigurationKey
            or SupportSafeCopyValueKind.SafeConfigurationValue
            or SupportSafeCopyValueKind.ApprovedReference;
        return isApproved && isKnownKind
            ? SupportSafeCopyEligibility.Allowed
            : SupportSafeCopyEligibility.Unsafe;
    }

    /// <summary>
    /// Determines whether a caller-supplied literal is eligible for clipboard interop.
    /// </summary>
    /// <param name="value">The exact literal that would be written to the clipboard.</param>
    /// <param name="kind">The approved data contract represented by the literal.</param>
    /// <param name="isApproved">Whether the authorized outer surface explicitly approved the literal.</param>
    /// <returns><see langword="true"/> only when the literal is non-empty and explicitly approved.</returns>
    public static bool IsAllowed(string? value, SupportSafeCopyValueKind kind, bool isApproved)
        => Classify(value, kind, isApproved) is SupportSafeCopyEligibility.Allowed;
}
