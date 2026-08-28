using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.UI.State.GlobalAdministrators;

namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>
/// Loads the complete fixed global-administrator projection through its protected cursor pages.
/// </summary>
internal static class GlobalAdministratorsProjectionLoader
{
    /// <summary>Maximum pages read by a complete projection walk.</summary>
    internal const int DefaultMaximumPageCount = 50;

    /// <summary>
    /// Loads and aggregates a bounded, stable, current global-administrator projection.
    /// </summary>
    /// <param name="gateway">One-page query gateway.</param>
    /// <param name="initialRequest">First-page request whose page size and validator are preserved.</param>
    /// <param name="cancellationToken">Cancellation token propagated to every page read.</param>
    /// <param name="maximumPageCount">Maximum number of pages to read.</param>
    /// <returns>Complete evidence, or an explicitly incomplete aggregate when any invariant fails.</returns>
    internal static async Task<GlobalAdministratorsSnapshot> LoadAsync(
        ITenantQueryGateway gateway,
        GlobalAdministratorsRequest? initialRequest = null,
        CancellationToken cancellationToken = default,
        int maximumPageCount = DefaultMaximumPageCount)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumPageCount);

        GlobalAdministratorsRequest request = (initialRequest ?? new GlobalAdministratorsRequest()) with
        {
            Cursor = null,
        };
        var rows = new Dictionary<string, GlobalAdministratorRow>(StringComparer.Ordinal);
        var visitedCursors = new HashSet<string>(StringComparer.Ordinal);
        string? projectionVersion = null;
        GlobalAdministratorsSnapshot? page = null;

        for (int pageNumber = 0; pageNumber < maximumPageCount; pageNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            page = await gateway
                .GetGlobalAdministratorsAsync(request, previous: null, cancellationToken)
                .ConfigureAwait(false);
            if (page is null)
            {
                return ToIncomplete(GlobalAdministratorsSnapshot.Invalid());
            }

            if (page.Kind is GlobalAdministratorsSurfaceKind.Unauthorized)
            {
                return GlobalAdministratorsSnapshot.Unauthorized();
            }

            if (!IsCurrentStablePage(page, request, projectionVersion))
            {
                return ToIncomplete(page);
            }

            projectionVersion ??= page.ProjectionVersion;
            foreach (GlobalAdministratorRow row in page.Rows)
            {
                if (!rows.TryAdd(row.UserId, row))
                {
                    return ToIncomplete(page);
                }
            }

            if (!page.HasMore)
            {
                return page with
                {
                    Kind = rows.Count == 0
                        ? GlobalAdministratorsSurfaceKind.Empty
                        : GlobalAdministratorsSurfaceKind.Ready,
                    Rows = rows.Values.ToArray(),
                    NextCursor = null,
                    HasMore = false,
                    ETag = null,
                    IsAuthorizationScopedEmpty = rows.Count == 0,
                    IsCompleteEvidence = true,
                    PagingRecovered = false,
                    RequestCursor = null,
                };
            }

            if (string.IsNullOrWhiteSpace(page.NextCursor)
                || !visitedCursors.Add(page.NextCursor))
            {
                return ToIncomplete(page);
            }

            request = request with
            {
                Cursor = page.NextCursor,
                ETag = null,
            };
        }

        return ToIncomplete(page ?? GlobalAdministratorsSnapshot.Unavailable());
    }

    private static bool IsCurrentStablePage(
        GlobalAdministratorsSnapshot page,
        GlobalAdministratorsRequest request,
        string? projectionVersion)
        => page.Kind is GlobalAdministratorsSurfaceKind.Ready or GlobalAdministratorsSurfaceKind.Empty
            && page.Freshness is ReadModelFreshnessState.Current
            && page.Lifecycle is ProjectionLifecycleState.Current
            && !page.PagingRecovered
            && page.Rows is not null
            && page.Rows.Count <= request.PageSize
            && page.RequestPageSize == request.PageSize
            && string.Equals(page.RequestCursor, request.Cursor, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(page.ProjectionVersion)
            && (projectionVersion is null
                || string.Equals(projectionVersion, page.ProjectionVersion, StringComparison.Ordinal))
            && (page.Kind is not GlobalAdministratorsSurfaceKind.Empty
                || page.IsAuthorizationScopedEmpty
                    && page.Rows.Count == 0
                    && !page.HasMore
                    && string.IsNullOrWhiteSpace(page.NextCursor))
            && (page.Kind is not GlobalAdministratorsSurfaceKind.Ready
                || !page.IsAuthorizationScopedEmpty && page.Rows.Count > 0)
            && (page.HasMore
                ? !string.IsNullOrWhiteSpace(page.NextCursor)
                : string.IsNullOrWhiteSpace(page.NextCursor))
            && page.Rows.All(static row =>
                row is not null
                && !string.IsNullOrWhiteSpace(row.UserId)
                && !row.UserId.Any(char.IsControl)
                && row.Freshness is ReadModelFreshnessState.Current
                && row.Lifecycle is ProjectionLifecycleState.Current);

    private static GlobalAdministratorsSnapshot ToIncomplete(GlobalAdministratorsSnapshot snapshot)
        => GlobalAdministratorsSnapshot.Unavailable(
            snapshot.Reason is GlobalAdministratorsReason.None
                ? GlobalAdministratorsReason.GatewayFailure
                : snapshot.Reason);
}
