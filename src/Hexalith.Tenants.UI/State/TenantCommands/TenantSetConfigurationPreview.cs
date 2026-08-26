using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Contains one authorization-scoped, redacted set-configuration preview.</summary>
public sealed class TenantSetConfigurationPreview
{
    private TenantSetConfigurationPreview(
        TenantSetConfigurationIntent intent,
        TenantStatus tenantStatus,
        TenantSetConfigurationCurrentState currentState,
        ReadModelFreshnessState freshness,
        ProjectionLifecycleState lifecycle,
        string? projectionVersion,
        bool isAuthorized)
    {
        ArgumentNullException.ThrowIfNull(intent);
        Intent = intent;
        TenantStatus = tenantStatus;
        CurrentState = currentState;
        Freshness = freshness;
        Lifecycle = lifecycle;
        ProjectionVersion = projectionVersion;
        IsAuthorized = isAuthorized;
    }

    /// <summary>Gets the safe intent represented by this preview.</summary>
    public TenantSetConfigurationIntent Intent { get; }

    /// <summary>Gets the authoritative tenant lifecycle state.</summary>
    public TenantStatus TenantStatus { get; }

    /// <summary>Gets the redacted classification of the current value.</summary>
    public TenantSetConfigurationCurrentState CurrentState { get; }

    /// <summary>Gets authoritative freshness evidence.</summary>
    public ReadModelFreshnessState Freshness { get; }

    /// <summary>Gets authoritative projection lifecycle evidence.</summary>
    public ProjectionLifecycleState Lifecycle { get; }

    /// <summary>Gets the ordered projection version captured with the preview.</summary>
    public string? ProjectionVersion { get; }

    /// <summary>Gets whether current server-reflected authority covers the exact namespace and key.</summary>
    public bool IsAuthorized { get; }

    /// <summary>Gets whether this preview is complete enough to bind a dispatch.</summary>
    public bool IsComplete
        => IsAuthorized
            && TenantStatus is TenantStatus.Active
            && Freshness is ReadModelFreshnessState.Current
            && Lifecycle is ProjectionLifecycleState.Current
            && TenantLifecycleProjectionVersion.IsOrdered(ProjectionVersion)
            && CurrentState is TenantSetConfigurationCurrentState.Absent
                or TenantSetConfigurationCurrentState.Different
                or TenantSetConfigurationCurrentState.Matching;

    /// <summary>Gets whether authoritative evidence proves the value is already applied.</summary>
    public bool IsAlreadyApplied
        => IsComplete && CurrentState is TenantSetConfigurationCurrentState.Matching;

    /// <summary>Creates an authorization-scoped preview.</summary>
    internal static TenantSetConfigurationPreview Create(
        TenantSetConfigurationIntent intent,
        TenantStatus tenantStatus,
        TenantSetConfigurationCurrentState currentState,
        ReadModelFreshnessState freshness,
        ProjectionLifecycleState lifecycle,
        string? projectionVersion,
        bool isAuthorized)
        => new(intent, tenantStatus, currentState, freshness, lifecycle, projectionVersion, isAuthorized);

    /// <summary>Creates a fail-closed unavailable preview.</summary>
    /// <param name="intent">Safe requested intent.</param>
    /// <returns>An unavailable preview containing no raw value.</returns>
    public static TenantSetConfigurationPreview Unavailable(TenantSetConfigurationIntent intent)
        => new(
            intent,
            TenantStatus.Unknown,
            TenantSetConfigurationCurrentState.Unknown,
            ReadModelFreshnessState.Unknown,
            ProjectionLifecycleState.Unknown,
            projectionVersion: null,
            isAuthorized: false);

    /// <summary>Returns a support-safe description containing classification only.</summary>
    /// <returns>A fixed-shape diagnostic description.</returns>
    public override string ToString()
        => $"{nameof(TenantSetConfigurationPreview)} {{ CurrentState = {CurrentState}, Freshness = {Freshness}, Lifecycle = {Lifecycle}, IsAuthorized = {IsAuthorized}, HasProjectionVersion = {!string.IsNullOrEmpty(ProjectionVersion)} }}";
}
