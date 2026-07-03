using Hexalith.EventStore.Client.Registration;
using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.Memories.Client.Rest;
using Hexalith.Tenants.UI.Composition;
using Hexalith.Tenants.UI.Services;
using Hexalith.Tenants.UI.Services.Gateways;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.Tenants.UI.Extensions;

/// <summary>
/// Service registration helpers for hosts that embed the Tenants UI module.
/// </summary>
public static class TenantsUiServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Tenants UI BFF services used by the standalone Tenants UI host.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">Host configuration.</param>
    /// <param name="enableGatewayAuthorization">Whether gateway HTTP clients should relay the signed-in user's bearer token.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddHexalithTenantsUiModule(
        this IServiceCollection services,
        IConfiguration configuration,
        bool enableGatewayAuthorization)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddAuthorizationCore(options =>
            options.AddPolicy(
                TenantsFrontComposerRegistration.GlobalAdministratorPolicy,
                policy => policy.RequireAssertion(context =>
                    TenantsGlobalAdministratorClaims.IsGlobalAdministrator(context.User))));

        if (Uri.TryCreate(configuration["EventStore:BaseAddress"], UriKind.Absolute, out Uri? eventStoreBaseAddress))
        {
            _ = services.AddHexalithEventStore(o => o.BaseAddress = eventStoreBaseAddress);

            IHttpClientBuilder eventStoreGatewayClient = services.AddEventStoreGatewayClient(o => o.BaseAddress = eventStoreBaseAddress);
            IHttpClientBuilder commandGatewayClient = services.AddHttpClient<TenantCommandGateway>(client => client.BaseAddress = eventStoreBaseAddress);
            if (enableGatewayAuthorization)
            {
                _ = eventStoreGatewayClient.AddFrontComposerGatewayAuthorization();
                _ = commandGatewayClient.AddFrontComposerGatewayAuthorization();
            }

            services.TryAddScoped<ITenantCommandGateway>(sp => sp.GetRequiredService<TenantCommandGateway>());
        }
        else
        {
            services.TryAddScoped<ITenantCommandGateway, UnavailableTenantCommandGateway>();
        }

        _ = services.AddMemoriesClient(o =>
        {
            if (Uri.TryCreate(configuration["Memories:BaseAddress"], UriKind.Absolute, out Uri? memoriesBaseAddress))
            {
                o.Endpoint = memoriesBaseAddress;
            }

            o.ApiToken = configuration["HEXALITH_MEMORIES_API_TOKEN"];
        });

        if (Uri.TryCreate(configuration["Tenants:BaseAddress"], UriKind.Absolute, out Uri? tenantsBaseAddress))
        {
            IHttpClientBuilder queryClient = services.AddHttpClient<ITenantsQueryApiClient, TenantsQueryApiClient>(
                client => client.BaseAddress = tenantsBaseAddress);
            if (enableGatewayAuthorization)
            {
                _ = queryClient.AddFrontComposerGatewayAuthorization();
            }

            services.TryAddScoped<ITenantQueryGateway, TenantQueryGateway>();
        }
        else
        {
            services.TryAddScoped<ITenantQueryGateway, UnavailableTenantQueryGateway>();
        }

        services.TryAddScoped<ITenantsBffComposition, TenantsBffComposition>();
        services.Configure<FcShellOptions>(configuration.GetSection("Hexalith:Shell"));
        return services;
    }
}
