#if HEXALITH_DEVELOPMENT_CATALOG_COHORT
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Hexalith.EventStore.Aspire;
using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Contracts.Routing;
using Hexalith.Tenants.Server.Routing;
using Microsoft.Extensions.Configuration;

namespace Hexalith.Tenants.AppHost;

/// <summary>Composes the existing owner traffic with explicit retained keys and actual SDK technical receivers.</summary>
internal static class TenantsDevelopmentRoutingCatalog
{
    internal static DevelopmentRouteCatalogComposition Create(IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> gateway, IResourceBuilder<ProjectResource> domain,
        IResourceBuilder<ProjectResource> api, IResourceBuilder<ProjectResource> ui,
        IResourceBuilder<ProjectResource> admin, IResourceBuilder<ProjectResource> adminUi, string stateComponent)
    {
        var custody = builder.Configuration.GetSection("Tenants:DevelopmentRouteCatalog")
            .Get<DevelopmentRouteCatalogCompositionOptions>()
            ?? throw new InvalidOperationException("Tenants:DevelopmentRouteCatalog must supply the retained owner custody and digest ring. An empty routing cell does not prove that the business backend is new.");
        // Preserve the existing human-delegated bootstrap's attribution using the existing
        // bound command proof, never a human submission of an internal operation.
        domain.WithEnvironment("Tenants__BootstrapBoundWorkloadEnabled", "true");
        gateway.WithEnvironment("Authentication__BoundCommand__AllowedDomains__tenants", "global-administrators");
        if (string.Equals(Environment.GetEnvironmentVariable("TENANTS_SOURCE_CATALOG_PROFILE"), "true", StringComparison.Ordinal))
        {
            gateway.WithEnvironment("Logging__LogLevel__Hexalith.EventStore.ServiceDefaults.Routing.TechnicalHttpRouteCatalogReceiver", "Debug");
            gateway.WithEnvironment("Logging__LogLevel__Hexalith.EventStore.Server.Commands.CommandRouter", "Debug");
            gateway.WithEnvironment("Logging__LogLevel__Hexalith.EventStore.Server.Pipeline.SubmitCommandHandler", "Debug");
            gateway.WithEnvironment("Logging__LogLevel__Hexalith.EventStore.Controllers.CommandsController", "Debug");
        }
        DevelopmentRouteCatalogInvocation[] domainCalls = EventStoreDomainServiceRoutes.Operational
            .Select(route => new DevelopmentRouteCatalogInvocation(gateway, route.Route, ["POST"])).ToArray();
        var catalog = builder.AddEventStoreDevelopmentRouteCatalog(gateway,
            [new(domain, typeof(TenantCatalogDeclarations).Assembly, typeof(TenantIntentAdapters).Assembly)],
            [new(gateway, [new(domain, "/api/v1/commands/bound/workload", ["POST"]), new(domain, "/api/v1/streams/read/workload", ["POST"]),
                new(api, "/api/v1/commands", ["POST"]), new(api, "/api/v1/queries", ["POST"]), new(api, "/ready", ["GET"]),
                new(admin, "/**", ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"])]) { TechnicalHttp = true },
                new(domain, domainCalls), api.CaptureEventStoreDevelopmentReceiver(ui),
                new(admin, [new(adminUi, "/**", ["GET", "POST", "PUT", "DELETE"])]) { TechnicalHttp = true, ActorStateOwners = [gateway] }],
            stateComponent, custody);
        domain.WithEnvironment("Tenants__BootstrapBoundWorkloadReceiptPath", Path.Combine(catalog.Directory, "tenants.bootstrap.json"));
        return catalog;
    }
}
#endif
