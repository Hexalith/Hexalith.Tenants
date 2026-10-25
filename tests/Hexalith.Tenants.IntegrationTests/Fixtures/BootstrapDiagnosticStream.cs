namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Counts actual streamed reads, including an accidental eager HttpClient buffer operation.</summary>
/// <param name="bytes">The diagnostic response bytes.</param>
internal sealed class BootstrapDiagnosticStream(byte[] bytes) : MemoryStream(bytes)
{
    /// <summary>Gets the number of body bytes consumed by the complete HTTP path.</summary>
    internal int BytesRead { get; private set; }

    /// <inheritdoc/>
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int count = await base.ReadAsync(buffer, cancellationToken);
        BytesRead += count;
        return count;
    }

    /// <inheritdoc/>
    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        BytesRead += checked((int)(Length - Position));
        return base.CopyToAsync(destination, bufferSize, cancellationToken);
    }
}
