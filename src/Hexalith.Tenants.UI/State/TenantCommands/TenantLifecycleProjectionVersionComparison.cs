namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>
/// Describes whether two lifecycle projection versions provide ordered advancement evidence.
/// </summary>
internal enum TenantLifecycleProjectionVersionComparison
{
    /// <summary>One or both projection versions cannot be parsed as ordered markers.</summary>
    Invalid,

    /// <summary>The projection versions use different ordering prefixes.</summary>
    PrefixMismatch,

    /// <summary>The current projection version is equal to or older than the baseline.</summary>
    NotAdvanced,

    /// <summary>The current projection version is strictly newer than the baseline.</summary>
    Advanced,
}
