namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Serializes local Dapr fixtures across test processes that share control-plane ports.</summary>
internal static class TenantsDaprFixtureLock
{
    internal static async Task<FileStream> AcquireAsync(string name, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(6));
        string path = Path.Combine(Path.GetTempPath(), name);
        while (true)
        {
            timeout.Token.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250), timeout.Token).ConfigureAwait(false);
            }
        }
    }
}
