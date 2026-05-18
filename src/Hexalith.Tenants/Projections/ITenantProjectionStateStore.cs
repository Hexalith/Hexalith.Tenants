using Dapr.Client;

namespace Hexalith.Tenants.Projections;

internal interface ITenantProjectionStateStore {
    Task<ProjectionStateRead<TValue>> GetStateAndETagAsync<TValue>(
        string storeName,
        string key,
        CancellationToken cancellationToken = default)
        where TValue : class;

    Task SaveStateAsync<TValue>(
        string storeName,
        string key,
        TValue value,
        StateOptions? stateOptions = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
        where TValue : class;

    Task<bool> TrySaveStateAsync<TValue>(
        string storeName,
        string key,
        TValue value,
        string etag,
        StateOptions stateOptions,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
        where TValue : class;
}

internal sealed record ProjectionStateRead<TValue>(TValue? Value, string? ETag)
    where TValue : class;
