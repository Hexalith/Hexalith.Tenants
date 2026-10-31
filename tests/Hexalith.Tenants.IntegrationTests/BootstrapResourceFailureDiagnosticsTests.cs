using System.Security.Cryptography;
using System.Text.Json;

using Aspire.Hosting.ApplicationModel;
using Hexalith.Tenants.IntegrationTests.Fixtures;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

public sealed class BootstrapResourceFailureDiagnosticsTests
{
    [Fact]
    public void CategoriesRetainPublicFailureIdentityWithoutRawMessages()
    {
        var observed = BootstrapResourceFailureDiagnostics.Classify(["System.MissingMethodException: Method not found: Hexalith.EventStore.Client.Routing.PublicApi.Read", "Route catalog refusal: Reason=configuration-missing"]);
        observed.ExceptionTypes.ShouldContain("System.MissingMethodException");
        observed.ApiSymbols.ShouldContain("Hexalith.EventStore.Client.Routing.PublicApi.Read");
        observed.CatalogReasons.ShouldBe(["configuration-missing"]);
        observed.CapturedLineCount.ShouldBe(2);
    }

    [Fact]
    public void CredentialBearingLinesAndOversizedInputCannotBeRetained()
    {
        string credential = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        var observed = BootstrapResourceFailureDiagnostics.Classify(["System.InvalidOperationException: Authorization: Bearer " + credential, "System.InvalidOperationException: password=" + credential, new string('x', 8193)]);
        observed.ExceptionTypes.ShouldBeEmpty();
        observed.ApiSymbols.ShouldBeEmpty();
        observed.Truncated.ShouldBeTrue();
        JsonSerializer.Serialize(observed).ShouldNotContain(credential);
    }

    [Fact]
    public async Task ActualAspireLogReaderClassifiesCompletedStreamsAndHonorsCancellation()
    {
        using ResourceLoggerService logger = new();
        foreach (string resource in new[] { "eventstore", "tenants", "tenants-api" })
        {
            logger.GetLogger(resource).LogError(new MissingMethodException("Hexalith.EventStore.Client.Routing.PublicApi.Read"), "bootstrap failed");
            logger.Complete(resource);
        }
        // A standalone logger has no disk-backed GetAllAsync service until an AppHost starts.
        // Completed real Aspire streams exercise the same bounded consumer without starting resources.
        using CancellationTokenSource bound = new(TimeSpan.FromSeconds(2));
        var observed = await BootstrapResourceFailureDiagnostics.ReadAsync(logger.WatchAsync, bound.Token);
        observed.Count.ShouldBe(3);
        observed.Values.ShouldAllBe(value => value.ExceptionTypes.Contains("System.MissingMethodException") && value.CapturedLineCount > 0);
        await bound.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => BootstrapResourceFailureDiagnostics.ReadAsync(logger.WatchAsync, bound.Token));
    }
}
