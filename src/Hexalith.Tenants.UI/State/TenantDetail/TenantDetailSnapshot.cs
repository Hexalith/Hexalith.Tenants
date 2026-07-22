using Hexalith.EventStore.Client.Projections;
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
        string? errorMessage,
        TenantConfigurationSafeModel configuration,
        TenantConfigurationManagementContext configurationManagement)
    {
        Kind = kind;
        Detail = detail;
        ETag = eTag;
        Freshness = freshness;
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

    /// <summary>Gets support-safe error copy.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Gets positively approved configuration read state.</summary>
    public TenantConfigurationSafeModel Configuration { get; }

    /// <summary>Gets non-sensitive command scope and safe remove targets.</summary>
    public TenantConfigurationManagementContext ConfigurationManagement { get; }

    internal static TenantDetailSnapshot Loading()
        => Empty(TenantDetailSurfaceKind.Loading, null);

    internal static TenantDetailSnapshot Ready(
        TenantDetailContract detail,
        string? eTag,
        ReadModelFreshnessState freshness)
        => FromComposition(
            TenantDetailSurfaceKind.Ready,
            UnavailableComposition(detail),
            eTag,
            freshness,
            null);

    internal static TenantDetailSnapshot Ready(
        TenantConfigurationComposition composition,
        string? eTag,
        ReadModelFreshnessState freshness)
        => FromComposition(TenantDetailSurfaceKind.Ready, composition, eTag, freshness, null);

    internal static TenantDetailSnapshot Stale(TenantDetailContract detail, string? eTag)
        => FromComposition(
            TenantDetailSurfaceKind.Stale,
            UnavailableComposition(detail),
            eTag,
            ReadModelFreshnessState.Stale,
            null);

    internal static TenantDetailSnapshot Stale(TenantConfigurationComposition composition, string? eTag)
        => FromComposition(
            TenantDetailSurfaceKind.Stale,
            composition,
            eTag,
            ReadModelFreshnessState.Stale,
            null);

    internal static TenantDetailSnapshot Degraded(TenantDetailContract? detail, string message, string? eTag = null)
        => detail is null
            ? Empty(TenantDetailSurfaceKind.Degraded, message, eTag)
            : FromComposition(
                TenantDetailSurfaceKind.Degraded,
                UnavailableComposition(detail),
                eTag,
                ReadModelFreshnessState.Unknown,
                message);

    internal static TenantDetailSnapshot DegradedFromComposition(
        TenantConfigurationComposition composition,
        string message,
        string? eTag = null)
        => FromComposition(
            TenantDetailSurfaceKind.Degraded,
            composition,
            eTag,
            ReadModelFreshnessState.Unknown,
            message);

    internal static TenantDetailSnapshot Unknown(string message, string? eTag = null)
        => Empty(TenantDetailSurfaceKind.Unknown, message, eTag);

    internal static TenantDetailSnapshot Unavailable(string message)
        => Empty(TenantDetailSurfaceKind.Unavailable, message);

    internal static TenantDetailSnapshot NotFound(string tenantId)
        => Empty(TenantDetailSurfaceKind.NotFound, tenantId);

    internal static TenantDetailSnapshot Unauthorized(string tenantId)
        => Empty(TenantDetailSurfaceKind.Unauthorized, tenantId);

    private static TenantDetailSnapshot FromComposition(
        TenantDetailSurfaceKind kind,
        TenantConfigurationComposition composition,
        string? eTag,
        ReadModelFreshnessState freshness,
        string? message)
        => new(
            kind,
            composition.SanitizedDetail,
            eTag,
            freshness,
            message,
            composition.SafeModel,
            composition.ManagementContext);

    private static TenantDetailSnapshot Empty(
        TenantDetailSurfaceKind kind,
        string? message,
        string? eTag = null)
        => new(
            kind,
            null,
            eTag,
            ReadModelFreshnessState.Unknown,
            message,
            TenantConfigurationSafeModel.Unavailable(string.Empty),
            TenantConfigurationManagementContext.Unavailable(string.Empty));

    private static TenantConfigurationComposition UnavailableComposition(TenantDetailContract detail)
        => new(
            TenantConfigurationSafeComposer.SanitizeDetail(detail),
            TenantConfigurationSafeModel.Unavailable(detail.TenantId),
            TenantConfigurationManagementContext.Unavailable(detail.TenantId, detail.Status));
}
