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
        string runtimeHome = Path.Combine(Path.GetTempPath(), "tenants-identity-runtime-" + Guid.NewGuid().ToString("N"));
        IResourceBuilder<ProjectResource> application = builder.AddProject("identity-probe", project)
            .WithDaprSidecar(sidecar => sidecar.WithOptions(new DaprSidecarOptions { AppId = "identity-probe", AppProtocol = "https", RuntimePath = runtimeHome }));
        var owned = DaprSelfHostedMtls.AddLocalApplicationInfrastructure(builder, repository.FullName,
            Path.Combine(repository.FullName, "src/Hexalith.Tenants.AppHost/DaprComponents/sentry.yaml"));
        try
        {
            builder.Configuration["DcpPublisher:CliPath"] = Path.Combine(owned.RunDirectory, "model-dcp-not-started");
            builder.Configuration["DcpPublisher:DashboardPath"] = Path.Combine(owned.RunDirectory, "model-dashboard-not-started");
            string runtime = Path.Combine(runtimeHome, ".dapr", "bin", OperatingSystem.IsWindows() ? "daprd.exe" : "daprd");
            Directory.CreateDirectory(Path.GetDirectoryName(runtime)!);
            if (!missingRuntime) await File.WriteAllTextAsync(runtime, "model-only native runtime");
            foreach (string name in new[] { "ca.crt", "issuer.crt", "issuer.key" })
                await File.WriteAllTextAsync(Path.Combine(owned.CertificateDirectory, name), "model-only-" + name);
            IDaprSidecarResource declared = application.Resource.Annotations.OfType<DaprSidecarAnnotation>().Single().Sidecar;
            DaprSidecarOptionsAnnotation options = declared.Annotations.OfType<DaprSidecarOptionsAnnotation>().Single();
            options.Options.RuntimePath.ShouldBe(runtimeHome);

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
            var environment = new EnvironmentCallbackContext(builder.ExecutionContext, actual,
                new Dictionary<string, object>(), TestContext.Current.CancellationToken);
            foreach (EnvironmentCallbackAnnotation callback in actual.Annotations.OfType<EnvironmentCallbackAnnotation>())
                await callback.Callback(environment);
            environment.EnvironmentVariables["DAPR_TRUST_ANCHORS"].ShouldBe("model-only-ca.crt");
            environment.EnvironmentVariables["DAPR_CERT_CHAIN"].ShouldBe("model-only-issuer.crt");
            environment.EnvironmentVariables["DAPR_CERT_KEY"].ShouldBe("model-only-issuer.key");
            environment.EnvironmentVariables["NAMESPACE"].ShouldBe("default");
            environment.EnvironmentVariables["DAPR_CONTROLPLANE_NAMESPACE"].ShouldBe("default");
            environment.EnvironmentVariables["DAPR_CONTROLPLANE_TRUST_DOMAIN"].ShouldBe("localhost");
        }
        finally
        {
            await CompositionWorkspace.TryDeleteAsync(owned.CertificateDirectory, CancellationToken.None);
            await CompositionWorkspace.TryDeleteAsync(owned.RunDirectory, CancellationToken.None);
            await CompositionWorkspace.TryDeleteAsync(runtimeHome, CancellationToken.None);
        }
    }
    [Fact]
    public void FailedCompositionRemovesItsRunAndSeparateCertificateDirectories()
    {
        string certificateParent = Path.Combine(Path.GetTempPath(), "tenants-failed-identity-" + Guid.NewGuid().ToString("N"));
        string runs = Path.Combine(Path.GetTempPath(), "hexalith-dapr");
        string[] before = Directory.Exists(runs) ? Directory.GetDirectories(runs) : [];
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = [], DisableDashboard = true, AllowUnsecuredTransport = true,
            AssemblyName = typeof(LocalDaprIdentityTests).Assembly.GetName().Name,
        });
        builder.AddDapr(options => options.EnableTelemetry = false);
        builder.Configuration["Dapr:Mtls:CertificateDirectory"] = certificateParent;
        DirectoryInfo repository = new(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(repository.FullName, "Hexalith.Tenants.slnx")))
            repository = repository.Parent ?? throw new DirectoryNotFoundException("Tenants checkout is required.");
        builder.AddProject("identity-probe", Path.Combine(repository.FullName, "src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj"))
            .WithDaprSidecar();
        // Fail after AddSentry creates its owned child directory, before the tuple can be returned.
        builder.AddContainer("dapr-sentry", "model-only-collision");
        try
        {
            Should.Throw<DistributedApplicationException>(() => DaprSelfHostedMtls.AddLocalApplicationInfrastructure(
                builder, repository.FullName, Path.Combine(repository.FullName, "src/Hexalith.Tenants.AppHost/DaprComponents/sentry.yaml")));
            Directory.Exists(certificateParent).ShouldBeTrue();
            Directory.GetFileSystemEntries(certificateParent).ShouldBeEmpty();
            Directory.GetDirectories(runs).Except(before, StringComparer.Ordinal).ShouldBeEmpty();
        }
        finally
        {
            if (Directory.Exists(certificateParent)) Directory.Delete(certificateParent, recursive: true);
        }
    }

}
