using System.Text.Json;
using Hexalith.EventStore.Client.Projections;

namespace Hexalith.Tenants.Server.Tests.Routing;

internal sealed class CatalogReadModelStore : IReadModelStore
{
    internal Dictionary<string, byte[]> Contents { get; } = new(StringComparer.Ordinal);
    public Task<ReadModelEntry<T>> GetAsync<T>(string storeName, string key, CancellationToken cancellationToken = default) where T : class =>
        Task.FromResult(new ReadModelEntry<T>(Contents.TryGetValue(key, out byte[]? bytes) ? JsonSerializer.Deserialize<T>(bytes) : null, Contents.ContainsKey(key) ? "etag" : null));
    public Task SaveAsync<T>(string storeName, string key, T value, CancellationToken cancellationToken = default) where T : class
    {
        Contents[key] = JsonSerializer.SerializeToUtf8Bytes(value);
        return Task.CompletedTask;
    }
    public async Task<bool> TrySaveAsync<T>(string storeName, string key, T value, string etag, CancellationToken cancellationToken = default) where T : class
    {
        await SaveAsync(storeName, key, value, cancellationToken);
        return true;
    }
}
