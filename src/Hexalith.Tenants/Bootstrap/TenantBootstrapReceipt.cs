using System.Text.Json;

using Microsoft.Extensions.Configuration;

namespace Hexalith.Tenants.Bootstrap;

/// <summary>Private delivery evidence for the existing bootstrap workflow; projected membership remains the authority.</summary>
internal sealed record TenantBootstrapReceipt(string AttemptId, string RootDigest, string MessageId,
    string CorrelationId, string PrincipalId, string Phase, int? HttpStatus)
{
    internal static async Task<TenantBootstrapReceipt?> BeginAsync(IConfiguration configuration, string messageId,
        string correlationId, string principalId, CancellationToken token)
    {
        string? path = ReceiptPath(configuration);
        if (path is null) return null;
        var receipt = new TenantBootstrapReceipt(configuration["EventStore:RouteCatalog:DevelopmentBootstrap:AttemptId"]!,
            configuration["EventStore:RouteCatalog:DevelopmentBootstrap:RootDigest"]!, messageId, correlationId, principalId, "attempted", null);
        if (string.IsNullOrWhiteSpace(receipt.AttemptId) || string.IsNullOrWhiteSpace(receipt.RootDigest))
            throw new InvalidOperationException("The bootstrap receipt requires the actual private catalog attempt.");
        await WriteNewAsync(path, JsonSerializer.SerializeToUtf8Bytes(receipt), token).ConfigureAwait(false);
        return receipt;
    }

    internal async Task CompleteAsync(IConfiguration configuration, int status, string phase, CancellationToken token)
    {
        string path = ReceiptPath(configuration) ?? throw new InvalidOperationException("The bootstrap receipt path changed.");
        RequirePrivate(path);
        byte[] original = JsonSerializer.SerializeToUtf8Bytes(this);
        if (new FileInfo(path).Length != original.Length || !(await File.ReadAllBytesAsync(path, token).ConfigureAwait(false)).AsSpan().SequenceEqual(original))
            throw new InvalidOperationException("The bootstrap delivery receipt changed; preserve it for recovery.");
        string pending = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        await WriteNewAsync(pending, JsonSerializer.SerializeToUtf8Bytes(this with { Phase = phase, HttpStatus = status }), token).ConfigureAwait(false);
        RequirePrivate(path);
        File.Move(pending, path, overwrite: true);
    }

    private static string? ReceiptPath(IConfiguration configuration)
    {
        if (configuration["Tenants:BootstrapBoundWorkloadReceiptPath"] is not { Length: > 0 } path) return null;
        string directory = configuration["EventStore:RouteCatalog:DevelopmentBootstrap:Directory"]
            ?? throw new InvalidOperationException("The bootstrap receipt has no actual private catalog directory.");
        if (!Path.IsPathFullyQualified(directory) || !Path.IsPathFullyQualified(path)
            || Path.GetFullPath(path) != Path.Combine(Path.GetFullPath(directory), "tenants.bootstrap.json"))
            throw new InvalidOperationException("The bootstrap receipt must remain in its actual private catalog directory.");
        RequirePrivate(directory, directory: true);
        return path;
    }

    private static void RequirePrivate(string path, bool directory = false)
    {
        FileSystemInfo entry = directory ? new DirectoryInfo(path) : new FileInfo(path);
        if (!entry.Exists || entry.LinkTarget is not null || (entry.Attributes & FileAttributes.ReparsePoint) != 0
            || !OperatingSystem.IsWindows() && (File.GetUnixFileMode(path) & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
                | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute)) != 0)
            throw new InvalidOperationException("The bootstrap receipt requires an owned regular private entry.");
    }

    private static async Task WriteNewAsync(string path, byte[] bytes, CancellationToken token)
    {
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.Read };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        var stream = new FileStream(path, options);
        await using (stream.ConfigureAwait(false))
            await stream.WriteAsync(bytes, token).ConfigureAwait(false);
    }
}
