using Hexalith.EventStore.Client.Registration;
using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.Tenants.UI.Components;
using Hexalith.Tenants.UI.Composition;
using Hexalith.Tenants.UI.Services;
using Hexalith.Tenants.UI.Services.Auth;
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

// Gate the Global Administrators left-menu entry (rendered by the shell via AuthorizeView) on the
// same server-side global-administrator principal shape the BFF composition reflects. Registered
// unconditionally so the policy resolves whether or not interactive OIDC sign-in is wired.
builder.Services.AddAuthorizationCore(options =>
    options.AddPolicy(
        TenantsFrontComposerRegistration.GlobalAdministratorPolicy,
        policy => policy
            .RequireRole("GlobalAdministrator")
            .RequireClaim("eventstore:tenant", "system")));

// Interactive per-user sign-in: when an OIDC provider is configured (AppHost supplies Keycloak
// authority/client), wire authorization-code login and relay the signed-in user's access token to
// the EventStore gateway so queries/commands authorize as that user.
bool authEnabled =
    Uri.TryCreate(builder.Configuration["Authentication:OpenIdConnect:Authority"], UriKind.Absolute, out Uri? oidcAuthority)
    && !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:OpenIdConnect:ClientId"])
    && !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:OpenIdConnect:ClientSecret"]);

if (authEnabled) {
    _ = builder.Services.AddHexalithFrontComposerAuthentication(o => o.UseKeycloak(
        oidcAuthority!,
        builder.Configuration["Authentication:OpenIdConnect:ClientId"]!,
        builder.Configuration["Authentication:OpenIdConnect:ClientSecret"]!,
        tenantClaimType: "eventstore:tenant",
        userClaimType: "sub"));
    _ = builder.Services.AddTenantsTokenRelay();
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
        _ = eventStoreGatewayClient.AddGatewayAuthorization();
        _ = commandGatewayClient.AddGatewayAuthorization();
    }

    builder.Services.TryAddScoped<ITenantCommandGateway>(sp => sp.GetRequiredService<TenantCommandGateway>());
}
else {
    builder.Services.TryAddScoped<ITenantCommandGateway, UnavailableTenantCommandGateway>();
}

if (Uri.TryCreate(builder.Configuration["Tenants:BaseAddress"], UriKind.Absolute, out Uri? tenantsBaseAddress)) {
    IHttpClientBuilder queryClient = builder.Services.AddHttpClient<ITenantsQueryApiClient, TenantsQueryApiClient>(
        client => client.BaseAddress = tenantsBaseAddress);
    if (authEnabled) {
        _ = queryClient.AddGatewayAuthorization();
    }

    builder.Services.TryAddScoped<ITenantQueryGateway, TenantQueryGateway>();
}
else {
    builder.Services.TryAddScoped<ITenantQueryGateway, UnavailableTenantQueryGateway>();
}

builder.Services.Replace(ServiceDescriptor.Scoped<IUserContextAccessor, ClaimsUserContextAccessor>());
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
