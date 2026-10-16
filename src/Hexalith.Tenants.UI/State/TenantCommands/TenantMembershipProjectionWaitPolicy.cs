using System.Globalization;

using Microsoft.Extensions.Configuration;

namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Bounds membership projection waiting without expiring dispatched command ownership.</summary>
internal sealed class TenantMembershipProjectionWaitPolicy
{
    /// <summary>The deployment setting for the membership projection wait.</summary>
    internal const string ConfigurationKey = "Tenants:Ui:MembershipProjectionTimeout";

    /// <summary>The localized uncertainty guidance used when the wait expires.</summary>
    internal const string ExpiredMessageKey = "Tenants.Membership.ProjectionWait.Expired";

    /// <summary>The finite membership wait used when no deployment value is supplied.</summary>
    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    /// <summary>Creates the membership policy with a monotonic clock.</summary>
    /// <param name="configuration">Deployment configuration.</param>
    /// <param name="timeProvider">The timer and elapsed-time clock.</param>
    internal TenantMembershipProjectionWaitPolicy(IConfiguration? configuration, TimeProvider? timeProvider)
    {
        string? configured = configuration?[ConfigurationKey];
        if (configured is not null
            && (!TimeSpan.TryParse(configured, CultureInfo.InvariantCulture, out TimeSpan parsed)
                || parsed <= TimeSpan.Zero
                || parsed.TotalMilliseconds > uint.MaxValue - 1))
        {
            throw new InvalidOperationException($"{ConfigurationKey} must be a positive finite TimeSpan within timer limits.");
        }

        Timeout = configured is null ? DefaultTimeout : TimeSpan.Parse(configured, CultureInfo.InvariantCulture);
        Clock = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Gets the configured elapsed bound.</summary>
    internal TimeSpan Timeout { get; }

    /// <summary>Gets the clock shared by timestamps and timers.</summary>
    internal TimeProvider Clock { get; }

    /// <summary>Identifies only membership projection waiting, excluding other command budgets.</summary>
    internal static bool IsPending(object snapshot)
        => snapshot is TenantAddMemberCommandSnapshot { State: TenantCommandLifecycleState.ProjectionPending }
            or TenantChangeRoleCommandSnapshot { State: TenantCommandLifecycleState.ProjectionPending }
            or TenantRemoveMemberCommandSnapshot { State: TenantCommandLifecycleState.ProjectionPending };

    /// <summary>Preserves complete evidence while presenting expired pending work as recoverable uncertainty.</summary>
    internal static object Expire(object snapshot)
        => snapshot switch
        {
            TenantAddMemberCommandSnapshot add when IsPending(add) => add with
            {
                State = TenantCommandLifecycleState.UnableToVerify,
                ProjectionWaitExpired = true,
                SafeMessage = null,
                SafeMessageKey = ExpiredMessageKey,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            },
            TenantChangeRoleCommandSnapshot change when IsPending(change) => change with
            {
                State = TenantCommandLifecycleState.UnableToVerify,
                ProjectionWaitExpired = true,
                SafeMessage = null,
                SafeMessageKey = ExpiredMessageKey,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            },
            TenantRemoveMemberCommandSnapshot remove when IsPending(remove) => remove with
            {
                State = TenantCommandLifecycleState.UnableToVerify,
                ProjectionWaitExpired = true,
                SafeMessage = null,
                SafeMessageKey = ExpiredMessageKey,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            },
            _ => snapshot,
        };
}
