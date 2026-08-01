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
        // The gateway side is matched on either descriptor shape. Matching only ImplementationType meant a
        // host registering the unavailable gateway as an instance skipped the check entirely -- exactly the
        // pairing the check exists to reject. The availability side stays instance-only by necessity: a
        // factory-registered availability cannot be asked for IsConnected without building a provider at
        // composition time, so its truthfulness is unknowable here and the pair is left unchecked.
        if (queryGatewayOverride is not null
            && readAvailabilityOverride?.ImplementationInstance is ITenantsReadSurfaceAvailability declaredAvailability)
        {
            // Tri-state on purpose. A factory descriptor exposes neither ImplementationType nor
            // ImplementationInstance, so the gateway's identity is unknowable at composition time. Because
            // the test below is a two-sided equality, an unrecognised shape did not "skip the check" -- it
            // inverted it: a host that factory-registered UnavailableTenantQueryGateway and truthfully
            // declared IsConnected: false hit false == false and was thrown out with the message saying the
            // registered gateway is a connected implementation. Unknowable shapes are now genuinely skipped,
            // matching how the availability side already handles its own factory case.
            bool? gatewayIsUnavailable = queryGatewayOverride.ImplementationType is not null
                ? queryGatewayOverride.ImplementationType == typeof(UnavailableTenantQueryGateway)
                : queryGatewayOverride.ImplementationInstance is not null
                    ? queryGatewayOverride.ImplementationInstance is UnavailableTenantQueryGateway
                    : null;
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

        // Decision D-G: composition runs before any logger exists, so a rejected base address is recorded
        // here by key name and reported once at startup by TenantsUiConfigurationDiagnosticsReporter. The
        // per-side fail-closed registration below is unchanged -- what is restored is the signal, without
        // which a typo and a genuine outage render identically and nothing anywhere names the setting.
        TenantsUiConfigurationDiagnostics diagnostics = new();

        // No service discovery is attached: the AppHost injects an already-resolved endpoint URL, which is
        // the exact shape the EventStore command/status clients below and the Memories client already
        // consume. Attaching .AddServiceDiscovery() registered the resolving handler without any endpoint
        // provider -- the UI host never calls AddServiceDefaults and this repo has no ServiceDefaults
        // project -- so every read threw "No provider which supports the provided service name" at send
        // time, and that handler also rebuilt the request URI, re-canonicalizing the %2E escaping that
        // DangerousDisablePathAndQueryCanonicalization exists to preserve.
        // AC2 / "Separate dependencies": read availability follows ONLY the read reference. A compound or
        // otherwise unsendable value fails closed to UnavailableTenantQueryGateway here -- it must not abort
        // composition, because that also prevented the command/status block below from registering, so a typo
        // in the read address took down command submission too. TryGetHttpBaseAddress already rejects the
        // compound scheme (IsSendableScheme accepts only exact http/https), so nothing is lost by not throwing.
        if (TryGetHttpBaseAddress(configuration["Tenants:BaseAddress"], out Uri? tenantsBaseAddress))
        {
            IHttpClientBuilder tenantsQueryClient = services
                .AddHttpClient<TenantsRestQueryClient>(client => client.BaseAddress = tenantsBaseAddress)
                // The six reads are exact-route contracts. Following a 3xx can silently turn one read into
                // another route whose overlapping JSON shape deserializes successfully, so redirects are a
                // fixed unavailable result rather than transport instructions this client follows.
                .ConfigurePrimaryHttpMessageHandler(static () => new HttpClientHandler { AllowAutoRedirect = false })
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
            // Review loop 13: record the rejection only when this fail-closed registration is the one that
            // takes effect. Every gateway registration is TryAdd*, so on a repeated call to this method the
            // first registration wins; recording unconditionally then warned that a surface was unavailable
            // when the container had in fact resolved a working one from the earlier call.
            bool queryGatewayAlreadyRegistered = services.Any(
                static descriptor => descriptor.ServiceType == typeof(ITenantQueryGateway));
            services.TryAddScoped<ITenantQueryGateway, UnavailableTenantQueryGateway>();
            services.TryAddSingleton<ITenantsReadSurfaceAvailability>(
                new TenantsReadSurfaceAvailability(IsConnected: false));
            if (!queryGatewayAlreadyRegistered)
            {
                diagnostics.RecordRejectedBaseAddressIfConfigured(configuration, "Tenants:BaseAddress");
            }
        }

        // Same scheme gate as the read side, and symmetrically fail-closed: a typo or copied service-discovery
        // value resolves UnavailableTenantCommandGateway rather than sending on a non-HTTP scheme, and it does
        // not disable the independent read side registered above. TryGetHttpBaseAddress narrows to plain
        // http/https, which also covers the previously deferred item "EventStore:BaseAddress accepts compound
        // service-discovery schemes while no service discovery is attached".
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
            // Same effect gate as the read side above.
            bool commandGatewayAlreadyRegistered = services.Any(
                static descriptor => descriptor.ServiceType == typeof(ITenantCommandGateway));
            services.TryAddScoped<ITenantCommandGateway, UnavailableTenantCommandGateway>();
            if (!commandGatewayAlreadyRegistered)
            {
                diagnostics.RecordRejectedBaseAddressIfConfigured(configuration, "EventStore:BaseAddress");
            }
        }

        _ = services.AddMemoriesClient(o =>
        {
            if (Uri.TryCreate(configuration["Memories:BaseAddress"], UriKind.Absolute, out Uri? memoriesBaseAddress))
            {
                o.Endpoint = memoriesBaseAddress;
            }

            o.ApiToken = configuration["HEXALITH_MEMORIES_API_TOKEN"];
        }).RemoveAllLoggers();

        // Added, not TryAdded. TryAddSingleton kept whatever was registered first, so a host that had already
        // registered this type -- or a second call to this method -- dropped the instance actually holding
        // this composition's rejections and left the reporter with an empty one. The reporter consumes the
        // whole enumerable and de-duplicates, so every composition's findings are reported exactly once.
        services.AddSingleton(diagnostics);
        services.AddHostedService<TenantsUiConfigurationDiagnosticsReporter>();

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
    /// can never be sent and this gate rejects it.
    /// <para>
    /// Review loop 9, decision D2: rejection is per-side and fail-closed, NOT a thrown boot failure. A
    /// separate <c>EnsureSendableOrThrow</c> guard used to throw here; because it ran before the
    /// command/status block, an unsendable READ address also prevented the command clients from
    /// registering, and the mirror case killed the read side — the all-or-nothing registration AC2 and the
    /// "Separate dependencies" matrix row forbid. An unsendable address now falls through to
    /// <see cref="UnavailableTenantQueryGateway"/> (or the command-side equivalent) for that side only.
    /// </para>
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
    /// Determines whether a URI scheme is one this module can actually send over.
    /// </summary>
    /// <param name="scheme">The scheme to test.</param>
    /// <returns><see langword="true"/> for exactly <c>http</c> or <c>https</c>; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// Exact match by design. Aspire compound schemes such as <c>https+http</c> need a registered service
    /// discovery mechanism to resolve, and none is attached to these clients, so accepting one would defer a
    /// certain failure from composition to every request.
    /// </remarks>
    private static bool IsSendableScheme(string scheme)
        => string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
}
