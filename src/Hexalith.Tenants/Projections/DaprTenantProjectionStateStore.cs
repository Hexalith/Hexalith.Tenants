using Dapr.Client;

namespace Hexalith.Tenants.Projections;

internal sealed class DaprTenantProjectionStateStore(DaprClient daprClient) : ITenantProjectionStateStore {
    public async Task<ProjectionStateRead<TValue>> GetStateAndETagAsync<TValue>(
        string storeName,
        string key,
        CancellationToken cancellationToken = default)
        where TValue : class {
        (TValue value, string etag) = await daprClient
            .GetStateAndETagAsync<TValue>(storeName, key, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new ProjectionStateRead<TValue>(value, etag);
    }

    public async Task SaveStateAsync<TValue>(
        string storeName,
        string key,
        TValue value,
        StateOptions? stateOptions = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
        where TValue : class {
        await daprClient
            .SaveStateAsync(
                storeName,
                key,
                value,
                stateOptions ?? new StateOptions(),
                metadata,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> TrySaveStateAsync<TValue>(
        string storeName,
        string key,
        TValue value,
        string etag,
        StateOptions stateOptions,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
        where TValue : class =>
        await daprClient
            .TrySaveStateAsync(
                storeName,
                key,
                value,
                etag,
                stateOptions,
                metadata,
                cancellationToken)
            .ConfigureAwait(false);
}
