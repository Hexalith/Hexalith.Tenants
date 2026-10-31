using System.Text.Json;

using Hexalith.Tenants.Bootstrap;

using Microsoft.Extensions.Configuration;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Bootstrap;

/// <summary>Private delivery evidence never overwrites an outside target or becomes domain authority.</summary>
public sealed class TenantBootstrapReceiptTests
{
    [Theory]
    [InlineData("valid")]
    [InlineData("duplicate")]
    [InlineData("begin-link")]
    [InlineData("complete-link")]
    [InlineData("changed")]
    [InlineData("outside-path")]
    public async Task ActualDeliveryReceiptPreservesOwnedBytesAndRefusesSubstitution(string scenario)
    {
        string directory = Directory.CreateTempSubdirectory("tenant-bootstrap-receipt-").FullName;
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        string path = Path.Combine(directory, "tenants.bootstrap.json");
        string outside = Path.Combine(Path.GetTempPath(), "tenant-outside-" + Guid.NewGuid().ToString("N"));
        byte[] outsideBytes = "outside-target-must-remain-unchanged"u8.ToArray(); await File.WriteAllBytesAsync(outside, outsideBytes, TestContext.Current.CancellationToken);
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tenants:BootstrapBoundWorkloadReceiptPath"] = scenario == "outside-path" ? outside : path,
            ["EventStore:RouteCatalog:DevelopmentBootstrap:Directory"] = directory,
            ["EventStore:RouteCatalog:DevelopmentBootstrap:AttemptId"] = "file-evidence-attempt",
            ["EventStore:RouteCatalog:DevelopmentBootstrap:RootDigest"] = new string('a', 64),
        }).Build();
        try
        {
            if (scenario == "begin-link") File.CreateSymbolicLink(path, outside);
            if (scenario is "begin-link" or "outside-path")
            {
                await Should.ThrowAsync<Exception>(() => TenantBootstrapReceipt.BeginAsync(configuration, "message-one", "correlation-one", "validated-administrator", TestContext.Current.CancellationToken));
            }
            else
            {
                var receipt = (await TenantBootstrapReceipt.BeginAsync(configuration, "message-one", "correlation-one", "validated-administrator", TestContext.Current.CancellationToken)).ShouldNotBeNull();
                byte[] original = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
                if (scenario == "duplicate")
                {
                    await Should.ThrowAsync<IOException>(() => TenantBootstrapReceipt.BeginAsync(configuration, "message-two", "correlation-two", "another", TestContext.Current.CancellationToken));
                    File.ReadAllBytes(path).ShouldBe(original);
                }
                else if (scenario is "complete-link" or "changed")
                {
                    if (scenario == "complete-link") { File.Delete(path); File.CreateSymbolicLink(path, outside); }
                    else await File.WriteAllTextAsync(path, "changed", TestContext.Current.CancellationToken);
                    await Should.ThrowAsync<InvalidOperationException>(() => receipt.CompleteAsync(configuration, 202, "accepted", TestContext.Current.CancellationToken));
                }
                else
                {
                    await receipt.CompleteAsync(configuration, 202, "accepted", TestContext.Current.CancellationToken);
                    var completed = JsonSerializer.Deserialize<TenantBootstrapReceipt>(await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken)).ShouldNotBeNull();
                    completed.Phase.ShouldBe("accepted"); completed.MessageId.ShouldBe(receipt.MessageId); completed.CorrelationId.ShouldBe(receipt.CorrelationId);
                    completed.PrincipalId.ShouldBe(receipt.PrincipalId); completed.HttpStatus.ShouldBe(202);
                    if (!OperatingSystem.IsWindows()) File.GetUnixFileMode(path).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);
                }
            }
            (await File.ReadAllBytesAsync(outside, TestContext.Current.CancellationToken)).ShouldBe(outsideBytes);
        }
        finally { Directory.Delete(directory, true); File.Delete(outside); }
    }
}
