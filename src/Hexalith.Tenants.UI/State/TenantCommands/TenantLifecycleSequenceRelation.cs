namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>
/// Describes how an incoming lifecycle projection version relates to a retained one.
/// </summary>
internal enum TenantLifecycleSequenceRelation
{
    /// <summary>The versions cannot be compared as ordered tenant-sequence markers.</summary>
    Incomparable,

    /// <summary>The incoming version is strictly older than the retained version.</summary>
    IncomingOlder,

    /// <summary>The versions are the same comparable tenant-sequence marker.</summary>
    Equal,

    /// <summary>The incoming version is strictly newer than the retained version.</summary>
    IncomingNewer,
}
