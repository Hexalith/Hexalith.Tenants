using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.EventStore.Client.Registration;
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

if (Uri.TryCreate(builder.Configuration["EventStore:BaseAddress"], UriKind.Absolute, out Uri? eventStoreBaseAddress)) {
    builder.Services.AddHexalithEventStore(o => o.BaseAddress = eventStoreBaseAddress);
    builder.Services.AddEventStoreGatewayClient(o => o.BaseAddress = eventStoreBaseAddress);
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
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
