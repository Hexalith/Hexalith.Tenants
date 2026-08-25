namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Defines stable automation names for reason categories that supplement the canonical unavailable reasons.
/// </summary>
public static class TenantHighImpactReasonCategoryNames
{
    /// <summary>Identifies a retained lifecycle attempt that can be reopened for reconciliation.</summary>
    public const string RetainedAttempt = nameof(RetainedAttempt);

    /// <summary>Identifies an in-flight command or disconnected command surface.</summary>
    public const string InFlightOrCommandSurface = nameof(InFlightOrCommandSurface);

    /// <summary>Identifies a proven lifecycle same-state domain outcome.</summary>
    public const string LifecycleStateAlreadySet = nameof(LifecycleStateAlreadySet);

    /// <summary>Gets the stable name of a canonical unavailable reason.</summary>
    /// <param name="reason">Canonical unavailable reason.</param>
    /// <returns>The stable reason-category name.</returns>
    public static string ForUnavailableReason(TenantHighImpactUnavailableReason reason)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, null);
        }

        return reason.ToString();
    }
}
