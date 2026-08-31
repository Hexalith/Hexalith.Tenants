using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantDetail;

namespace Hexalith.Tenants.UI.State.GlobalAdministrators;

/// <summary>Contains immutable evidence for one fixed-scope availability decision.</summary>
/// <param name="IsAuthorized">Whether current server-reflected authority is affirmative.</param>
/// <param name="VisibleKind">Visible direct-read surface state.</param>
/// <param name="VisibleFreshness">Visible direct-read freshness.</param>
/// <param name="VisibleLifecycle">Visible projection lifecycle.</param>
/// <param name="VisibleProjectionVersion">Visible projection version.</param>
/// <param name="VisibleIsAuthorizationScopedEmpty">Whether an empty visible read is authorization scoped.</param>
/// <param name="VisibleRows">Visible administrator rows.</param>
/// <param name="CompleteKind">Bounded complete-walk surface state.</param>
/// <param name="CompleteFreshness">Bounded complete-walk freshness.</param>
/// <param name="CompleteLifecycle">Bounded complete-walk projection lifecycle.</param>
/// <param name="CompleteProjectionVersion">Bounded complete-walk projection version.</param>
/// <param name="CompleteIsAuthorizationScopedEmpty">Whether an empty complete read is authorization scoped.</param>
/// <param name="CompleteRows">Rows from the bounded complete walk.</param>
/// <param name="HasCompletePopulation">Whether the complete walk proved its invariants.</param>
/// <param name="SupportsDispatch">Whether fixed-scope dispatch is available.</param>
/// <param name="SupportsStatus">Whether command status lookup is available.</param>
/// <param name="SupportsRequery">Whether fixed-scope requery is available.</param>
/// <param name="IsAdmissionAvailable">Whether the fixed aggregate is free for this owner.</param>
/// <param name="IsRemovePreviewReady">Whether the actual removal preview is ready.</param>
/// <param name="Viewport">Measured viewport safety state.</param>
/// <param name="HasViewportMeasurement">Whether viewport state came from a browser measurement.</param>
public sealed record GlobalAdministratorActionEvidence(
    bool IsAuthorized,
    GlobalAdministratorsSurfaceKind VisibleKind,
    ReadModelFreshnessState VisibleFreshness,
    ProjectionLifecycleState VisibleLifecycle,
    string? VisibleProjectionVersion,
    bool VisibleIsAuthorizationScopedEmpty,
    IReadOnlyList<GlobalAdministratorRow> VisibleRows,
    GlobalAdministratorsSurfaceKind CompleteKind,
    ReadModelFreshnessState CompleteFreshness,
    ProjectionLifecycleState CompleteLifecycle,
    string? CompleteProjectionVersion,
    bool CompleteIsAuthorizationScopedEmpty,
    IReadOnlyList<GlobalAdministratorRow> CompleteRows,
    bool HasCompletePopulation,
    bool SupportsDispatch,
    bool SupportsStatus,
    bool SupportsRequery,
    bool IsAdmissionAvailable,
    bool IsRemovePreviewReady,
    TenantHighImpactViewportState Viewport,
    bool HasViewportMeasurement)
{
    /// <summary>
    /// Gets whether the actual grant preview is ready.
    /// </summary>
    /// <remarks>
    /// Existing action-specific consumers that supplied one preview value retain that value. Consumers that
    /// can observe grant and removal independently must set this property explicitly.
    /// </remarks>
    public bool IsGrantPreviewReady { get; init; } = IsRemovePreviewReady;

    /// <summary>Returns a support-safe description that omits identities and projection metadata.</summary>
    /// <returns>A bounded diagnostic description.</returns>
    public override string ToString()
        => $"{nameof(GlobalAdministratorActionEvidence)} {{ IsAuthorized = {IsAuthorized}, VisibleKind = {VisibleKind}, CompleteKind = {CompleteKind}, HasCompletePopulation = {HasCompletePopulation}, SupportsDispatch = {SupportsDispatch}, SupportsStatus = {SupportsStatus}, SupportsRequery = {SupportsRequery}, IsAdmissionAvailable = {IsAdmissionAvailable}, IsGrantPreviewReady = {IsGrantPreviewReady}, IsRemovePreviewReady = {IsRemovePreviewReady}, Viewport = {Viewport}, HasViewportMeasurement = {HasViewportMeasurement} }}";
}
