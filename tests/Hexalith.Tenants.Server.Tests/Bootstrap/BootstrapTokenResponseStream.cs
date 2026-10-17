namespace Hexalith.Tenants.Server.Tests.Bootstrap;

/// <summary>
/// Provides a nonseekable authority response with observable, bounded read chunks.
/// </summary>
/// <param name="body">The response bytes.</param>
/// <param name="blockReads">Whether asynchronous reads wait for cancellation.</param>
/// <param name="readChunkSize">The maximum number of bytes returned by one read.</param>
internal sealed class BootstrapTokenResponseStream(byte[] body, bool blockReads = false, int readChunkSize = 4096) : Stream
{
    private readonly MemoryStream _body = new(body, writable: false);
    private readonly TaskCompletionSource _readStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets the number of response bytes consumed.</summary>
    internal int BytesRead { get; private set; }

    /// <summary>Gets whether the response stream has been disposed.</summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>Gets a task that completes when an asynchronous body read starts.</summary>
    internal Task ReadStarted => _readStarted.Task;

    /// <inheritdoc/>
    public override bool CanRead => !IsDisposed;

    /// <inheritdoc/>
    public override bool CanSeek => false;

    /// <inheritdoc/>
    public override bool CanWrite => false;

    /// <inheritdoc/>
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc/>
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        int read = _body.Read(buffer, offset, Math.Min(count, readChunkSize));
        BytesRead += read;
        return read;
    }

    /// <inheritdoc/>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    /// <inheritdoc/>
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        _readStarted.TrySetResult();
        cancellationToken.ThrowIfCancellationRequested();
        if (blockReads)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        }

        int read = _body.Read(buffer.Span[..Math.Min(buffer.Length, readChunkSize)]);
        BytesRead += read;
        return read;
    }

    /// <inheritdoc/>
    public override void Flush() => throw new NotSupportedException();

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            IsDisposed = true;
            _body.Dispose();
        }

        base.Dispose(disposing);
    }
}
