using Hexalith.EventStore.Client.Projections;

namespace Hexalith.Tenants.Server.Tests.Configuration;

/// <summary>Provides an empty read-model store for domain-service registration tests.</summary>
internal sealed class EmptyReadModelStore : IReadModelStore {
    /// <inheritdoc/>
    public Task<ReadModelEntry<TValue>> GetAsync<TValue>(
        string storeName,
        string key,
        CancellationToken cancellationToken = default)
        where TValue : class
        => Task.FromResult(new ReadModelEntry<TValue>(null, null));

    /// <inheritdoc/>
    public Task SaveAsync<TValue>(
        string storeName,
        string key,
        TValue value,
        CancellationToken cancellationToken = default)
        where TValue : class
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<bool> TrySaveAsync<TValue>(
        string storeName,
        string key,
        TValue value,
        string etag,
        CancellationToken cancellationToken = default)
        where TValue : class
        => Task.FromResult(true);
}
