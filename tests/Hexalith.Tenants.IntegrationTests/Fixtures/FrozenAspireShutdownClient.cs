namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>A shutdown-only owned client that keeps the frozen disposer behind a completed application stop.</summary>
internal sealed class FrozenAspireShutdownClient : HttpClient
{
    private readonly object _sync = new();
    private readonly Func<CancellationToken, Task> _stopApplication;
    private Task? _stop;

    internal FrozenAspireShutdownClient(Func<CancellationToken, Task> stopApplication)
    {
        _stopApplication = stopApplication;
    }

    internal Task StopAsync()
    {
        lock (_sync)
        {
            // The legacy client-disposal phase is synchronous. Run the one stop task
            // independently of the caller's synchronization context; normal module
            // disposal awaits this same task asynchronously before entering that phase.
            return _stop ??= Task.Run(async () =>
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                try
                {
                    await _stopApplication(timeout.Token).WaitAsync(timeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (timeout.IsCancellationRequested)
                {
                    throw new InvalidOperationException("frozen-aspire-stop-timeout");
                }
                catch
                {
                    // Native test artifacts retain exception text. Never attach the
                    // original exception, which can contain private topology or secrets.
                    throw new InvalidOperationException("frozen-aspire-stop-failed");
                }
            });
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // A failed stop must prevent the package from disposing the application,
            // builder or fixture lock. In particular, do not put base.Dispose in finally.
            StopAsync().GetAwaiter().GetResult();
        }

        base.Dispose(disposing);
    }
}
