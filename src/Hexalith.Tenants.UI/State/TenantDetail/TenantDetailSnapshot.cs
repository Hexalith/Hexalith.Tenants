using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Services.Configuration;

using TenantDetailContract = Hexalith.Tenants.Contracts.Queries.TenantDetail;

namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Contains factory-sanitized tenant detail and separate safe configuration state.
/// </summary>
public sealed class TenantDetailSnapshot
{
    private TenantDetailSnapshot(
        TenantDetailSurfaceKind kind,
        TenantDetailContract? detail,
        string? eTag,
        ReadModelFreshnessState freshness,
        ProjectionLifecycleState lifecycle,
        string? projectionVersion,
        string? errorMessage,
        TenantConfigurationSafeModel configuration,
        TenantConfigurationManagementContext configurationManagement)
    {
        Kind = kind;
        Detail = detail;
        ETag = eTag;
        Freshness = freshness;
        Lifecycle = lifecycle;
        ProjectionVersion = projectionVersion;
        ErrorMessage = errorMessage;
        Configuration = configuration;
        ConfigurationManagement = configurationManagement;
    }

    /// <summary>Gets the detail surface state.</summary>
    public TenantDetailSurfaceKind Kind { get; }

    /// <summary>Gets sanitized detail whose configuration dictionary is always empty.</summary>
    public TenantDetailContract? Detail { get; }

    /// <summary>Gets the server ETag retained only inside BFF state.</summary>
    public string? ETag { get; }

    /// <summary>Gets authoritative freshness state.</summary>
    public ReadModelFreshnessState Freshness { get; }

    /// <summary>Gets authoritative projection lifecycle evidence without collapsing operational states into freshness.</summary>
    public ProjectionLifecycleState Lifecycle { get; }

    /// <summary>Gets authoritative projection-version evidence retained only for server-side composition.</summary>
    public string? ProjectionVersion { get; }

    /// <summary>Gets support-safe error copy.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Gets positively approved configuration read state.</summary>
    public TenantConfigurationSafeModel Configuration { get; }

    /// <summary>Gets non-sensitive command scope and safe remove targets.</summary>
    public TenantConfigurationManagementContext ConfigurationManagement { get; }

    /// <summary>
    /// Returns a support-safe description that omits tenant identity, configuration keys and values, ETag,
    /// and every other reconstructable field.
    /// </summary>
    /// <remarks>
    /// This type is a class, so without an override it rendered as its own type name. Absence assertions
    /// written against that string could never fail and therefore certified nothing. A fixed shape that
    /// emits only enums, bools, and counts makes the diagnostic pinnable by equality, which is what turns
    /// it into evidence.
    /// </remarks>
    /// <returns>A fixed-shape diagnostic string carrying no disclosable material.</returns>
    public override string ToString()
        => $"{nameof(TenantDetailSnapshot)} {{ Kind = {Kind}, HasDetail = {Detail is not null}, "
            + $"Freshness = {Freshness}, Lifecycle = {Lifecycle}, HasErrorMessage = {ErrorMessage is not null} }}";

    internal static TenantDetailSnapshot Loading()
        => Empty(TenantDetailSurfaceKind.Loading, null);

    internal static TenantDetailSnapshot Ready(
        TenantDetailContract detail,
        string? eTag,
        ReadModelFreshnessState freshness,
        ProjectionLifecycleState lifecycle = ProjectionLifecycleState.Unknown,
        string? projectionVersion = null)
        => FromComposition(
            TenantDetailSurfaceKind.Ready,
            UnavailableComposition(detail),
            eTag,
            freshness,
            lifecycle,
            projectionVersion,
            null);

    internal static TenantDetailSnapshot Ready(
        TenantConfigurationComposition composition,
        string? eTag,
        ReadModelFreshnessState freshness,
        ProjectionLifecycleState lifecycle = ProjectionLifecycleState.Unknown,
        string? projectionVersion = null)
        => FromComposition(TenantDetailSurfaceKind.Ready, composition, eTag, freshness, lifecycle, projectionVersion, null);

    internal static TenantDetailSnapshot Stale(
        TenantDetailContract detail,
        string? eTag,
        ProjectionLifecycleState lifecycle = ProjectionLifecycleState.Stale,
        string? projectionVersion = null)
        => FromComposition(
            TenantDetailSurfaceKind.Stale,
            UnavailableComposition(detail),
            eTag,
            ReadModelFreshnessState.Stale,
            lifecycle,
            projectionVersion,
            null);

    internal static TenantDetailSnapshot Stale(
        TenantConfigurationComposition composition,
        string? eTag,
        ProjectionLifecycleState lifecycle = ProjectionLifecycleState.Stale,
        string? projectionVersion = null)
        => FromComposition(
            TenantDetailSurfaceKind.Stale,
            composition,
            eTag,
            ReadModelFreshnessState.Stale,
            lifecycle,
            projectionVersion,
            null);

    internal static TenantDetailSnapshot Degraded(
        TenantDetailContract? detail,
        string message,
        string? eTag = null,
        ProjectionLifecycleState lifecycle = ProjectionLifecycleState.Unknown,
        string? projectionVersion = null)
        => detail is null
            ? Empty(TenantDetailSurfaceKind.Degraded, message, eTag, lifecycle)
            : FromComposition(
                TenantDetailSurfaceKind.Degraded,
                UnavailableComposition(detail),
                eTag,
                ReadModelFreshnessState.Unknown,
                lifecycle,
                projectionVersion,
                message);

    internal static TenantDetailSnapshot DegradedFromComposition(
        TenantConfigurationComposition composition,
        string message,
        string? eTag = null,
        ProjectionLifecycleState lifecycle = ProjectionLifecycleState.Unknown,
        string? projectionVersion = null)
        => FromComposition(
            TenantDetailSurfaceKind.Degraded,
            composition,
            eTag,
            ReadModelFreshnessState.Unknown,
            lifecycle,
            projectionVersion,
            message);

    internal static TenantDetailSnapshot Unknown(string message, string? eTag = null)
        => Empty(TenantDetailSurfaceKind.Unknown, message, eTag);

    internal static TenantDetailSnapshot Unavailable(
        string message,
        ProjectionLifecycleState lifecycle = ProjectionLifecycleState.Unknown)
        => Empty(TenantDetailSurfaceKind.Unavailable, message, lifecycle: lifecycle);

    internal static TenantDetailSnapshot NotFound(string tenantId)
        => Empty(TenantDetailSurfaceKind.NotFound, tenantId);

    internal static TenantDetailSnapshot Unauthorized(string tenantId)
        => Empty(TenantDetailSurfaceKind.Unauthorized, tenantId);

    private static TenantDetailSnapshot FromComposition(
        TenantDetailSurfaceKind kind,
        TenantConfigurationComposition composition,
        string? eTag,
        ReadModelFreshnessState freshness,
        ProjectionLifecycleState lifecycle,
        string? projectionVersion,
        string? message)
        => new(
            kind,
            composition.SanitizedDetail,
            eTag,
            freshness,
            lifecycle,
            projectionVersion,
            message,
            composition.SafeModel,
            composition.ManagementContext);

    private static TenantDetailSnapshot Empty(
        TenantDetailSurfaceKind kind,
        string? message,
        string? eTag = null,
        ProjectionLifecycleState lifecycle = ProjectionLifecycleState.Unknown)
        => new(
            kind,
            null,
            eTag,
            ReadModelFreshnessState.Unknown,
            lifecycle,
            null,
            message,
            TenantConfigurationSafeModel.Unavailable(string.Empty),
            TenantConfigurationManagementContext.Unavailable(string.Empty));

    private static TenantConfigurationComposition UnavailableComposition(TenantDetailContract detail)
        => new(
            TenantConfigurationSafeComposer.SanitizeDetail(detail),
            TenantConfigurationSafeModel.Unavailable(detail.TenantId),
            TenantConfigurationManagementContext.Unavailable(detail.TenantId, detail.Status));
}
