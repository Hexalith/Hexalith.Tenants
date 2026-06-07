using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.EventStore.Client.Registration;
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

// Interactive per-user sign-in: when an OIDC provider is configured (AppHost supplies Keycloak
// authority/client), wire authorization-code login and relay the signed-in user's access token to
// the EventStore gateway so queries/commands authorize as that user.
bool authEnabled =
    Uri.TryCreate(builder.Configuration["Authentication:OpenIdConnect:Authority"], UriKind.Absolute, out Uri? oidcAuthority)
    && !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:OpenIdConnect:ClientId"])
    && !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:OpenIdConnect:ClientSecret"]);

if (authEnabled) {
    builder.Services.AddHexalithFrontComposerAuthentication(o => o.UseKeycloak(
        oidcAuthority!,
        builder.Configuration["Authentication:OpenIdConnect:ClientId"]!,
        builder.Configuration["Authentication:OpenIdConnect:ClientSecret"]!,
        tenantClaimType: "eventstore:tenant",
        userClaimType: "sub"));
    builder.Services.AddTenantsTokenRelay();
}

if (Uri.TryCreate(builder.Configuration["EventStore:BaseAddress"], UriKind.Absolute, out Uri? eventStoreBaseAddress)) {
    builder.Services.AddHexalithEventStore(o => o.BaseAddress = eventStoreBaseAddress);
    IHttpClientBuilder queryGatewayClient = builder.Services.AddEventStoreGatewayClient(o => o.BaseAddress = eventStoreBaseAddress);
    IHttpClientBuilder commandGatewayClient = builder.Services.AddHttpClient<TenantCommandGateway>(client => client.BaseAddress = eventStoreBaseAddress);
    if (authEnabled) {
        _ = queryGatewayClient.AddGatewayAuthorization();
        _ = commandGatewayClient.AddGatewayAuthorization();
    }

    builder.Services.TryAddScoped<ITenantQueryGateway, TenantQueryGateway>();
    builder.Services.TryAddScoped<ITenantCommandGateway>(sp => sp.GetRequiredService<TenantCommandGateway>());
}
else {
    builder.Services.TryAddScoped<ITenantQueryGateway, UnavailableTenantQueryGateway>();
    builder.Services.TryAddScoped<ITenantCommandGateway, UnavailableTenantCommandGateway>();
}

builder.Services.Replace(ServiceDescriptor.Scoped<IUserContextAccessor, ClaimsUserContextAccessor>());
builder.Services.TryAddScoped<ITenantsBffComposition, TenantsBffComposition>();
builder.Services.Configure<FcShellOptions>(builder.Configuration.GetSection("Hexalith:Shell"));

WebApplication app = builder.Build();

app.MapStaticAssets();
app.UseStaticFiles();
app.UseRequestLocalization();

if (authEnabled) {
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

if (authEnabled) {
    _ = app.MapHexalithFrontComposerAuthenticationEndpoints();
}

app.Run();
