namespace Hexalith.Tenants.UI.State.GlobalAdministrators;

/// <summary>Canonical fixed-scope action blocker vocabulary.</summary>
public enum GlobalAdministratorActionUnavailableReason
{
    /// <summary>No blocker.</summary>
    None = 0,

    /// <summary>Current authority is missing or indeterminate.</summary>
    MissingPermission = 1,

    /// <summary>Direct-read evidence is absent, stale, contradictory, or otherwise unsafe.</summary>
    StaleData = 2,

    /// <summary>One or more required command lifecycle capabilities are unavailable.</summary>
    MissingLifecycleSupport = 3,

    /// <summary>The browser viewport has not been measured as safe.</summary>
    UnsafeViewport = 4,

    /// <summary>Another owner has admitted work for the fixed aggregate.</summary>
    AggregateBusy = 5,

    /// <summary>The evaluated action's consequence preview is unavailable.</summary>
    MissingConsequencePreview = 6,

    /// <summary>The complete fixed population cannot be proven.</summary>
    IncompletePopulation = 7,

    /// <summary>The requested removal target is not visible in qualified evidence.</summary>
    TargetMissing = 8,

    /// <summary>The target is the last proven global administrator.</summary>
    LastAdministrator = 9,
}
