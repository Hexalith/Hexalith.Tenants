using Dapr.Actors.Client;
using Dapr.Client;

using FluentValidation;

using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.Controllers;
using Hexalith.EventStore.Extensions;
using Hexalith.EventStore.Middleware;
using Hexalith.EventStore.Server.Commands;
using Hexalith.EventStore.Server.Projections;
using Hexalith.EventStore.Server.Queries;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Tenants.Authorization;
using Hexalith.Tenants.Server.Aggregates;
using Hexalith.Tenants.Validation;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using NSubstitute;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Composes the published gateway HTTP pipeline independently of the Tenants domain host.</summary>
/// <param name="router">The command routing boundary supplied by the contract test.</param>
/// <param name="statusStore">The status producer supplied by the contract test.</param>
/// <param name="archiveStore">The archive supplied by the contract test.</param>
/// <param name="useLegacyStatusAdapter">Whether lifecycle-only records need their status-key message identity.</param>
internal sealed class TenantGatewayTestHost(
    ICommandRouter? router = null,
    ICommandStatusStore? statusStore = null,
    ICommandArchiveStore? archiveStore = null,
    bool useLegacyStatusAdapter = true) : IAsyncDisposable
{
    private WebApplication? _application;

    /// <summary>The isolated gateway audience used by the HTTP contract tests.</summary>
    internal const string Audience = "hexalith-tenants";

    /// <summary>The isolated gateway issuer used by the HTTP contract tests.</summary>
    internal const string Issuer = "hexalith-dev";

    /// <summary>The development-only signing key shared with the test token builder.</summary>
    internal const string SigningKey = "this-is-a-development-signing-key-minimum-32-chars";

    /// <summary>Creates a client with no domain-service workload credentials.</summary>
    /// <returns>A client for the real gateway controllers and middleware.</returns>
    internal HttpClient CreateClient()
    {
        _application ??= CreateApplication();
        return _application.GetTestClient();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_application is not null)
        {
            await _application.DisposeAsync();
        }
    }

    private WebApplication CreateApplication()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(CommandsController).Assembly.GetName().Name,
            ContentRootPath = AppContext.BaseDirectory,
            EnvironmentName = Environments.Development,
        });
        builder.WebHost.UseTestServer();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:JwtBearer:Issuer"] = Issuer,
            ["Authentication:JwtBearer:Audience"] = Audience,
            ["Authentication:JwtBearer:SigningKey"] = SigningKey,
            ["Authentication:JwtBearer:RequireHttpsMetadata"] = "false",
            ["Authentication:JwtBearer:AllowInsecureSymmetricKey"] = "true",
            ["EventStore:RateLimiting:PermitLimit"] = "10000",
        });

        // The gateway owns authentication, authorization, validation, controllers, and the
        // real SubmitCommandHandler. Only its external routing and persistence seams are doubled.
        builder.Services.AddEventStore();
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
        builder.Services.AddSingleton(TenantOwnerCatalogTestAuthority.Create());
#endif
        builder.Services.AddControllers().AddApplicationPart(typeof(CommandsController).Assembly);
        builder.Services.AddValidatorsFromAssembly(typeof(TenantSubmitCommandValidator).Assembly);
        builder.Services.AddValidatorsFromAssembly(typeof(TenantAggregate).Assembly);
        builder.Services.RemoveAll<ITenantValidator>();
        builder.Services.AddScoped<ITenantValidator, TenantsSystemTenantValidator>();
        builder.Services.AddSingleton(Substitute.For<DaprClient>());
        builder.Services.AddSingleton<IActorProxyFactory>(_ => new ActorProxyFactory());
        builder.Services.AddSingleton(Substitute.For<IQueryRouter>());
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<INamedProjectionRouteCatalog, NamedProjectionRouteCatalog>();
        builder.Services.AddSingleton<IProjectionUpdateOrchestrator, NoOpProjectionUpdateOrchestrator>();
        TestServiceOverrides.ReplaceCommandRouter(builder.Services);
        builder.Services.RemoveAll<ICommandRouter>();
        builder.Services.AddSingleton(router ?? Substitute.For<ICommandRouter>());
        builder.Services.RemoveAll<ICommandStatusStore>();
        ICommandStatusStore suppliedStatusStore = statusStore ?? new InMemoryCommandStatusStore();
        builder.Services.AddSingleton<ICommandStatusStore>(useLegacyStatusAdapter
            ? new MessageIdentifyingCommandStatusStore(suppliedStatusStore) : suppliedStatusStore);
        builder.Services.RemoveAll<ICommandArchiveStore>();
        builder.Services.AddSingleton(archiveStore ?? new InMemoryCommandArchiveStore());
        builder.Services.RemoveAll<ICommandCorrelationIndex>();
        builder.Services.AddSingleton<ICommandCorrelationIndex, InMemoryCommandCorrelationIndex>();

        WebApplication application = builder.Build();
        try
        {
            application.UseMiddleware<CorrelationIdMiddleware>();
            application.UseExceptionHandler();
            application.UseRouting();
            application.UseAuthentication();
            application.UseRateLimiter();
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
            application.UseEventStoreHttpHostSecurity();
#endif
            application.UseAuthorization();
            application.MapControllers();
            application.StartAsync().GetAwaiter().GetResult();
            return application;
        }
        catch
        {
            application.DisposeAsync().AsTask().GetAwaiter().GetResult();
            throw;
        }
    }
}
