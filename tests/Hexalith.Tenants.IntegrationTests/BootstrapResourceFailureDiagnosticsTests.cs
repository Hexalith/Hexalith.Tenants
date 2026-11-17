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

    [Fact]
    public void RenderedEnrollmentRetainsOnlyPresenceAndProgressWithoutEnvironmentOrFileContents()
    {
        string root = Directory.CreateTempSubdirectory("tenants-enrollment-test-").FullName;
        string directory = Path.Combine(root, "hexalith-route-catalog-owned");
        Directory.CreateDirectory(directory);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        string credential = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        try
        {
            foreach (string name in new[] { "catalog.json", "authority.backend.json", "eventstore.configuration.json",
                "eventstore.observation.json", "tenants.observation.json", "tenants-api.observation.json", "eventstore-admin.observation.json",
                "eventstore.ready.json", "activation.json", "tenants.bootstrap.json" })
                File.WriteAllText(Path.Combine(directory, name), "Bearer " + credential);
            CustomResourceSnapshot snapshot = EnrollmentSnapshot(directory, credential);
            var observation = BootstrapResourceFailureDiagnostics.ClassifyEnrollment(snapshot, "eventstore", root);
            observation.SnapshotObserved.ShouldBeTrue();
            observation.EnvironmentObserved.ShouldBeTrue();
            observation.DevelopmentEnvironment.ShouldBeTrue();
            observation.DirectoryConfigured.ShouldBeTrue();
            observation.CatalogConfigured.ShouldBeTrue();
            observation.ComponentConfigured.ShouldBeTrue();
            observation.AccessControlConfigured.ShouldBeTrue();
            observation.TechnicalBindingV2.ShouldBeTrue();
            observation.HostIdentityMatches.ShouldBeTrue();
            observation.PrivateDirectoryObserved.ShouldBeTrue();
            observation.CatalogFilePresent.ShouldBeTrue();
            observation.ComponentFilePresent.ShouldBeTrue();
            observation.AccessControlFilePresent.ShouldBeTrue();
            observation.OwnObservationPresent.ShouldBeTrue();
            observation.ObservedHostCount.ShouldBe(4);
            observation.ReadyHostCount.ShouldBe(1);
            observation.CommittedHostCount.ShouldBe(0);
            observation.ActivationPresent.ShouldBeTrue();
            observation.BootstrapReceiptPresent.ShouldBeTrue();
            string json = JsonSerializer.Serialize(observation);
            json.ShouldNotContain(credential);
            json.ShouldNotContain(root);
            json.ShouldNotContain("ACTIONS_ID_TOKEN_REQUEST_TOKEN");
            json.ShouldNotContain("Bearer");
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void EnrollmentNeverCountsFilesFromAnotherDirectoryOrLinkedInputs()
    {
        string root = Directory.CreateTempSubdirectory("tenants-enrollment-boundary-").FullName;
        string privateRoot = Path.Combine(root, "profile");
        string directory = Path.Combine(privateRoot, "hexalith-route-catalog-owned");
        string foreign = Path.Combine(root, "hexalith-route-catalog-foreign");
        Directory.CreateDirectory(directory);
        Directory.CreateDirectory(foreign);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        try
        {
            File.WriteAllText(Path.Combine(foreign, "eventstore.observation.json"), "foreign credential");
            File.CreateSymbolicLink(Path.Combine(directory, "eventstore.observation.json"), Path.Combine(foreign, "eventstore.observation.json"));
            string link = Path.Combine(privateRoot, "hexalith-route-catalog-link");
            Directory.CreateSymbolicLink(link, foreign);
            foreach (string selected in new[] { directory, foreign, link })
            {
                var observation = BootstrapResourceFailureDiagnostics.ClassifyEnrollment(EnrollmentSnapshot(selected, "sentinel"), "eventstore", privateRoot);
                observation.OwnObservationPresent.ShouldBeFalse();
                observation.ObservedHostCount.ShouldBe(0);
                observation.ReadyHostCount.ShouldBe(0);
                observation.CommittedHostCount.ShouldBe(0);
                observation.PrivateDirectoryObserved.ShouldBe(selected == directory);
            }
            File.ReadAllText(Path.Combine(foreign, "eventstore.observation.json")).ShouldBe("foreign credential");
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task SnapshotReaderUsesAllocatedInstancesAndMarksBoundedPartialObservationExplicitly()
    {
        async IAsyncEnumerable<ResourceEvent> Complete()
        {
            yield return new(new ContainerResource("unrelated"), "private-allocated-name", EnrollmentSnapshot("/private", "credential"));
            yield return new(new ContainerResource("eventstore"), "eventstore", new() { ResourceType = "project", Properties = [] });
            foreach (string name in new[] { "eventstore", "tenants", "tenants-api" })
            {
                await Task.Yield();
                yield return new(new ContainerResource(name), name + "-private-instance", EnrollmentSnapshot("/private", "credential"));
            }
        }
        using CancellationTokenSource bound = new(TimeSpan.FromSeconds(2));
        var complete = await BootstrapResourceFailureDiagnostics.ReadEnrollmentAsync(Complete(), Path.GetTempPath(), bound.Token);
        complete.Count.ShouldBe(3);
        complete.Values.ShouldAllBe(value => value.SnapshotObserved && value.EnvironmentObserved && !value.PrivateDirectoryObserved);
        JsonSerializer.Serialize(complete).ShouldNotContain("private-instance");
        JsonSerializer.Serialize(complete).ShouldNotContain("credential");

        using CancellationTokenSource partialBound = new(TimeSpan.FromMilliseconds(100));
        async IAsyncEnumerable<ResourceEvent> Partial()
        {
            yield return new(new ContainerResource("eventstore"), "allocated", EnrollmentSnapshot("/private", "credential"));
            await Task.Delay(Timeout.Infinite, partialBound.Token);
        }
        var partial = await BootstrapResourceFailureDiagnostics.ReadEnrollmentAsync(Partial(), Path.GetTempPath(), partialBound.Token);
        partial["eventstore"].EnvironmentObserved.ShouldBeTrue();
        partial["tenants"].SnapshotObserved.ShouldBeFalse();
        partial["tenants-api"].EnvironmentObserved.ShouldBeFalse();
        await Should.ThrowAsync<OperationCanceledException>(() => BootstrapResourceFailureDiagnostics.ReadEnrollmentAsync(Complete(), Path.GetTempPath(), partialBound.Token));
    }

    private static CustomResourceSnapshot EnrollmentSnapshot(string directory, string credential)
        => new()
        {
            ResourceType = "project", Properties = [],
            EnvironmentVariables = [
                new("DOTNET_ENVIRONMENT", "Development", true),
                new("EventStore__RouteCatalog__DevelopmentBootstrap__Directory", directory, true),
                new("EventStore__RouteCatalog__DevelopmentBootstrap__CatalogPath", Path.Combine(directory, "catalog.json"), true),
                new("EventStore__RouteCatalog__DevelopmentBootstrap__ComponentPath", Path.Combine(directory, "authority.backend.json"), true),
                new("EventStore__RouteCatalog__DevelopmentBootstrap__AccessControlPath", Path.Combine(directory, "eventstore.configuration.json"), true),
                new("EventStore__RouteCatalog__DevelopmentBootstrap__BindingVersion", "2", true),
                new("EventStore__RouteCatalog__DevelopmentBootstrap__HostId", "eventstore", true),
                new("ACTIONS_ID_TOKEN_REQUEST_TOKEN", credential, false),
                new("Authentication__SigningKey", credential, true),
            ],
        };
}
