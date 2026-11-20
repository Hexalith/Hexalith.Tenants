using System.Collections.Concurrent;
using System.Reflection;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using Hexalith.Tenants.IntegrationTests.Fixtures;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NSubstitute;
using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Exercises the Tenants harness disposal sequence without starting an AppHost or Dapr.</summary>
[Collection("BootstrapReadiness")]
public sealed class TenantsAspireLifecycleTests
{
    /// <summary>Both module disposal and the base lifecycle method used on startup failure await the owned application's stop.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnedDisposalWaitsForStopBeforeApplicationBuilderAndFixtureLockRelease(bool startupFailure)
    {
        string root = Directory.CreateTempSubdirectory("tenants-frozen-lifetime-").FullName;
        string? profile = Environment.GetEnvironmentVariable("TENANTS_SOURCE_CATALOG_PROFILE");
        string? trust = Environment.GetEnvironmentVariable("SSL_CERT_DIR");
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ConcurrentQueue<string> events = new();
        Task? disposing = null;
        try
        {
            Environment.SetEnvironmentVariable("TENANTS_SOURCE_CATALOG_PROFILE", "false");
            IHost host = Substitute.For<IHost, IAsyncDisposable>();
            using ServiceProvider services = CreateServices();
            host.Services.Returns(services);
            host.StopAsync(Arg.Any<CancellationToken>()).Returns(async call =>
            {
                call.Arg<CancellationToken>().CanBeCanceled.ShouldBeTrue();
                events.Enqueue("stop-started");
                started.TrySetResult();
                await finish.Task.WaitAsync(call.Arg<CancellationToken>());
                events.Enqueue("stop-finished");
            });
            ((IAsyncDisposable)host).DisposeAsync().Returns(_ => { events.Enqueue("application-disposed"); return ValueTask.CompletedTask; });
            IDistributedApplicationTestingBuilder builder = Substitute.For<IDistributedApplicationTestingBuilder>();
            builder.DisposeAsync().Returns(_ => { events.Enqueue("builder-disposed"); return ValueTask.CompletedTask; });
            var application = new DistributedApplication(host);
            var fixture = new AspireTopologyFixture();
            SetField(fixture, "_app", application);
            SetField(fixture, "_builder", builder);
            string lockPath = Path.Combine(root, "fixture.lock");
            using var fixtureLock = new FileStream(lockPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
            SetField(fixture, "_daprFixtureLock", fixtureLock);

            // Exercise the same cleanup entry point used by the startup failure path.
            disposing = Task.Run(async () =>
            {
                if (startupFailure) await ((TenantsAspireHarness<Projects.Hexalith_Tenants_AppHost>)fixture).DisposeAsync();
                else await fixture.DisposeAsync();
            });
            await started.Task.WaitAsync(TimeSpan.FromSeconds(3));
            events.ToArray().ShouldBe(["stop-started"]);
            disposing.IsCompleted.ShouldBeFalse();
            Should.Throw<IOException>(() => { using var held = File.Open(lockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None); });
            finish.TrySetResult();
            await disposing.WaitAsync(TimeSpan.FromSeconds(5));
            events.ToArray().ShouldBe(["stop-started", "stop-finished", "application-disposed", "builder-disposed"]);
            using var released = File.Open(lockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            await host.Received(1).StopAsync(Arg.Any<CancellationToken>());
        }
        finally
        {
            finish.TrySetResult();
            if (disposing is not null) await disposing.WaitAsync(TimeSpan.FromSeconds(5));
            Environment.SetEnvironmentVariable("TENANTS_SOURCE_CATALOG_PROFILE", profile);
            Environment.SetEnvironmentVariable("SSL_CERT_DIR", trust);
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>A stop refusal cannot advance application/builder disposal or release the private fixture lock.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedStopDoesNotReleaseOwnedApplicationBuilderOrFixtureLock(bool startupFailure)
    {
        const string sentinel = "credential-sentinel://private-stop?password=never-retain";
        string root = Directory.CreateTempSubdirectory("tenants-frozen-stop-refusal-").FullName;
        string? profile = Environment.GetEnvironmentVariable("TENANTS_SOURCE_CATALOG_PROFILE");
        string? trust = Environment.GetEnvironmentVariable("SSL_CERT_DIR");
        try
        {
            Environment.SetEnvironmentVariable("TENANTS_SOURCE_CATALOG_PROFILE", "false");
            IHost host = Substitute.For<IHost, IAsyncDisposable>();
            using ServiceProvider services = CreateServices();
            host.Services.Returns(services);
            host.StopAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException(sentinel)));
            IDistributedApplicationTestingBuilder builder = Substitute.For<IDistributedApplicationTestingBuilder>();
            var fixture = new AspireTopologyFixture();
            SetField(fixture, "_app", new DistributedApplication(host));
            SetField(fixture, "_builder", builder);
            string lockPath = Path.Combine(root, "fixture.lock");
            using var fixtureLock = new FileStream(lockPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
            SetField(fixture, "_daprFixtureLock", fixtureLock);
            Task disposing = Task.Run(async () =>
            {
                if (startupFailure) await ((TenantsAspireHarness<Projects.Hexalith_Tenants_AppHost>)fixture).DisposeAsync();
                else await fixture.DisposeAsync();
            });
            InvalidOperationException failure = await Should.ThrowAsync<InvalidOperationException>(() => disposing);
            failure.Message.ShouldBe("tenants-aspire-stop-failed");
            failure.Message.ShouldNotContain(sentinel);
            failure.ToString().ShouldNotContain(sentinel);
            failure.InnerException.ShouldBeNull();
            await ((IAsyncDisposable)host).DidNotReceive().DisposeAsync();
            await builder.DidNotReceive().DisposeAsync();
            Should.Throw<IOException>(() => { using var held = File.Open(lockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None); });
        }
        finally
        {
            Environment.SetEnvironmentVariable("TENANTS_SOURCE_CATALOG_PROFILE", profile);
            Environment.SetEnvironmentVariable("SSL_CERT_DIR", trust);
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>No owned application means no global application lookup or stop of another session.</summary>
    [Fact]
    public async Task EmptyFrozenFixtureDisposesItsBuilderWithoutInventingAnApplication()
    {
        string? profile = Environment.GetEnvironmentVariable("TENANTS_SOURCE_CATALOG_PROFILE");
        string? trust = Environment.GetEnvironmentVariable("SSL_CERT_DIR");
        try
        {
            Environment.SetEnvironmentVariable("TENANTS_SOURCE_CATALOG_PROFILE", "false");
            var fixture = new AspireTopologyFixture();
            IDistributedApplicationTestingBuilder builder = Substitute.For<IDistributedApplicationTestingBuilder>();
            SetField(fixture, "_builder", builder);
            await ((TenantsAspireHarness<Projects.Hexalith_Tenants_AppHost>)fixture).DisposeAsync();
            await builder.Received(1).DisposeAsync();
        }
        finally
        {
            Environment.SetEnvironmentVariable("TENANTS_SOURCE_CATALOG_PROFILE", profile);
            Environment.SetEnvironmentVariable("SSL_CERT_DIR", trust);
        }
    }

    private static ServiceProvider CreateServices()
        => new ServiceCollection().AddLogging()
            .AddSingleton<IHostApplicationLifetime>(Substitute.For<IHostApplicationLifetime>())
            .AddSingleton(new DistributedApplicationModel(Array.Empty<IResource>()))
            .AddSingleton<ResourceLoggerService>().AddSingleton<ResourceNotificationService>().BuildServiceProvider();

    private static void SetField(AspireTopologyFixture fixture, string name, object value)
        => typeof(TenantsAspireHarness<Projects.Hexalith_Tenants_AppHost>)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(fixture, value);
}
