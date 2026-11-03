using System.Globalization;
using System.Text.Json;

using Hexalith.EventStore.ServiceDefaults.Routing;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Isolates catalog activation with serialized state and optimistic compare-and-exchange.</summary>
/// <param name="coordinates">Optional independently selected coordinates retained by transport fixtures.</param>
internal sealed class TenantOwnerCatalogTestStateStore(RouteCatalogStateCoordinates? coordinates = null) : IRouteCatalogStateStore
{
    private readonly object _gate = new();
    private byte[] _state = JsonSerializer.SerializeToUtf8Bytes(RouteCatalogActivationState.Empty);
    private long _version;

    /// <inheritdoc/>
    public RouteCatalogStateCoordinates Coordinates { get; } = coordinates ?? new("route-catalog", "isolated-owner-authority-v1", "http://127.0.0.1:3500/");

    /// <inheritdoc/>
    public Task<(RouteCatalogActivationState State, string ETag)> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult((JsonSerializer.Deserialize<RouteCatalogActivationState>(_state)!, _version.ToString(CultureInfo.InvariantCulture)));
        }
    }

    /// <inheritdoc/>
    public Task<bool> CompareExchangeAsync(RouteCatalogActivationState state, string etag, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (etag != _version.ToString(CultureInfo.InvariantCulture))
            {
                return Task.FromResult(false);
            }
            _state = JsonSerializer.SerializeToUtf8Bytes(state);
            _version++;
            return Task.FromResult(true);
        }
    }
}
