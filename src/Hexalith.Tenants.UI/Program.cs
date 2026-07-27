using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.Tenants.UI.Components;
using Hexalith.Tenants.UI.Composition;
using Hexalith.Tenants.UI.Extensions;

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

// The standalone host composes exactly the module an embedding host composes. These registrations used to
// be duplicated here line for line, which let the two copies drift silently while only the module's copy
// was under test -- and what they register is security-relevant: the dedicated search cursor purpose, the
// circuit-scoped paging state, and the suppression of default HttpClient logging that would otherwise carry
// raw Memories queries and offsets.
//
// IUserContextAccessor is provided by the FrontComposer authentication bridge
// (ClaimsPrincipalUserContextAccessor), configured above with the eventstore:tenant / sub claim
// mapping. No tenant-specific accessor override is needed.
builder.Services.AddHexalithTenantsUiModule(builder.Configuration, authEnabled);

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
