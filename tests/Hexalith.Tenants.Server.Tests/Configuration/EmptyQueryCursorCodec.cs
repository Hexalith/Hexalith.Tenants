using Hexalith.EventStore.Client.Queries;

namespace Hexalith.Tenants.Server.Tests.Configuration;

/// <summary>Provides a deterministic no-op query cursor codec for registration tests.</summary>
internal sealed class EmptyQueryCursorCodec : IQueryCursorCodec {
    /// <inheritdoc/>
    public string Encode(string queryType, string scope, string position) => position;

    /// <inheritdoc/>
    public bool TryDecode(
        string? cursor,
        string queryType,
        string scope,
        out string? position,
        out string? failureReason) {
        position = null;
        failureReason = null;
        return true;
    }
}
