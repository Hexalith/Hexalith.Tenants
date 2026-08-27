namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Classifies authoritative key presence without disclosing its value.</summary>
public enum TenantRemoveConfigurationCurrentState
{
    /// <summary>The current key state could not be classified safely.</summary>
    Unknown,

    /// <summary>The exact key is absent.</summary>
    Absent,

    /// <summary>The exact key is present.</summary>
    Present,
}
