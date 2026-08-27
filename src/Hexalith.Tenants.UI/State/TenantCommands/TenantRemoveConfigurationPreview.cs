using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Contains one authorization-scoped, value-free remove-configuration preview.</summary>
public sealed class TenantRemoveConfigurationPreview
{
    private TenantRemoveConfigurationPreview(
        TenantRemoveConfigurationIntent intent,
        TenantStatus tenantStatus,
        TenantRemoveConfigurationCurrentState currentState,
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
    public TenantRemoveConfigurationIntent Intent { get; }

    /// <summary>Gets the authoritative tenant lifecycle state.</summary>
    public TenantStatus TenantStatus { get; }

    /// <summary>Gets the redacted current key classification.</summary>
    public TenantRemoveConfigurationCurrentState CurrentState { get; }

    /// <summary>Gets authoritative freshness evidence.</summary>
    public ReadModelFreshnessState Freshness { get; }

    /// <summary>Gets authoritative projection lifecycle evidence.</summary>
    public ProjectionLifecycleState Lifecycle { get; }

    /// <summary>Gets the ordered projection version captured with the preview.</summary>
    public string? ProjectionVersion { get; }

    /// <summary>Gets whether current server-reflected authority covers the exact namespace and key.</summary>
    public bool IsAuthorized { get; }

    /// <summary>Gets whether the evidence is authoritative enough to classify the exact key.</summary>
    public bool IsAuthoritative
        => IsAuthorized
            && TenantStatus is TenantStatus.Active
            && Freshness is ReadModelFreshnessState.Current
            && Lifecycle is ProjectionLifecycleState.Current
            && TenantLifecycleProjectionVersion.IsOrdered(ProjectionVersion)
            && CurrentState is TenantRemoveConfigurationCurrentState.Absent
                or TenantRemoveConfigurationCurrentState.Present;

    /// <summary>Gets whether this preview contains the present target required for removal dispatch.</summary>
    public bool IsComplete
        => IsAuthoritative && CurrentState is TenantRemoveConfigurationCurrentState.Present;

    /// <summary>Creates authorization-scoped redacted evidence.</summary>
    internal static TenantRemoveConfigurationPreview Create(
        TenantRemoveConfigurationIntent intent,
        TenantStatus tenantStatus,
        TenantRemoveConfigurationCurrentState currentState,
        ReadModelFreshnessState freshness,
        ProjectionLifecycleState lifecycle,
        string? projectionVersion,
        bool isAuthorized)
        => new(intent, tenantStatus, currentState, freshness, lifecycle, projectionVersion, isAuthorized);

    /// <summary>Creates fail-closed unavailable evidence.</summary>
    public static TenantRemoveConfigurationPreview Unavailable(TenantRemoveConfigurationIntent intent)
        => new(
            intent,
            TenantStatus.Unknown,
            TenantRemoveConfigurationCurrentState.Unknown,
            ReadModelFreshnessState.Unknown,
            ProjectionLifecycleState.Unknown,
            projectionVersion: null,
            isAuthorized: false);

    /// <summary>Returns a support-safe description containing classifications only.</summary>
    /// <returns>A fixed-shape diagnostic description.</returns>
    public override string ToString()
        => $"{nameof(TenantRemoveConfigurationPreview)} {{ CurrentState = {CurrentState}, Freshness = {Freshness}, Lifecycle = {Lifecycle}, IsAuthorized = {IsAuthorized}, HasProjectionVersion = {!string.IsNullOrEmpty(ProjectionVersion)} }}";
}
