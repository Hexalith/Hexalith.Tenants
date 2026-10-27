using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.Builds.Tooling.Runtime;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.Tenants.AppHost.Tests;

/// <summary>Exercises the shared Platform bootstrap through Aspire's actual generated-sidecar callbacks.</summary>
public sealed class LocalDaprIdentityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GeneratedSidecarUsesIsolatedMutualTlsOrRefusesMissingRuntime(bool missingRuntime)
    {
        DirectoryInfo repository = new(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(repository.FullName, "Hexalith.Tenants.slnx")))
            repository = repository.Parent ?? throw new DirectoryNotFoundException("Tenants checkout is required.");
        string project = Path.Combine(repository.FullName, "src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj");
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = [], DisableDashboard = true, AllowUnsecuredTransport = true,
            AssemblyName = typeof(LocalDaprIdentityTests).Assembly.GetName().Name,
        });
        builder.AddDapr(options => options.EnableTelemetry = false);
        IResourceBuilder<ProjectResource> application = builder.AddProject("identity-probe", project)
            .WithDaprSidecar(sidecar => sidecar.WithOptions(new DaprSidecarOptions { AppId = "identity-probe", AppProtocol = "https" }));
        var owned = DaprSelfHostedMtls.AddLocalApplicationInfrastructure(builder, repository.FullName,
            Path.Combine(repository.FullName, "src/Hexalith.Tenants.AppHost/DaprComponents/sentry.yaml"));
        try
        {
            builder.Configuration["DcpPublisher:CliPath"] = Path.Combine(owned.RunDirectory, "model-dcp-not-started");
            builder.Configuration["DcpPublisher:DashboardPath"] = Path.Combine(owned.RunDirectory, "model-dashboard-not-started");
            string runtimeHome = Path.Combine(owned.RunDirectory, "runtime");
            string runtime = Path.Combine(runtimeHome, ".dapr", "bin", OperatingSystem.IsWindows() ? "daprd.exe" : "daprd");
            Directory.CreateDirectory(Path.GetDirectoryName(runtime)!);
            if (!missingRuntime) await File.WriteAllTextAsync(runtime, "model-only native runtime");
            foreach (string name in new[] { "ca.crt", "issuer.crt", "issuer.key" })
                await File.WriteAllTextAsync(Path.Combine(owned.CertificateDirectory, name), "model-only-" + name);
            IDaprSidecarResource declared = application.Resource.Annotations.OfType<DaprSidecarAnnotation>().Single().Sidecar;
            DaprSidecarOptionsAnnotation options = declared.Annotations.OfType<DaprSidecarOptionsAnnotation>().Single();
            declared.Annotations[declared.Annotations.IndexOf(options)] = new DaprSidecarOptionsAnnotation(options.Options with { RuntimePath = runtimeHome });

            await using DistributedApplication app = builder.Build();
            DistributedApplicationModel model = app.Services.GetRequiredService<DistributedApplicationModel>();
            IDistributedApplicationEventing eventing = app.Services.GetRequiredService<IDistributedApplicationEventing>();
            foreach (IDistributedApplicationEventingSubscriber subscriber in app.Services.GetServices<IDistributedApplicationEventingSubscriber>())
                await subscriber.SubscribeAsync(eventing, builder.ExecutionContext, TestContext.Current.CancellationToken);
            if (missingRuntime)
            {
                await Should.ThrowAsync<InvalidDataException>(() => eventing.PublishAsync(new BeforeStartEvent(app.Services, model), TestContext.Current.CancellationToken));
                return;
            }
            await eventing.PublishAsync(new BeforeStartEvent(app.Services, model), TestContext.Current.CancellationToken);
            foreach (IResource resource in model.Resources)
            foreach (EndpointAnnotation endpoint in resource.Annotations.OfType<EndpointAnnotation>())
                endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "localhost", endpoint.Port ?? endpoint.TargetPort ?? 32123);
            ExecutableResource actual = model.Resources.OfType<ExecutableResource>().Single(resource => resource.Name == "identity-probe-dapr-cli");
            actual.Annotations.OfType<ExecutableAnnotation>().Last().Command.ShouldBe(runtime);
            List<object> arguments = [];
            foreach (CommandLineArgsCallbackAnnotation callback in actual.Annotations.OfType<CommandLineArgsCallbackAnnotation>())
                await callback.Callback(new CommandLineArgsCallbackContext(arguments, TestContext.Current.CancellationToken));
            arguments.ShouldContain("--enable-mtls");
            arguments.ShouldNotContain("run");
            arguments.ShouldNotContain("--runtime-path");
            int sentryPort = model.Resources.Single(resource => resource.Name == "dapr-sentry").Annotations
                .OfType<EndpointAnnotation>().Single(endpoint => endpoint.Name == "grpc").Port!.Value;
            arguments[arguments.IndexOf("--sentry-address") + 1].ShouldBe("localhost:" + sentryPort);
            foreach (string name in new[] { "dapr-sentry", "dapr-placement-mtls", "dapr-scheduler-mtls" })
                model.Resources.ShouldContain(resource => resource.Name == name);
            options.Options.PlacementHostAddress.ShouldNotBeNull();
            options.Options.SchedulerHostAddress.ShouldNotBeNull();
            actual.Annotations.OfType<EndpointAnnotation>().ShouldAllBe(endpoint => endpoint.IsProxied == false);
        }
        finally
        {
            await CompositionWorkspace.TryDeleteAsync(owned.CertificateDirectory, CancellationToken.None);
            await CompositionWorkspace.TryDeleteAsync(owned.RunDirectory, CancellationToken.None);
        }
    }
}
