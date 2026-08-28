namespace Hexalith.Tenants.UI.State.GlobalAdministrators;

/// <summary>Identifies a fixed-scope global-administrator mutation.</summary>
public enum GlobalAdministratorActionKind
{
    /// <summary>No qualifying action.</summary>
    Unknown = 0,

    /// <summary>Grant global-administrator authority.</summary>
    Grant = 1,

    /// <summary>Remove global-administrator authority.</summary>
    Remove = 2,
}
