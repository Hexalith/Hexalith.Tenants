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

        ServiceDescriptor? queryGatewayOverride = services.LastOrDefault(
            static descriptor => !descriptor.IsKeyedService
                && descriptor.ServiceType == typeof(ITenantQueryGateway));
        ServiceDescriptor? readAvailabilityOverride = services.LastOrDefault(
            static descriptor => !descriptor.IsKeyedService
                && descriptor.ServiceType == typeof(ITenantsReadSurfaceAvailability));
        if ((queryGatewayOverride is null) != (readAvailabilityOverride is null))
        {
            throw new InvalidOperationException(
                "A host-provided ITenantQueryGateway and ITenantsReadSurfaceAvailability must be registered together before AddHexalithTenantsUiModule.");
        }

        // Presence alone is not the contract. ITenantsReadSurfaceAvailability exists so consumers can gate
        // on a *truthful* statement about the read surface, so a host that declares IsConnected: true while
        // registering UnavailableTenantQueryGateway (or the converse) must be rejected rather than accepted
        // as a matched pair -- that pairing is exactly what requiring the pair is supposed to prevent.
        if (queryGatewayOverride is not null
            && readAvailabilityOverride?.ImplementationInstance is ITenantsReadSurfaceAvailability declaredAvailability)
        {
            bool gatewayIsUnavailable = queryGatewayOverride.ImplementationType == typeof(UnavailableTenantQueryGateway);
            if (gatewayIsUnavailable == declaredAvailability.IsConnected)
            {
                throw new InvalidOperationException(
                    declaredAvailability.IsConnected
                        ? "A host-provided ITenantsReadSurfaceAvailability declares IsConnected: true while the registered ITenantQueryGateway is UnavailableTenantQueryGateway."
                        : "A host-provided ITenantsReadSurfaceAvailability declares IsConnected: false while the registered ITenantQueryGateway is a connected implementation.");
            }
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

        // No service discovery is attached: the AppHost injects an already-resolved endpoint URL, which is
        // the exact shape the EventStore command/status clients below and the Memories client already
        // consume. Attaching .AddServiceDiscovery() registered the resolving handler without any endpoint
        // provider -- the UI host never calls AddServiceDefaults and this repo has no ServiceDefaults
        // project -- so every read threw "No provider which supports the provided service name" at send
        // time, and that handler also rebuilt the request URI, re-canonicalizing the %2E escaping that
        // DangerousDisablePathAndQueryCanonicalization exists to preserve.
        EnsureSendableOrThrow("Tenants:BaseAddress", configuration["Tenants:BaseAddress"]);
        if (TryGetHttpBaseAddress(configuration["Tenants:BaseAddress"], out Uri? tenantsBaseAddress))
        {
            IHttpClientBuilder tenantsQueryClient = services
                .AddHttpClient<TenantsRestQueryClient>(client => client.BaseAddress = tenantsBaseAddress)
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
        // instead of resolving UnavailableTenantCommandGateway. Narrowing the shared gate to plain http/https
        // also closes the previously deferred item "EventStore:BaseAddress accepts compound
        // service-discovery schemes while no service discovery is attached": no discovery is attached to
        // these clients either, so the same value would fail at every request instead of at boot.
        EnsureSendableOrThrow("EventStore:BaseAddress", configuration["EventStore:BaseAddress"]);
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
    /// Accepts only base addresses this module can actually send over: plain <c>http</c> or <c>https</c>.
    /// </summary>
    /// <remarks>
    /// An earlier revision also accepted the Aspire compound forms such as <c>https+http://tenants</c>,
    /// because <c>.AddServiceDiscovery()</c> was attached to the read client and would resolve them. That
    /// widening is superseded: no discovery mechanism is registered in this topology, so a compound scheme
    /// can never be sent. It is now rejected loudly by <see cref="EnsureSendableOrThrow"/> rather than
    /// silently, because falling through to <see cref="UnavailableTenantQueryGateway"/> would leave every
    /// read fail-closed with no diagnostic — a misconfiguration indistinguishable from an outage.
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

    /// <summary>
    /// Fails composition when a setting carries a service-discovery compound scheme this module cannot send.
    /// </summary>
    /// <param name="settingName">The configuration key, for the diagnostic.</param>
    /// <param name="value">The configured value.</param>
    /// <remarks>
    /// Absent, empty, or unparseable values are NOT an error: they mean the dependency is simply not
    /// configured, and the caller fails closed to the unavailable gateway. A compound scheme is different --
    /// it is a value the operator believed would work, so it is reported at boot instead of failing every
    /// request at send time.
    /// </remarks>
    private static void EnsureSendableOrThrow(string settingName, string? value)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out Uri? parsed)
            && IsServiceDiscoveryCompoundScheme(parsed.Scheme))
        {
            throw new InvalidOperationException(
                $"'{settingName}' uses the service-discovery compound scheme '{parsed.Scheme}', which this module cannot send: no service discovery is registered. Configure a resolved http or https address.");
        }
    }

    /// <summary>
    /// Recognises a well-formed Aspire compound scheme such as <c>https+http</c>.
    /// </summary>
    /// <remarks>
    /// Deliberately narrower than "contains a plus": a malformed value such as <c>http++https</c> has an
    /// empty part and is a typo, not a service-discovery address. Typos keep the existing fail-closed
    /// behaviour so this guard reports only the one misconfiguration it can describe accurately.
    /// </remarks>
    private static bool IsServiceDiscoveryCompoundScheme(string scheme)
    {
        string[] parts = scheme.Split('+', StringSplitOptions.None);
        return parts.Length > 1 && Array.TrueForAll(parts, IsSendableScheme);
    }

    private static bool IsSendableScheme(string scheme)
        => string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
}
