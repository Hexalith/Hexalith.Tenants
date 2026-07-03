using Hexalith.EventStore.Client.Registration;
using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.Memories.Client.Rest;
using Hexalith.Tenants.UI.Components;
using Hexalith.Tenants.UI.Composition;
using Hexalith.Tenants.UI.Services;
using Hexalith.Tenants.UI.Services.Gateways;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(o => o.ValidateScopes = true);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();
builder.Services.AddHttpContextAccessor();

builder.Services.AddHexalithFrontComposerQuickstart(
    o => o.ScanAssemblies(typeof(TenantsFrontComposerDomain).Assembly));
builder.Services.AddHexalithDomain<TenantsFrontComposerDomain>();

// Gate Global Administrators surfaces on the same server-side global-administrator principal shape the BFF
// composition reflects. Registered unconditionally so the policy resolves whether or not interactive OIDC
// sign-in is wired.
builder.Services.AddAuthorizationCore(options =>
    options.AddPolicy(
        TenantsFrontComposerRegistration.GlobalAdministratorPolicy,
        policy => policy.RequireAssertion(context =>
            TenantsGlobalAdministratorClaims.IsGlobalAdministrator(context.User))));

// Interactive per-user sign-in: when an OIDC provider is configured (AppHost supplies Keycloak
// authority/client), wire authorization-code login and relay the signed-in user's access token to
// the EventStore gateway so queries/commands authorize as that user.
bool authEnabled =
    Uri.TryCreate(builder.Configuration["Authentication:OpenIdConnect:Authority"], UriKind.Absolute, out Uri? oidcAuthority)
    && !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:OpenIdConnect:ClientId"])
    && !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:OpenIdConnect:ClientSecret"]);

if (authEnabled) {
    // Server-side FrontComposer security is framework-owned plumbing: the authentication bridge, the
    // interactive Server AuthenticationStateProvider (replacing the quickstart's fail-closed anonymous
    // one so the header account menu / AuthorizeView see the signed-in user), and the per-user access
    // token relay. Tenants supplies only the domain-specific provider configuration (Keycloak + the
    // tenant/user claim mapping); the generic wiring lives in Hexalith.FrontComposer.Shell so every
    // domain module reuses it instead of duplicating it.
    _ = builder.Services.AddHexalithFrontComposerServerSecurity(o => o.UseKeycloak(
        oidcAuthority!,
        builder.Configuration["Authentication:OpenIdConnect:ClientId"]!,
        builder.Configuration["Authentication:OpenIdConnect:ClientSecret"]!,
        // IUserContextAccessor maps to the single-valued *current tenant* claim, NOT the multi-valued
        // eventstore:tenant authorization scope (the fail-closed extractor rejects multi-valued tenant
        // claims). EventStore command/query authorization continues to use eventstore:tenant server-side.
        tenantClaimType: "eventstore:current-tenant",
        userClaimType: "sub"));
}

if (Uri.TryCreate(builder.Configuration["EventStore:BaseAddress"], UriKind.Absolute, out Uri? eventStoreBaseAddress)) {
    _ = builder.Services.AddHexalithEventStore(o => o.BaseAddress = eventStoreBaseAddress);

    // TenantCommandGateway submits commands through IEventStoreGatewayClient (the EventStore.Client
    // typed HTTP client). AddHexalithEventStore wires the Shell's own command/query clients but not
    // this gateway abstraction, so register it explicitly and relay the signed-in user's token the
    // same way the status client does when auth is enabled.
    IHttpClientBuilder eventStoreGatewayClient = builder.Services.AddEventStoreGatewayClient(o => o.BaseAddress = eventStoreBaseAddress);
    IHttpClientBuilder commandGatewayClient = builder.Services.AddHttpClient<TenantCommandGateway>(client => client.BaseAddress = eventStoreBaseAddress);
    if (authEnabled) {
        _ = eventStoreGatewayClient.AddFrontComposerGatewayAuthorization();
        _ = commandGatewayClient.AddFrontComposerGatewayAuthorization();
    }

    builder.Services.TryAddScoped<ITenantCommandGateway>(sp => sp.GetRequiredService<TenantCommandGateway>());
    builder.Services.TryAddScoped<ITenantQueryGateway, TenantQueryGateway>();
}
else {
    builder.Services.TryAddScoped<ITenantCommandGateway, UnavailableTenantCommandGateway>();
    builder.Services.TryAddScoped<ITenantQueryGateway, UnavailableTenantQueryGateway>();
}

// Memories-backed cross-set tenant search. TenantQueryGateway calls MemoriesClient.SearchAsync to get
// the match-set of tenant ids; rows are still hydrated through the ETag-fresh tenant detail path (D6),
// so Memories decides which tenants appear, never what each row shows. Registered unconditionally so the
// gateway always resolves a client; when Memories:BaseAddress is unset the search path degrades to the
// cursor list (no exception reaches the circuit). The per-user token is intentionally NOT relayed here —
// per-user visibility is enforced at hydration (forbidden ids dropped), not by the index lookup. Memories
// uses its own service ApiToken (HEXALITH_MEMORIES_API_TOKEN) via the client's auth handler.
_ = builder.Services.AddMemoriesClient(o => {
    if (Uri.TryCreate(builder.Configuration["Memories:BaseAddress"], UriKind.Absolute, out Uri? memoriesBaseAddress)) {
        o.Endpoint = memoriesBaseAddress;
    }

    o.ApiToken = builder.Configuration["HEXALITH_MEMORIES_API_TOKEN"];
});

// IUserContextAccessor is provided by the FrontComposer authentication bridge
// (ClaimsPrincipalUserContextAccessor), configured above with the eventstore:tenant / sub claim
// mapping. No tenant-specific accessor override is needed.
builder.Services.TryAddScoped<ITenantsBffComposition, TenantsBffComposition>();
builder.Services.Configure<FcShellOptions>(builder.Configuration.GetSection("Hexalith:Shell"));

WebApplication app = builder.Build();

app.MapStaticAssets();
app.UseStaticFiles();
app.UseRequestLocalization();

if (authEnabled) {
    _ = app.UseAuthentication();
    _ = app.UseAuthorization();
}

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

if (authEnabled) {
    _ = app.MapHexalithFrontComposerAuthenticationEndpoints();
}

app.Run();
