using System.Security.Claims;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Testing;
using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Composition;
using Hexalith.Tenants.UI.Extensions;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantList;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests;

/// <summary>
/// Verifies that the Tenants UI assembly contributes renderable FrontComposer generated surfaces.
/// </summary>
public sealed class GeneratedTenantsSurfaceTests : FrontComposerTestBase
{
    /// <summary>Initializes the generated Tenants domain in the supported test bootstrap.</summary>
    public GeneratedTenantsSurfaceTests()
    {
        Services.AddFluxor(options => options.ScanTypes(
            typeof(TenantSummaryProjectionFeature),
            typeof(TenantSummaryProjectionReducers),
            typeof(CreateTenantCommandLifecycleFeature),
            typeof(CreateTenantCommandReducers)));
        Services.AddHexalithDomain<TenantsFrontComposerDomain>();
    }

    [Fact]
    public void Three_call_bootstrap_registers_one_tenants_manifest_with_generated_surfaces()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EventStore:BaseAddress"] = "https://eventstore.invalid",
            })
            .Build();
        ServiceCollection services = new();
        services.AddSingleton(configuration);
        services.AddLogging();

        services.AddHexalithFrontComposerQuickstart(
            options => options.ScanAssemblies(typeof(TenantsFrontComposerDomain).Assembly));
        services.AddHexalithDomain<TenantsFrontComposerDomain>();
        services.AddHexalithTenantsUiModule(configuration, enableGatewayAuthorization: false);

        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        IFrontComposerRegistry registry = provider.GetRequiredService<IFrontComposerRegistry>();
        DomainManifest manifest = registry.GetManifests()
            .Where(static entry => entry.BoundedContext == "tenants")
            .ShouldHaveSingleItem();

        manifest.Projections.ShouldBe([typeof(TenantSummaryProjection).FullName!]);
        manifest.Commands.ShouldBe([typeof(CreateTenantCommand).FullName!]);
        manifest.FullPageCommands.ShouldBeEmpty();
    }

    [Fact]
    public async Task Three_call_host_renders_generated_projection_route()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(
                Arg.Any<TenantListRequest>(),
                null,
                Arg.Any<CancellationToken>())
            .Returns(TenantListSnapshot.Ready(
                [TenantListRow.FromSummary(new TenantSummary("tenant-a", "Tenant A", TenantStatus.Active))],
                nextCursor: null,
                hasMore: false,
                eTag: null,
                ReadModelFreshnessState.Current,
                isDegraded: false));

        await using WebApplicationFactory<global::Program> factory = new();
        using WebApplicationFactory<global::Program> configured = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ITenantQueryGateway>();
                services.AddSingleton(gateway);
                services.RemoveAll<IUserContextAccessor>();
                services.AddSingleton<IUserContextAccessor>(new FrontComposerTestUserContextAccessor
                {
                    TenantId = "tenant-a",
                    UserId = "user-a",
                });
            }));
        using HttpClient client = configured.CreateClient();

        string html = await client.GetStringAsync("/tenants/tenant-summary-projection");

        html.ShouldContain("aria-rowcount=\"1\"");
        html.ShouldContain("data-fc-datagrid");
        html.ShouldContain("Generated form preview");
        html.ShouldContain("disabled");
    }

    [Fact]
    public async Task Generated_tenant_summary_projection_renders_tenant_columns_and_count()
    {
        await InitializeStoreAsync();

        Services.GetRequiredService<IDispatcher>().Dispatch(new TenantSummaryProjectionLoadedAction(
            "tenant-summary",
            [new TenantSummaryProjection { Id = "tenant-a", Name = "Tenant A", Status = TenantStatus.Active }]));

        IRenderedComponent<TenantSummaryProjectionView> cut = Render<TenantSummaryProjectionView>();

        GeneratedProjectionAssertions.AssertDataGridEnvelope(cut);
        GeneratedProjectionAssertions.AssertHeadersInOrder(cut, "Id", "Name", "Status");
        cut.Find("table").GetAttribute("aria-rowcount").ShouldBe("1");
    }

    [Fact]
    public async Task Generated_create_tenant_command_renders_required_inputs()
    {
        await InitializeStoreAsync();

        IRenderedComponent<CreateTenantCommandForm> cut = Render<CreateTenantCommandForm>();

        cut.Markup.ShouldContain("Tenant ID");
        cut.Markup.ShouldContain("Name");
    }

    [Fact]
    public async Task Manifest_projection_route_renders_authorized_tenant_summary()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(
                Arg.Any<TenantListRequest>(),
                null,
                Arg.Any<CancellationToken>())
            .Returns(TenantListSnapshot.Ready(
                [TenantListRow.FromSummary(new TenantSummary("tenant-a", "Tenant A", TenantStatus.Active))],
                nextCursor: null,
                hasMore: false,
                eTag: null,
                ReadModelFreshnessState.Current,
                isDegraded: false));
        Services.AddSingleton(gateway);
        await InitializeStoreAsync();

        IRenderedComponent<TenantSummaryProjectionPage> cut = Render<TenantSummaryProjectionPage>();

        cut.WaitForAssertion(() => cut.Find("table").GetAttribute("aria-rowcount").ShouldBe("1"));
        cut.Find("fieldset").HasAttribute("disabled").ShouldBeTrue();
        cut.Markup.ShouldContain("Generated form preview");
    }

    [Fact]
    public async Task Manifest_projection_route_hides_grid_when_read_is_unauthorized()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(
                Arg.Any<TenantListRequest>(),
                null,
                Arg.Any<CancellationToken>())
            .Returns(TenantListSnapshot.Unauthorized());
        Services.AddSingleton(gateway);
        await InitializeStoreAsync();

        IRenderedComponent<TenantSummaryProjectionPage> cut = Render<TenantSummaryProjectionPage>();

        cut.Markup.ShouldNotContain("data-fc-datagrid");
        cut.Markup.ShouldContain("Sign in to view tenants");
    }

    [Fact]
    public async Task Manifest_projection_route_retains_rows_when_read_is_stale()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(
                Arg.Any<TenantListRequest>(),
                null,
                Arg.Any<CancellationToken>())
            .Returns(TenantListSnapshot.Stale(
                [TenantListRow.FromSummary(new TenantSummary("tenant-a", "Tenant A", TenantStatus.Active))],
                eTag: null));
        Services.AddSingleton(gateway);
        await InitializeStoreAsync();

        IRenderedComponent<TenantSummaryProjectionPage> cut = Render<TenantSummaryProjectionPage>();

        cut.WaitForAssertion(() => cut.Find("table").GetAttribute("aria-rowcount").ShouldBe("1"));
        cut.Markup.ShouldContain("stale");
    }

    [Fact]
    public async Task Manifest_projection_route_clears_rows_on_authentication_change()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(
                Arg.Any<TenantListRequest>(),
                null,
                Arg.Any<CancellationToken>())
            .Returns(
                TenantListSnapshot.Ready(
                    [TenantListRow.FromSummary(new TenantSummary("tenant-a", "Tenant A", TenantStatus.Active))],
                    nextCursor: null,
                    hasMore: false,
                    eTag: null,
                    ReadModelFreshnessState.Current,
                    isDegraded: false),
                TenantListSnapshot.Unauthorized());
        Services.AddSingleton(gateway);
        MutableAuthenticationStateProvider authentication = new();
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        await InitializeStoreAsync();

        IRenderedComponent<TenantSummaryProjectionPage> cut = Render<TenantSummaryProjectionPage>();
        cut.WaitForAssertion(() => cut.Find("table").GetAttribute("aria-rowcount").ShouldBe("1"));

        authentication.PublishChange();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldNotContain("data-fc-datagrid");
            cut.Markup.ShouldContain("Sign in to view tenants");
            Services.GetRequiredService<IState<TenantSummaryProjectionState>>()
                .Value.Items.ShouldBeEmpty();
        });
    }

    private sealed class MutableAuthenticationStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));

        public void PublishChange()
            => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
