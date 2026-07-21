namespace Hexalith.Tenants.UI.Services.SupportSafety;

/// <summary>
/// Describes the fail-closed clipboard eligibility of a caller-supplied literal.
/// </summary>
public enum SupportSafeCopyEligibility
{
    /// <summary>
    /// Indicates that clipboard interop is not approved.
    /// </summary>
    Unsafe = 0,

    /// <summary>
    /// Indicates that the literal contains no non-whitespace content.
    /// </summary>
    Empty = 1,

    /// <summary>
    /// Indicates that the literal is non-empty and explicitly approved for its declared contract.
    /// </summary>
    Allowed = 2,
}
