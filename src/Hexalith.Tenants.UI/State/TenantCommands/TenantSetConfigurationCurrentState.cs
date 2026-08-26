namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Classifies the current authoritative value without disclosing it.</summary>
public enum TenantSetConfigurationCurrentState
{
    /// <summary>The current value could not be classified safely.</summary>
    Unknown,

    /// <summary>The composed key is absent.</summary>
    Absent,

    /// <summary>The composed key exists with a different value.</summary>
    Different,

    /// <summary>The composed key already has the intended value.</summary>
    Matching,
}
