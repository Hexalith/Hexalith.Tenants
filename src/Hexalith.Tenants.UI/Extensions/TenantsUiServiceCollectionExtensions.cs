using System.Diagnostics.CodeAnalysis;

using Hexalith.EventStore.Client.Registration;
using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.Memories.Client.Rest;
using Hexalith.Tenants.UI.Composition;
using Hexalith.Tenants.UI.Services;
using Hexalith.Tenants.UI.Services.Configuration;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.AspNetCore.DataProtection;
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

        bool hasQueryGatewayOverride = services.Any(
            static descriptor => !descriptor.IsKeyedService
                && descriptor.ServiceType == typeof(ITenantQueryGateway));
        bool hasReadAvailabilityOverride = services.Any(
            static descriptor => !descriptor.IsKeyedService
                && descriptor.ServiceType == typeof(ITenantsReadSurfaceAvailability));
        if (hasQueryGatewayOverride != hasReadAvailabilityOverride)
        {
            throw new InvalidOperationException(
                "A host-provided ITenantQueryGateway and ITenantsReadSurfaceAvailability must be registered together before AddHexalithTenantsUiModule.");
        }

        _ = services.AddDataProtection();
        services.TryAddScoped<IUserContextAccessor, NullUserContextAccessor>();
        services.AddTenantConfigurationReadPolicy(configuration);
        services.TryAddSingleton<ITenantSearchCursorCodec, TenantSearchCursorCodec>();
        services.TryAddScoped<TenantSearchPagingState>();
        services.TryAddScoped<TenantReadRefreshSubscription>();

        services.AddAuthorizationCore(options =>
            options.AddPolicy(
                TenantsFrontComposerRegistration.GlobalAdministratorPolicy,
                policy => policy.RequireAssertion(context =>
                    TenantsGlobalAdministratorClaims.Evaluate(context.User)
                        == TenantLifecycleAuthorizationReflectionState.Authorized)));

        if (TryGetHttpBaseAddress(configuration["Tenants:BaseAddress"], out Uri? tenantsBaseAddress))
        {
            IHttpClientBuilder tenantsQueryClient = services
                .AddHttpClient<TenantsRestQueryClient>(client => client.BaseAddress = tenantsBaseAddress)
                .AddServiceDiscovery()
                .RemoveAllLoggers();
            if (enableGatewayAuthorization)
            {
                _ = tenantsQueryClient.AddFrontComposerGatewayAuthorization();
            }

            services.TryAddScoped<ITenantsRestQueryClient>(sp => sp.GetRequiredService<TenantsRestQueryClient>());
            services.TryAddScoped<ITenantQueryGateway, TenantQueryGateway>();
            services.TryAddSingleton<ITenantsReadSurfaceAvailability>(
                new TenantsReadSurfaceAvailability(IsConnected: true));
        }
        else
        {
            services.TryAddScoped<ITenantQueryGateway, UnavailableTenantQueryGateway>();
            services.TryAddSingleton<ITenantsReadSurfaceAvailability>(
                new TenantsReadSurfaceAvailability(IsConnected: false));
        }

        // Same scheme gate as the read side. Without it a typo or copied service-discovery value registers a
        // command HttpClient on a non-HTTP scheme, which fails at send time with a raw transport exception
        // instead of resolving UnavailableTenantCommandGateway.
        if (TryGetHttpBaseAddress(configuration["EventStore:BaseAddress"], out Uri? eventStoreBaseAddress))
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
        }).RemoveAllLoggers();

        services.TryAddScoped<ITenantsBffComposition, TenantsBffComposition>();
        services.Configure<FcShellOptions>(configuration.GetSection("Hexalith:Shell"));
        return services;
    }

    /// <summary>
    /// Accepts only base addresses this module can actually send over: plain <c>http</c>/<c>https</c>, plus
    /// the Aspire service-discovery compound forms such as <c>https+http://tenants</c>.
    /// </summary>
    /// <remarks>
    /// The compound schemes must be accepted because <c>.AddServiceDiscovery()</c> is attached to the same
    /// client. Rejecting them would make the canonical Aspire configuration value fail the gate, silently
    /// resolve <see cref="UnavailableTenantQueryGateway"/>, and leave every read fail-closed with no
    /// diagnostic — a misconfiguration indistinguishable from an outage.
    /// </remarks>
    private static bool TryGetHttpBaseAddress(string? value, [NotNullWhen(true)] out Uri? baseAddress)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out Uri? parsed)
            && IsSendableScheme(parsed.Scheme)
            && !string.IsNullOrWhiteSpace(parsed.Host)
            && string.IsNullOrEmpty(parsed.UserInfo)
            && string.IsNullOrEmpty(parsed.Query)
            && string.IsNullOrEmpty(parsed.Fragment))
        {
            baseAddress = parsed;
            return true;
        }

        baseAddress = null;
        return false;
    }

    private static bool IsSendableScheme(string scheme)
    {
        string[] parts = scheme.Split('+', StringSplitOptions.None);
        foreach (string part in parts)
        {
            if (part.Length == 0
                || (!string.Equals(part, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(part, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        return parts.Length > 0;
    }
}
