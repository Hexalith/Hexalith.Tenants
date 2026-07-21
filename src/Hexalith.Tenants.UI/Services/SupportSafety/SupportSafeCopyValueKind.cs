namespace Hexalith.Tenants.UI.Services.SupportSafety;

/// <summary>
/// Identifies the authorized read-model contract represented by a copyable literal.
/// </summary>
public enum SupportSafeCopyValueKind
{
    /// <summary>
    /// Indicates that no approved copy contract was supplied.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Identifies a tenant identifier from an authorized tenant projection.
    /// </summary>
    TenantId = 1,

    /// <summary>
    /// Identifies a user identifier from an authorized membership projection.
    /// </summary>
    UserId = 2,

    /// <summary>
    /// Identifies a configuration key from a positively approved safe configuration model.
    /// </summary>
    ConfigurationKey = 3,

    /// <summary>
    /// Identifies a configuration value from a positively approved safe configuration model.
    /// </summary>
    SafeConfigurationValue = 4,

    /// <summary>
    /// Identifies a support reference explicitly approved by its authorized outer surface.
    /// </summary>
    ApprovedReference = 5,
}
