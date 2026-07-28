using System.Globalization;
using System.Text.RegularExpressions;

using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Layout;

using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Components.Tenants;
using Hexalith.Tenants.UI.Components.Tenants.Configuration;
using Hexalith.Tenants.UI.Components.Tenants.Members;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services;
using Hexalith.Tenants.UI.Services.Configuration;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.TenantUsers;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class TenantDetailSurfaceTests : BunitContext
{
    // Protected search paging is a required scoped circuit service; the workspace fails loudly without it.
    public TenantDetailSurfaceTests()
    {
        Services.AddScoped<TenantSearchPagingState>();    }

    [Fact]
    public void Detail_page_loads_through_gateway_and_renders_operational_overview()
    {
        const string tenantId = "  tenant/%2F?x=é&glyph=о  ";
        const string memberId = "  user/%2F?x=é&glyph=о  ";
        TenantDetail detail = Detail(
            tenantId,
            new Dictionary<string, string> { ["billing.mode"] = "trial" },
            TenantStatus.Active,
            [
                new TenantMember(memberId, TenantRole.TenantOwner),
                new TenantMember("reader-user", TenantRole.TenantReader),
            ]);
        List<TenantDetailRequest> requests = [];
        RegisterServices(call =>
        {
            requests.Add(call.ArgAt<TenantDetailRequest>(0));
            return Task.FromResult(ReadyWithSafeConfiguration(detail));
        });
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        JSRuntimeInvocationHandler tenantWrite = module.SetupVoid("writeText", tenantId).SetVoidResult();
        JSRuntimeInvocationHandler memberWrite = module.SetupVoid("writeText", memberId).SetVoidResult();

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/tenants/tenant.alpha?returnUrl=%2Ftenants%3Fsearch%3Dalpha%26selected%3Dtenant.alpha");

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, tenantId));
        cut.WaitForElement("[data-testid='tenants-detail-identity']");

        requests.ShouldHaveSingleItem().TenantId.ShouldBe(tenantId);
        cut.Find("[data-testid='tenants-detail-back']").GetAttribute("href").ShouldBe("/tenants?search=alpha&selected=tenant.alpha");
        cut.Find("[data-testid='tenants-detail-truth-state']").TextContent.ShouldContain("Current");
        cut.Find(".tenant-detail__literal").TextContent.ShouldBe(tenantId);
        cut.Find("[data-testid='tenants-member-user-id'] code").TextContent.ShouldBe(memberId);
        cut.Find("[data-testid='tenants-edit-metadata-flow']").TextContent.ShouldContain("Alpha");
        cut.Find("[data-testid='tenants-detail-copy-reference']").GetAttribute("data-copy-kind").ShouldBe("TenantId");
        cut.Find("[data-testid='tenants-detail-copy-reference']").TextContent.ShouldContain("Copy");
        cut.Find("[data-testid='tenants-lifecycle-actions']");
        cut.Find("[data-testid='tenants-lifecycle-current-status']").TextContent.ShouldContain("Active");
        cut.Find("[data-testid='tenants-lifecycle-unavailable-reason']").TextContent.ShouldContain("TenantLifecycleStateAlreadySet");
        cut.Find("[data-testid='tenants-detail-member-summary']").TextContent.ShouldContain("2 members");
        cut.Find("[data-testid='tenants-detail-configuration-summary']").TextContent.ShouldContain("1 visible configuration keys");
        cut.Find("[data-testid='tenants-config-read-table']").TextContent.ShouldContain("billing.mode");
        cut.Find(".tenant-detail__literal").GetAttribute("aria-label").ShouldBe($"Full tenant identifier {tenantId}");
        cut.Markup.ShouldContain("aria-label=\"Tenant status Active\"");

        cut.Find("[data-surface-testid='tenants-detail-copy-reference']").Click();
        cut.WaitForAssertion(() => tenantWrite.Invocations.Count.ShouldBe(1));
        tenantWrite.Invocations.Single().Arguments[0].ShouldBe(tenantId);

        cut.Find("[data-surface-testid='tenants-member-copy-reference']").Click();
        cut.WaitForAssertion(() => memberWrite.Invocations.Count.ShouldBe(1));
        memberWrite.Invocations.Single().Arguments[0].ShouldBe(memberId);
    }

    [Fact]
    public void Detail_page_is_composed_from_the_frontcomposer_aggregate_detail_wrapper()
    {
        // cc-2026-06-21 extraction guard: the detail page reuses FcAggregateDetailPage<TItem> (the shared
        // FC-DTL chrome) and maps its ready snapshot onto the constrained-measure wrapper instead of a
        // Tenants-local detail page shell. Keeps the rebase from silently regressing.
        RegisterServices(_ => Task.FromResult(ReadyWithSafeConfiguration(Detail("tenant.alpha"))));

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-detail-identity']");

        FcAggregateDetailPage<TenantDetail> wrapper = cut.FindComponent<FcAggregateDetailPage<TenantDetail>>().Instance;
        wrapper.LayoutMode.ShouldBe(FcPageLayoutMode.Constrained);
        wrapper.State.ShouldBe(FcAggregateDetailState.Ready);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Detail_page_renders_unnamed_fallback_heading_for_blank_name_without_crashing(string blankName)
    {
        // Regression guard for P-DN1: a tenant persisted with a blank Name must not crash the page.
        // FcPageHeader.OnParametersSet throws ArgumentException on a blank Heading, so the success
        // branch supplies the localized "Unnamed tenant" fallback instead of binding the empty name.
        // Rendering the success identity surface at all proves FcPageHeader did not throw.
        TenantDetail unnamed = new(
            "tenant.alpha",
            blankName,
            "Tenant alpha description",
            TenantStatus.Active,
            [
                new TenantMember("owner-user", TenantRole.TenantOwner),
                new TenantMember("reader-user", TenantRole.TenantReader),
            ],
            new Dictionary<string, string> { ["billing.mode"] = "trial" },
            DateTimeOffset.Parse("2026-06-01T12:00:00Z", CultureInfo.InvariantCulture));
        RegisterServices(_ => Task.FromResult(
            TenantDetailSnapshot.Ready(unnamed, "\"etag\"", ReadModelFreshnessState.Current)));

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-detail-identity']");

        string expectedFallback = Services
            .GetRequiredService<IStringLocalizer<TenantsResources>>()["Tenants.Detail.UnnamedTenant"];
        IElement heading = cut.Find("h1[id='tenants-detail-identity']");
        heading.TextContent.ShouldNotBeNullOrWhiteSpace();
        heading.TextContent.Trim().ShouldBe(expectedFallback);
    }

    [Fact]
    public void Detail_page_displays_loading_until_gateway_completes()
    {
        TaskCompletionSource<TenantDetailSnapshot> detailResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RegisterServices(_ => detailResult.Task);

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));

        cut.Find("[data-testid='tenants-detail-loading']").TextContent.ShouldContain("loading", Case.Insensitive);

        detailResult.SetResult(TenantDetailSnapshot.Ready(Detail("tenant.alpha"), "\"etag\"", ReadModelFreshnessState.Current));

        cut.WaitForElement("[data-testid='tenants-detail-identity']");
        cut.Find("[data-testid='tenants-detail-identity']").TextContent.ShouldContain("tenant.alpha");
    }

    [Fact]
    public async Task Route_change_cancels_and_discards_the_obsolete_tenant_completion()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        var alphaResult = new TaskCompletionSource<TenantDetailSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken alphaCancellation = default;
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                TenantDetailRequest request = call.Arg<TenantDetailRequest>()
                    ?? throw new InvalidOperationException("A tenant detail request is required.");
                if (request.TenantId == "tenant.alpha")
                {
                    alphaCancellation = call.ArgAt<CancellationToken>(2);
                    return alphaResult.Task;
                }

                return Task.FromResult(TenantDetailSnapshot.Ready(
                    Detail("tenant.beta"),
                    "beta-etag",
                    ReadModelFreshnessState.Current,
                    ProjectionLifecycleState.Current,
                    "projection-v1"));
            });
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                TenantUsersRequest request = call.Arg<TenantUsersRequest>()
                    ?? throw new InvalidOperationException("A tenant-users request is required.");
                return Task.FromResult(TenantUsersSnapshot.Ready(
                    request.TenantId,
                    [new TenantMember(request.TenantId + "-member", TenantRole.TenantReader)],
                    nextCursor: null,
                    hasMore: false,
                    eTag: request.TenantId + "-members-etag",
                    projectionVersion: "projection-v1",
                    ReadModelFreshnessState.Current,
                    ProjectionLifecycleState.Current));
            });
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.Find("[data-testid='tenants-detail-loading']");

        cut.Render(parameters => parameters.Add(page => page.TenantId, "tenant.beta"));
        cut.WaitForAssertion(() =>
        {
            alphaCancellation.IsCancellationRequested.ShouldBeTrue();
            cut.Find(".tenant-detail__literal").TextContent.ShouldBe("tenant.beta");
            cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("tenant.beta-member");
        });

        alphaResult.SetResult(TenantDetailSnapshot.Ready(
            Detail("tenant.alpha"),
            "alpha-etag",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            "projection-v1"));
        await Task.Yield();

        cut.Find(".tenant-detail__literal").TextContent.ShouldBe("tenant.beta");
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldNotContain("tenant.alpha-member");
    }

    [Fact]
    public async Task Matching_notification_retains_member_rows_and_triggers_only_the_authoritative_tenant_reads()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        IProjectionSubscription backendSubscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        backendSubscription
            .SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        TenantDetailSnapshot initialDetail = ReadyWithSafeConfiguration(
            Detail("tenant.alpha"),
            ProjectionLifecycleState.Current,
            "projection-v1");
        TenantUsersSnapshot initialMembers = TenantUsersSnapshot.Ready(
            "tenant.alpha",
            [new TenantMember("last-confirmed-user", TenantRole.TenantReader)],
            nextCursor: null,
            hasMore: false,
            eTag: "members-v1-etag",
            projectionVersion: "projection-v1",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current);
        var refreshedDetail = new TaskCompletionSource<TenantDetailSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var refreshedMembers = new TaskCompletionSource<TenantUsersSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        List<TenantDetailRequest> detailRequests = [];
        List<TenantUsersRequest> memberRequests = [];

        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                detailRequests.Add(call.Arg<TenantDetailRequest>()!);
                return detailRequests.Count == 1 ? Task.FromResult(initialDetail) : refreshedDetail.Task;
            });
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                memberRequests.Add(call.Arg<TenantUsersRequest>()!);
                return memberRequests.Count == 1 ? Task.FromResult(initialMembers) : refreshedMembers.Task;
            });

        Services.AddSingleton(gateway);
        Services.AddSingleton(backendSubscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-member-table']");
        await backendSubscription.Received(1)
            .SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());

        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.beta");
        await Task.Yield();
        detailRequests.Count.ShouldBe(1);
        memberRequests.Count.ShouldBe(1);

        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.alpha");
        cut.WaitForAssertion(() =>
        {
            detailRequests.Count.ShouldBe(2);
            memberRequests.Count.ShouldBe(2);
            cut.Find("[data-testid='tenants-member-refreshing']");
            cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("last-confirmed-user");
        });

        detailRequests[1].ETag.ShouldBe(initialDetail.ETag);
        memberRequests[1].ETag.ShouldBe(initialMembers.ETag);
        memberRequests[1].Cursor.ShouldBeNull();

        refreshedDetail.SetResult(initialDetail);
        refreshedMembers.SetResult(TenantUsersSnapshot.Ready(
            "tenant.alpha",
            [new TenantMember("authoritative-refreshed-user", TenantRole.TenantReader)],
            nextCursor: null,
            hasMore: false,
            eTag: "members-v2-etag",
            projectionVersion: "projection-v1",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current));

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("authoritative-refreshed-user");
            cut.FindAll("[data-testid='tenants-member-refreshing']").ShouldBeEmpty();
        });
    }

    [Theory]
    [InlineData("https%3A%2F%2Fexample.test%2Ftenants")]
    [InlineData("%2F%2Fevil.test%2Ftenants")]
    [InlineData("%2Fglobal-administrators")]
    public void Detail_page_rejects_unsafe_return_url(string encodedReturnUrl)
    {
        RegisterServices(_ => Task.FromResult(TenantDetailSnapshot.Ready(
            Detail("tenant.alpha"),
            "\"etag\"",
            ReadModelFreshnessState.Current)));

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"/tenants/tenant.alpha?returnUrl={encodedReturnUrl}");

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-detail-identity']");

        cut.Find("[data-testid='tenants-detail-back']").GetAttribute("href").ShouldBe("/tenants");
    }

    [Theory]
    [InlineData(TenantDetailSurfaceKind.Stale, "tenants-detail-stale", "stale")]
    [InlineData(TenantDetailSurfaceKind.Degraded, "tenants-detail-degraded", "degraded")]
    [InlineData(TenantDetailSurfaceKind.Unauthorized, "tenants-detail-unauthorized", "authorized")]
    [InlineData(TenantDetailSurfaceKind.NotFound, "tenants-detail-error", "not found")]
    [InlineData(TenantDetailSurfaceKind.Unavailable, "tenants-detail-error", "unavailable")]
    [InlineData(TenantDetailSurfaceKind.Unknown, "tenants-detail-error", "unavailable")]
    public void Detail_page_renders_distinct_safe_states(
        TenantDetailSurfaceKind kind,
        string selector,
        string expectedText)
    {
        RegisterServices(_ => Task.FromResult(Snapshot(kind)));

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement($"[data-testid='{selector}']");

        cut.Find($"[data-testid='{selector}']").TextContent.ShouldContain(expectedText, Case.Insensitive);
        if (kind is TenantDetailSurfaceKind.Unauthorized or TenantDetailSurfaceKind.NotFound or TenantDetailSurfaceKind.Unavailable or TenantDetailSurfaceKind.Unknown)
        {
            cut.Markup.ShouldNotContain("Tenant alpha description");
            cut.Markup.ShouldNotContain("tenants-config-table");
        }
    }

    [Fact]
    public void Detail_page_fails_closed_to_unavailable_when_degraded_snapshot_has_no_payload()
    {
        // A Degraded surface with a NULL detail payload (reachable via an unmapped gateway status ->
        // Degraded(null, ...) or a 304-not-modified with no cached detail) must NOT render the degraded
        // body. The FcAggregateDetailPage state mapping fails closed to Unavailable so a projection-less
        // state is never dressed as the ready surface. The payload-carrying Degraded case does not cover
        // this; this pins the fail-closed edge.
        RegisterServices(_ => Task.FromResult(
            TenantDetailSnapshot.Degraded(null, "Tenant detail query gateway returned a safe degraded state.")));

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-detail-error']");

        cut.Find("[data-testid='tenants-detail-error']").TextContent.ShouldContain("unavailable", Case.Insensitive);
        cut.FindAll("[data-testid='tenants-detail-degraded']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-detail-identity']").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("tenants-config-table");
    }

    [Fact]
    public void Configuration_read_contract_accepts_only_the_safe_model_and_read_state()
    {
        Type component = typeof(TenantConfigurationView);

        component.GetProperty("Model").ShouldNotBeNull();
        component.GetProperty("Detail").ShouldBeNull();
        component.GetProperty("ProjectionEvidenceProvider").ShouldBeNull();
        component.GetProperty("RemoveProjectionEvidenceProvider").ShouldBeNull();
        component.GetProperty("OnCommandActivityChanged").ShouldBeNull();
    }

    [Theory]
    [InlineData(typeof(SetTenantConfigurationFlow))]
    [InlineData(typeof(RemoveTenantConfigurationFlow))]
    public void Configuration_management_flows_accept_only_safe_context_and_proof_contracts(Type component)
    {
        ArgumentNullException.ThrowIfNull(component);
        component.GetProperty("Context").ShouldNotBeNull();
        component.GetProperty("Detail").ShouldBeNull();
        component.GetProperty("ProjectionEvidenceProvider").ShouldNotBeNull();
        component.GetProperty("ReauthorizeProvider").ShouldNotBeNull();
    }

    [Fact]
    public void Configuration_read_view_renders_only_positive_safe_rows_without_mutation_or_copy_affordances()
    {
        RegisterComponentServices();
        TenantConfigurationSafeModel model = SafeConfiguration(
            ("billing", "billing.mode", "trial"),
            ("feature", "feature", "enabled"));

        IRenderedComponent<TenantConfigurationView> cut = Render<TenantConfigurationView>(parameters => parameters
            .Add(view => view.Model, model)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current));

        cut.FindAll("[data-testid='tenants-config-read-group']").Count.ShouldBe(2);
        cut.Find("[data-testid='tenants-config-read-table']").TextContent.ShouldContain("billing.mode");
        cut.Find("[data-testid='tenants-config-read-table']").TextContent.ShouldContain("trial");
        cut.Find("[data-testid='tenants-config-read-table']").TextContent.ShouldContain("feature");
        cut.Markup.ShouldContain("aria-label=\"Full configuration key billing.mode\"");
        cut.Markup.ShouldContain("aria-label=\"Visible configuration value trial\"");
        cut.Markup.ShouldNotContain("tenants-config-set-flow");
        cut.Markup.ShouldNotContain("tenants-config-remove");
        cut.Markup.ShouldNotContain("<form", Case.Insensitive);
        cut.FindAll("[data-copy-kind='ConfigurationKey']").ShouldBeEmpty();
        cut.FindAll("[data-copy-kind='SafeConfigurationValue']").ShouldBeEmpty();
    }

    [Fact]
    public void Configuration_read_view_keeps_valid_empty_policy_unavailable_and_filtered_empty_distinct()
    {
        RegisterComponentServices();
        IRenderedComponent<TenantConfigurationView> unavailable = Render<TenantConfigurationView>(parameters => parameters
            .Add(view => view.Model, TenantConfigurationSafeModel.Unavailable("tenant.alpha"))
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current));

        unavailable.Find("[data-testid='tenants-config-read-state']").TextContent.ShouldContain("unavailable", Case.Insensitive);
        unavailable.Markup.ShouldNotContain("tenants-config-read-empty");
        unavailable.Markup.ShouldNotContain("visible configuration entries", Case.Insensitive);

        IRenderedComponent<TenantConfigurationView> empty = Render<TenantConfigurationView>(parameters => parameters
            .Add(view => view.Model, SafeConfiguration())
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current));

        empty.Find("[data-testid='tenants-config-read-empty']").TextContent.ShouldContain("No visible configuration");
        empty.Markup.ShouldNotContain("tenants-config-read-filtered-empty");

        IRenderedComponent<TenantConfigurationView> filtered = Render<TenantConfigurationView>(parameters => parameters
            .Add(view => view.Model, SafeConfiguration(("billing", "billing.mode", "trial")))
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current));

        filtered.Find("[data-testid='tenants-config-read-filter']").Change("missing");

        filtered.Find("[data-testid='tenants-config-read-filtered-empty']").TextContent.ShouldContain("No visible configuration matches");
        filtered.Find("[data-testid='tenants-config-read-announcer']").TextContent.ShouldContain("0 visible configuration entries");
        filtered.Find("[data-testid='tenants-config-read-clear-filter']").Click();
        filtered.Find("[data-testid='tenants-config-read-table']").TextContent.ShouldContain("billing.mode");
    }

    [Fact]
    public void Configuration_read_view_preserves_literal_namespace_unicode_accessibility_and_responsive_overflow()
    {
        RegisterComponentServices();
        IRenderedComponent<TenantConfigurationView> cut = Render<TenantConfigurationView>(parameters => parameters
            .Add(view => view.Model, SafeConfiguration(
                ("billing..Α", "billing..Α.<script>\u202E", "値🙂<literal>"),
                ("identity", "identity.region", "eu")))
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-read-filter']").Change("billing");

        cut.FindAll("[data-testid='tenants-config-read-group']").Count.ShouldBe(1);
        cut.Find("[data-testid='tenants-config-read-table']").TextContent.ShouldContain("Namespace billing..Α");
        cut.Find("[data-testid='tenants-config-read-table']").TextContent.ShouldContain("billing..Α.<script>\u202E");
        cut.Find("[data-testid='tenants-config-read-table']").TextContent.ShouldContain("値🙂<literal>");
        cut.Find("[data-testid='tenants-config-read-table']").TextContent.ShouldNotContain("identity.region");
        cut.Find("[data-testid='tenants-config-read-truth-state']").TextContent.ShouldContain("Current");
        cut.Find(".tenant-config__scope").TextContent.ShouldContain("approved by the current namespace and display policy");
        cut.Find("[data-testid='tenants-config-read-announcer']").TextContent.ShouldContain("1 visible configuration entries");
        cut.Find("[data-testid='tenants-config-read-filter']").GetAttribute("aria-describedby").ShouldBe("tenants-config-read-filter-help");
        cut.Find("[data-testid='tenants-config-read-clear-filter']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-config-read-table']").GetAttribute("tabindex").ShouldBe("0");
        cut.Find("[data-testid='tenants-config-read-table']").GetAttribute("aria-label").ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(TenantDetailSurfaceKind.Stale, ReadModelFreshnessState.Stale, "stale")]
    [InlineData(TenantDetailSurfaceKind.Degraded, ReadModelFreshnessState.Unknown, "degraded")]
    [InlineData(TenantDetailSurfaceKind.Unknown, ReadModelFreshnessState.Unknown, "Unknown")]
    public void Configuration_read_view_surfaces_non_current_truth_without_collapsing_to_success(
        TenantDetailSurfaceKind kind,
        ReadModelFreshnessState freshness,
        string expectedText)
    {
        RegisterComponentServices();

        IRenderedComponent<TenantConfigurationView> cut = Render<TenantConfigurationView>(parameters => parameters
            .Add(view => view.Model, SafeConfiguration(("billing", "billing.mode", "trial")))
            .Add(view => view.SurfaceKind, kind)
            .Add(view => view.Freshness, freshness));

        cut.Find("[data-testid='tenants-config-read-truth-state']").TextContent.ShouldContain(expectedText, Case.Insensitive);
        cut.Markup.ShouldNotContain("Success");
    }

    [Fact]
    public void Configuration_management_is_a_sibling_landmark_with_safe_targets_and_target_specific_actions()
    {
        RegisterComponentServices();
        TenantConfigurationSafeRow row = new("billing", "billing.mode", "trial");
        TenantConfigurationManagementContext context = TenantConfigurationManagementContext.Available(
            "tenant.alpha",
            TenantStatus.Active,
            false,
            ["billing"],
            [row]);

        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(component => component.Context, context)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-management-section']");
        cut.Find("[data-testid='tenants-config-set-flow']");
        cut.Find("[data-testid='tenants-config-management-targets']").GetAttribute("aria-label").ShouldNotBeNullOrWhiteSpace();
        IElement removeAction = cut.Find("[data-testid='tenants-config-management-remove-open']");
        string accessibleName = removeAction.GetAttribute("aria-label") ?? string.Empty;
        accessibleName.ShouldContain("billing.mode");

        removeAction.Click();
        cut.Find("[data-testid='tenants-config-remove-flow']");
        cut.Find("[data-testid='tenants-config-remove-cancel']").Click();
        cut.FindAll("[data-testid='tenants-config-remove-flow']").ShouldBeEmpty();
    }

    [Fact]
    public void Configuration_management_returns_focus_to_the_launching_control_when_the_remove_flow_closes()
    {
        // Restores the focus-return regression guard dropped by the read/management split, which
        // test-summary.md still claimed was covered. The launching control is refocused through an
        // ElementReference, so a JS focus invocation is the observable evidence.
        RegisterComponentServices();
        TenantConfigurationSafeRow row = new("billing", "billing.mode", "trial");
        TenantConfigurationManagementContext context = TenantConfigurationManagementContext.Available(
            "tenant.alpha",
            TenantStatus.Active,
            false,
            ["billing"],
            [row]);

        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(component => component.Context, context)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-management-remove-open']").Click();
        int focusCallsBeforeClose = JSInterop.Invocations.Count(invocation =>
            invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));

        cut.Find("[data-testid='tenants-config-remove-cancel']").Click();

        JSInterop.Invocations
            .Count(invocation => invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase))
            .ShouldBeGreaterThan(focusCallsBeforeClose);
    }

    [Fact]
    public void Configuration_management_can_remove_an_authorized_key_that_contains_no_separator()
    {
        // Grant `P` authorizes exact key `P` as well as `P.*`, so a dotless key is a legitimate safe
        // row. The remove flow derived its namespace as empty for such a key and opened permanently
        // blocked on scope, while the set flow handled the same key correctly.
        RegisterComponentServices();
        TenantConfigurationSafeRow row = new("maintenance", "maintenance", "on");
        TenantConfigurationManagementContext context = TenantConfigurationManagementContext.Available(
            "tenant.alpha",
            TenantStatus.Active,
            false,
            ["maintenance"],
            [row]);

        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(component => component.Context, context)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-management-remove-open']").Click();

        cut.Find("[data-testid='tenants-config-remove-flow']");
        cut.FindAll("[data-testid='tenants-config-remove-preview-blocked']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-config-remove-preview-namespace']").TextContent.ShouldContain("maintenance");
    }

    [Fact]
    public void Configuration_management_distinguishes_an_unverifiable_policy_from_a_policy_that_grants_nothing()
    {
        // A valid policy granting this caller nothing is not a verification failure. Reporting it as one
        // contradicted the sibling read landmark, which correctly showed authorization-safe empty.
        RegisterComponentServices();

        IRenderedComponent<TenantConfigurationManagement> noScope = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(component => component.Context, TenantConfigurationManagementContext.Available(
                "tenant.alpha",
                TenantStatus.Active,
                false,
                [],
                []))
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current));

        string noScopeText = noScope.Find("[data-testid='tenants-config-management-unavailable']").TextContent;
        noScopeText.ShouldContain("no configuration namespace is granted", Case.Insensitive);
        noScopeText.ShouldNotContain("cannot be verified", Case.Insensitive);

        IRenderedComponent<TenantConfigurationManagement> unverifiable = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(component => component.Context, TenantConfigurationManagementContext.Unavailable("tenant.alpha"))
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current));

        unverifiable.Find("[data-testid='tenants-config-management-unavailable']").TextContent
            .ShouldContain("cannot be verified", Case.Insensitive);
    }

    [Fact]
    public void Configuration_management_keeps_valid_empty_and_unavailable_states_mutually_exclusive()
    {
        RegisterComponentServices();
        TenantConfigurationManagementContext empty = TenantConfigurationManagementContext.Available(
            "tenant.alpha",
            TenantStatus.Active,
            false,
            ["billing"],
            []);

        IRenderedComponent<TenantConfigurationManagement> validEmpty = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(component => component.Context, empty)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current));

        validEmpty.Find("[data-testid='tenants-config-management-empty']");
        validEmpty.Find("[data-testid='tenants-config-set-flow']");
        validEmpty.FindAll("[data-testid='tenants-config-management-unavailable']").ShouldBeEmpty();

        IRenderedComponent<TenantConfigurationManagement> policyUnavailable = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(component => component.Context, TenantConfigurationManagementContext.Unavailable("tenant.alpha"))
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current));

        policyUnavailable.Find("[data-testid='tenants-config-management-unavailable']");
        policyUnavailable.FindAll("[data-testid='tenants-config-management-empty']").ShouldBeEmpty();
        policyUnavailable.FindAll("[data-testid='tenants-config-set-flow']").ShouldBeEmpty();

        IRenderedComponent<TenantConfigurationManagement> stale = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(component => component.Context, empty)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Stale)
            .Add(component => component.Freshness, ReadModelFreshnessState.Stale));

        stale.Find("[data-testid='tenants-config-management-unavailable']");
        stale.FindAll("[data-testid='tenants-config-management-empty']").ShouldBeEmpty();
        stale.FindAll("[data-testid='tenants-config-set-flow']").ShouldBeEmpty();
    }

    [Fact]
    public void Detail_page_composes_member_access_review_without_replacing_existing_surfaces()
    {
        RegisterServices(_ => Task.FromResult(ReadyWithSafeConfiguration(Detail("tenant.alpha"))));

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-member-table']");

        cut.Find("[data-testid='tenants-detail-member-summary']").TextContent.ShouldContain("2 members");
        cut.Find("[data-testid='tenants-member-section']").TextContent.ShouldContain("Member access review");
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("owner-user");
        cut.Find("[data-testid='tenants-config-read-table']").TextContent.ShouldContain("billing.mode");
        cut.Markup.ShouldContain("data-testid=\"tenants-member-truth-badge\"");
        cut.Markup.ShouldNotContain("tenants-list-truth-state");
    }

    [Fact]
    public void Detail_page_keeps_member_actions_fail_closed_when_routed_detail_is_stale()
    {
        RegisterServices(_ => Task.FromResult(TenantDetailSnapshot.Stale(Detail("tenant.alpha"), "\"etag\"")));

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-member-table']");

        cut.Find("[data-testid='tenants-detail-stale']").TextContent.ShouldContain("stale", Case.Insensitive);
        IElement memberSection = cut.Find("[data-testid='tenants-member-section']");
        memberSection.TextContent.ShouldContain("stale data", Case.Insensitive);
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("owner-user");
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("stale data");
        cut.Find("[data-testid='tenants-add-member-flow']").TextContent.ShouldContain("Refresh current tenant detail");
        cut.Find("[data-testid='tenants-add-member-submit']").GetAttribute("disabled").ShouldNotBeNull();
        cut.FindAll("[data-testid='tenants-member-action-slot']")
            .ShouldAllBe(static slot => slot.TextContent.Contains("Unavailable", StringComparison.OrdinalIgnoreCase));
        memberSection.TextContent.ShouldNotContain("Success");
    }

    [Fact]
    public void Member_access_review_renders_literal_members_roles_context_and_accessible_table_semantics()
    {
        RegisterComponentServices();
        TenantDetail detail = Detail(
            "tenant.alpha",
            new Dictionary<string, string>(),
            TenantStatus.Active,
            [
                new TenantMember("OWNER/User.01", TenantRole.TenantOwner),
                new TenantMember("reader-user-with-a-very-long-literal-identifier", TenantRole.TenantReader),
                new TenantMember("contributor-user", TenantRole.TenantContributor),
                new TenantMember("unknown-role-user", TenantRole.Unknown),
            ]);

        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, MemberSnapshot(detail)));

        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("OWNER/User.01");
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("reader-user-with-a-very-long-literal-identifier");
        cut.FindAll("[data-testid='tenants-member-copy-reference']").Count.ShouldBe(4);
        cut.FindAll("[data-testid='tenants-member-copy-reference']").ShouldAllBe(static copy => copy.GetAttribute("data-copy-kind") == "UserId");
        cut.FindAll("[data-testid='tenants-member-row']").Count.ShouldBe(4);
        cut.FindAll("[data-testid='tenants-member-user-id']").Count.ShouldBe(4);
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("User");
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("Role");
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("Change role");
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("Remove member");
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("Tenant owner");
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("Tenant contributor");
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("Tenant reader");
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("Unknown role");
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("1 visible owner");
        cut.Find("[data-testid='tenants-member-truth-state']").TextContent.ShouldContain("Current");
        cut.Markup.ShouldContain("aria-describedby=\"tenants-member-reasons-0\"");
        cut.Markup.ShouldContain("aria-label=\"Literal member user identifier OWNER/User.01\"");
        cut.Find("[data-testid='tenants-member-reason-list']").GetAttribute("tabindex").ShouldBe("0");
    }

    [Fact]
    public void Member_access_review_associates_every_action_slot_with_its_row_reason_list()
    {
        RegisterComponentServices();
        TenantDetail detail = Detail(
            "tenant.alpha",
            new Dictionary<string, string>(),
            TenantStatus.Active,
            [
                new TenantMember("owner-user", TenantRole.TenantOwner),
                new TenantMember("reader-user", TenantRole.TenantReader),
            ]);

        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, MemberSnapshot(detail)));

        HashSet<string> reasonListIds = cut.FindAll("[data-testid='tenants-member-reason-list']")
            .Select(static list => list.GetAttribute("id").ShouldNotBeNull())
            .ToHashSet(StringComparer.Ordinal);

        reasonListIds.Count.ShouldBe(2);
        cut.FindAll("[data-testid='tenants-member-action-slot']").Count.ShouldBe(2);
        cut.FindAll("[data-testid='tenants-change-role-open']").Count.ShouldBe(2);
        cut.FindAll("[data-testid='tenants-remove-member-open']").Count.ShouldBe(2);
        foreach (IElement slot in cut.FindAll("[data-testid='tenants-member-action-slot']"))
        {
            string describedBy = slot.GetAttribute("aria-describedby").ShouldNotBeNull();
            reasonListIds.ShouldContain(describedBy);
            slot.GetAttribute("aria-label").ShouldNotBeNull().ShouldContain("unavailable");
        }

        cut.FindAll("[data-testid='tenants-change-role-open']")
            .ShouldAllBe(static button => button.GetAttribute("aria-controls") == "tenants-change-role-flow-region");
        cut.FindAll("[data-testid='tenants-remove-member-open']")
            .ShouldAllBe(static button => button.GetAttribute("aria-controls") == "tenants-remove-member-flow-region");
        cut.FindAll("[data-testid='tenants-member-row']")
            .ShouldAllBe(static row => row.GetAttribute("tabindex") == "0");
    }

    [Theory]
    [InlineData("tenants-change-role-open", "tenants-change-role-flow-region", "tenants-change-role-flow")]
    [InlineData("tenants-remove-member-open", "tenants-remove-member-flow-region", "tenants-remove-member-flow")]
    public void Member_access_review_aria_controls_resolves_to_rendered_region_after_open(
        string launchTestId,
        string regionId,
        string flowTestId)
    {
        RegisterComponentServices();
        TenantDetail detail = Detail("tenant.alpha");
        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, MemberSnapshot(detail)));

        IElement launch = cut.FindAll($"[data-testid='{launchTestId}']")[0];
        launch.GetAttribute("aria-controls").ShouldBe(regionId);
        // The controlled region is not rendered until the launcher is activated.
        cut.FindAll($"#{regionId}").ShouldBeEmpty();

        launch.Click();

        // After the FluentStack migration the active region must still expose the id that aria-controls
        // names, so the relationship resolves to a real rendered target (not a dangling reference).
        IElement region = cut.Find($"#{regionId}");
        region.Id.ShouldBe(regionId);
        region.QuerySelector($"[data-testid='{flowTestId}']").ShouldNotBeNull(
            $"aria-controls on {launchTestId} must point to the rendered {regionId} region containing the flow.");
    }

    [Fact]
    public void Member_access_review_surfaces_all_canonical_unavailable_reason_categories_without_mutation_affordances()
    {
        RegisterComponentServices();
        TenantDetail detail = Detail("tenant.alpha");
        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, MemberSnapshot(detail)));

        string[] categories =
        [
            "missing permission",
            "stale data",
            "missing lifecycle support",
            "missing consequence preview",
            "missing audit proof",
            "high-impact flow not ready",
        ];

        foreach (string category in categories)
        {
            cut.Find($"[data-testid='tenants-member-reason-{category.Replace(' ', '-')}']")
                .TextContent.ShouldContain(category);
        }

        cut.FindAll("[data-testid='tenants-member-action-slot']").Count.ShouldBeGreaterThanOrEqualTo(2);
        cut.FindAll("[data-testid='tenants-member-unavailable-reason']").Count.ShouldBeGreaterThanOrEqualTo(4);
        cut.Find("[data-testid='tenants-add-member-flow']");
        cut.Find("[data-testid='tenants-add-member-submit']").GetAttribute("type").ShouldBe("submit");
        cut.FindAll("[data-testid='tenants-change-role-open']").Count.ShouldBe(2);
        cut.FindAll("[data-testid='tenants-remove-member-open']").Count.ShouldBe(2);
        cut.FindAll("[data-testid='tenants-member-action-slot']")
            .ShouldAllBe(static slot => slot.TextContent.Contains("Unavailable", StringComparison.OrdinalIgnoreCase));
        cut.Markup.ShouldNotContain("command payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("accepted", Case.Insensitive);
        cut.Markup.ShouldNotContain("confirmed", Case.Insensitive);
    }

    [Fact]
    public void Member_access_review_opens_change_role_flow_without_removing_add_or_remove_action_slots()
    {
        RegisterComponentServices();
        TenantDetail detail = Detail("tenant.alpha");
        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, MemberSnapshot(detail)));

        cut.Find("[data-testid='tenants-change-role-open']").Click();

        cut.Find("[data-testid='tenants-change-role-flow']");
        cut.Find("[data-testid='tenants-change-role-user-id']").TextContent.ShouldContain("owner-user");
        cut.Find("[data-testid='tenants-change-role-current-role']").TextContent.ShouldContain("Tenant owner");
        cut.Find("[data-testid='tenants-change-role-new-role']");
        cut.Find("[data-testid='tenants-change-role-submit']").GetAttribute("type").ShouldBe("submit");
        cut.Find("[data-testid='tenants-change-role-lifecycle']");
        cut.FindAll("[data-testid='tenants-member-action-slot']").Count.ShouldBe(2);
        cut.FindAll("[data-testid='tenants-remove-member-open']").Count.ShouldBe(2);
        cut.Find("[data-testid='tenants-add-member-flow']");
        cut.Find("[data-testid='tenants-add-member-submit']").GetAttribute("type").ShouldBe("submit");
        cut.Markup.ShouldContain("Remove member", Case.Insensitive);
    }

    [Theory]
    [InlineData(TenantStatus.Disabled, TenantDetailSurfaceKind.Ready, ReadModelFreshnessState.Current, "missing lifecycle support")]
    [InlineData(TenantStatus.Active, TenantDetailSurfaceKind.Stale, ReadModelFreshnessState.Stale, "stale data")]
    [InlineData(TenantStatus.Active, TenantDetailSurfaceKind.Ready, ReadModelFreshnessState.Unknown, "stale data")]
    [InlineData(TenantStatus.Active, TenantDetailSurfaceKind.Degraded, ReadModelFreshnessState.Current, "stale data")]
    [InlineData(TenantStatus.Active, TenantDetailSurfaceKind.Unavailable, ReadModelFreshnessState.Current, "stale data")]
    [InlineData(TenantStatus.Active, TenantDetailSurfaceKind.Unknown, ReadModelFreshnessState.Current, "stale data")]
    [InlineData(TenantStatus.Unknown, TenantDetailSurfaceKind.Ready, ReadModelFreshnessState.Current, "missing lifecycle support")]
    public void Member_access_review_fails_closed_for_disabled_stale_unknown_and_degraded_states(
        TenantStatus status,
        TenantDetailSurfaceKind surfaceKind,
        ReadModelFreshnessState freshness,
        string expectedReason)
    {
        RegisterComponentServices();
        TenantDetail detail = Detail(
                "tenant.alpha",
                new Dictionary<string, string>(),
                status,
                [new TenantMember("owner-user", TenantRole.TenantOwner)]);
        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, surfaceKind)
            .Add(view => view.Freshness, freshness)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, MemberSnapshot(detail)));

        cut.Find("[data-testid='tenants-member-section']").TextContent.ShouldContain(expectedReason);
        if (surfaceKind is TenantDetailSurfaceKind.Degraded or TenantDetailSurfaceKind.Unavailable or TenantDetailSurfaceKind.Unknown)
        {
            cut.FindAll("[data-testid='tenants-member-unavailable-reason']")
                .ShouldAllBe(static reason => !reason.TextContent.Contains("missing permission", StringComparison.OrdinalIgnoreCase));
        }

        cut.FindAll("[data-testid='tenants-member-action-slot']")
            .ShouldAllBe(static slot => slot.TextContent.Contains("Unavailable", StringComparison.OrdinalIgnoreCase));
        // Visible text only — avoids the Fluent success-color token false positive (see VisibleText).
        cut.VisibleText().ShouldNotContain("Success", Case.Insensitive);
    }

    [Fact]
    public void Member_access_review_true_authorization_failure_still_renders_missing_permission()
    {
        RegisterComponentServices();
        TenantDetail detail = Detail("tenant.alpha");
        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Unauthorized)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, MemberSnapshot(detail)));

        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("missing permission");
        cut.FindAll("[data-testid='tenants-member-action-slot']")
            .ShouldAllBe(static slot => slot.TextContent.Contains("Unavailable", StringComparison.OrdinalIgnoreCase));
        cut.Markup.ShouldNotContain("accepted", Case.Insensitive);
        cut.Markup.ShouldNotContain("confirmed", Case.Insensitive);
    }

    [Fact]
    public void Member_access_review_does_not_render_backend_urls_tokens_payloads_or_command_lifecycle_context()
    {
        RegisterComponentServices();
        TenantDetail detail = Detail(
                "tenant.alpha",
                new Dictionary<string, string>(),
                TenantStatus.Active,
                [new TenantMember("literal-user", TenantRole.TenantOwner)]);
        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, MemberSnapshot(detail)));

        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("literal-user");
        cut.Markup.ShouldNotContain("/api/tenants", Case.Insensitive);
        cut.Markup.ShouldNotContain("Bearer ", Case.Insensitive);
        cut.Markup.ShouldNotContain("jwt", Case.Insensitive);
        cut.Markup.ShouldNotContain("command payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("correlation", Case.Insensitive);
        cut.Markup.ShouldNotContain("audit available", Case.Insensitive);
        cut.Markup.ShouldNotContain("preview available", Case.Insensitive);
    }

    [Fact]
    public void Member_access_review_renders_authorization_safe_empty_state()
    {
        RegisterComponentServices();
        TenantDetail detail = Detail(
                "tenant.empty",
                new Dictionary<string, string>(),
                TenantStatus.Active,
                []);
        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Unknown)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, MemberSnapshot(detail)));

        cut.Find("[data-testid='tenants-member-empty']").TextContent.ShouldContain("No visible members");
        cut.Find("[data-testid='tenants-member-empty']").TextContent.ShouldContain("does not reveal hidden memberships");
        cut.Find("[data-testid='tenants-member-empty']").TextContent.ShouldContain("stale data");
        cut.Markup.ShouldNotContain("tenants-member-row");
    }

    [Fact]
    public void Member_access_review_keeps_rows_readable_but_actions_unavailable_for_version_mismatch()
    {
        RegisterComponentServices();
        TenantDetail detail = Detail("tenant.alpha");
        TenantUsersSnapshot members = MemberSnapshot(detail) with { ProjectionVersion = "members-v2" };

        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "detail-v1")
            .Add(view => view.Members, members));

        cut.FindAll("[data-testid='tenants-member-row']").Count.ShouldBe(detail.Members.Count);
        cut.FindAll("[data-testid='tenants-change-role-open']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-remove-member-open']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("stale data");
    }

    [Fact]
    public void Detail_member_rows_and_paging_use_only_the_dedicated_tenant_users_snapshot_and_exact_cursor_history()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        List<TenantUsersRequest> memberRequests = [];
        TenantDetail embeddedDetail = Detail(
            "tenant.alpha",
            new Dictionary<string, string>(),
            TenantStatus.Active,
            [new TenantMember("embedded-should-not-render", TenantRole.TenantOwner)]);
        TenantDetailSnapshot detailSnapshot = ReadyWithSafeConfiguration(
            embeddedDetail,
            ProjectionLifecycleState.Current,
            "projection-v1");
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(detailSnapshot));
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                TenantUsersRequest request = call.Arg<TenantUsersRequest>()
                    ?? throw new InvalidOperationException("A tenant-users request is required.");
                memberRequests.Add(request);
                return Task.FromResult(request.Cursor switch
                {
                    "cursor-page-2" => TenantUsersSnapshot.Ready(
                        "tenant.alpha",
                        [new TenantMember("page-2-user", TenantRole.TenantReader)],
                        nextCursor: null,
                        hasMore: false,
                        eTag: "members-page-2",
                        projectionVersion: "projection-v1",
                        ReadModelFreshnessState.Current,
                        ProjectionLifecycleState.Current),
                    _ => TenantUsersSnapshot.Ready(
                        "tenant.alpha",
                        [new TenantMember("page-1-user", TenantRole.TenantOwner)],
                        nextCursor: "cursor-page-2",
                        hasMore: true,
                        eTag: "members-page-1",
                        projectionVersion: "projection-v1",
                        ReadModelFreshnessState.Current,
                        ProjectionLifecycleState.Current),
                });
            });
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-member-next']");

        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("page-1-user");
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldNotContain("embedded-should-not-render");
        cut.Find("[data-testid='tenants-member-next']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("page-2-user"));
        cut.Find("[data-testid='tenants-member-previous']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("page-1-user"));

        memberRequests.Select(static request => request.Cursor).ShouldBe([null, "cursor-page-2", null]);
        memberRequests[1].ETag.ShouldBeNull();
        memberRequests[2].ETag.ShouldBeNull();
    }

    [Fact]
    public void Workspace_detail_link_preserves_non_cursor_context_in_return_url()
    {
        TenantListSnapshot snapshot = TenantListSnapshot.Ready(
            [
                TenantListRow.FromSummary(new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active)),
            ],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current,
            isDegraded: false);
        RegisterServices(snapshot);

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/tenants?search=alpha&status=Active&sort=name&desc=True&cursor=cursor-1");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-detail-link']");

        string href = cut.Find("[data-testid='tenants-list-detail-link']").GetAttribute("href").ShouldNotBeNull();
        href.ShouldStartWith("/tenants/tenant.alpha?returnUrl=");
        string decoded = Uri.UnescapeDataString(href[(href.IndexOf("returnUrl=", StringComparison.Ordinal) + "returnUrl=".Length)..]);
        decoded.ShouldContain("search=alpha");
        decoded.ShouldContain("status=Active");
        decoded.ShouldContain("sort=name");
        decoded.ShouldContain("desc=True");
        decoded.ShouldNotContain("cursor=cursor-1");
        decoded.ShouldContain("selected=tenant.alpha");
        decoded.ShouldContain("anchor=tenant-row-tenant.alpha");
    }

    [Fact]
    public void Workspace_restores_return_context_from_query_before_loading_list()
    {
        List<TenantListRequest> requests = [];
        TenantListSnapshot snapshot = TenantListSnapshot.Ready(
            [
                TenantListRow.FromSummary(new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active)),
                TenantListRow.FromSummary(new TenantSummary("tenant.beta", "Beta", TenantStatus.Disabled)),
            ],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current,
            isDegraded: false);
        RegisterListServices(call =>
        {
            requests.Add(call.ArgAt<TenantListRequest>(0));
            return Task.FromResult(snapshot);
        });

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/tenants?search=beta&status=Disabled&sort=name&desc=True&cursor=cursor-1&selected=tenant.beta&anchor=tenant-row-tenant.beta");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-return-context']");

        TenantListRequest request = requests.ShouldHaveSingleItem();
        request.Search.ShouldBe("beta");
        request.Status.ShouldBe(TenantStatus.Disabled);
        request.SortColumn.ShouldBe(TenantListSortColumns.Name);
        request.SortDescending.ShouldBeTrue();
        request.Cursor.ShouldBeNull();
        cut.Find("[data-testid='tenants-list-return-context']").TextContent.ShouldContain("tenant.beta");
        cut.Markup.ShouldContain("tenant.beta");
        cut.Markup.ShouldNotContain("tenant.alpha");

        // The generated return anchor (anchor=tenant-row-tenant.beta) must resolve to a real DOM
        // target so focus/scroll restoration has something to land on.
        cut.Find("[id='tenant-row-tenant.beta']").ShouldNotBeNull();
        cut.Find("[id='tenants-list-heading']").GetAttribute("tabindex").ShouldBe("-1");
    }

    [Fact]
    public void Detail_styles_preserve_responsive_safety_and_forced_colors_hooks()
    {
        string projectRoot = ProjectRoot();
        string styles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Pages",
            "TenantDetailPage.razor.css"));

        styles.ShouldContain("overflow-wrap: anywhere");
        styles.ShouldContain("white-space: break-spaces");
        styles.ShouldContain("grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr))");
        styles.ShouldContain("@media (max-width: 767px)");
        styles.ShouldContain("grid-template-columns: minmax(0, 1fr)");
        styles.ShouldContain("@media (forced-colors: active)");
        styles.ShouldContain(":focus-visible");

        string configurationStyles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "TenantConfigurationView.razor.css"));

        configurationStyles.ShouldContain("overflow-wrap: anywhere");
        configurationStyles.ShouldContain("grid-template-columns");
        configurationStyles.ShouldContain("@media (max-width: 767px)");
        configurationStyles.ShouldContain("@media (forced-colors: active)");
        configurationStyles.ShouldContain(":focus-visible");

        string memberStyles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Members",
            "MemberAccessReview.razor.css"));

        memberStyles.ShouldContain("overflow-wrap: anywhere");
        memberStyles.ShouldContain("white-space: break-spaces");
        memberStyles.ShouldContain("grid-template-columns");
        memberStyles.ShouldContain("min-width");
        memberStyles.ShouldContain("@media (max-width: 767px)");
        memberStyles.ShouldContain("@media (forced-colors: active)");
        memberStyles.ShouldContain(":focus-visible");
        memberStyles.ShouldContain("grid-template-columns: minmax(0, 1fr) auto");

        string lifecycleStyles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Lifecycle",
            "TenantLifecycleActionAvailability.razor.css"));

        lifecycleStyles.ShouldContain("overflow-wrap: anywhere");
        lifecycleStyles.ShouldContain("grid-template-columns");
        lifecycleStyles.ShouldContain("@media (max-width: 767px)");
        lifecycleStyles.ShouldContain("@media (forced-colors: active)");
        lifecycleStyles.ShouldContain(":focus-visible");

        string changeRoleStyles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Members",
            "ChangeTenantMemberRoleFlow.razor.css"));

        changeRoleStyles.ShouldContain("overflow-wrap: anywhere");
        changeRoleStyles.ShouldContain("grid-template-columns");
        changeRoleStyles.ShouldContain("@media (max-width: 767px)");
        changeRoleStyles.ShouldContain("@media (forced-colors: active)");
        changeRoleStyles.ShouldContain(":focus-visible");

        string addMemberStyles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Members",
            "AddTenantMemberFlow.razor.css"));

        addMemberStyles.ShouldContain("overflow-wrap: anywhere");
        addMemberStyles.ShouldContain("grid-template-columns");
        addMemberStyles.ShouldContain("@media (max-width: 767px)");
        addMemberStyles.ShouldContain("@media (forced-colors: active)");
        addMemberStyles.ShouldContain(":focus-visible");

        string copyStyles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Shared",
            "SupportSafeCopyButton.razor.css"));

        copyStyles.ShouldContain("inline-size");
        copyStyles.ShouldContain(":focus-visible");
        copyStyles.ShouldContain("@media (forced-colors: active)");
        copyStyles.ShouldContain("min-inline-size");
        copyStyles.ShouldNotContain("animation:", Case.Insensitive);
        copyStyles.ShouldNotContain("transition:", Case.Insensitive);
    }

    [Fact]
    public void French_resources_include_detail_and_configuration_keys()
    {
        string projectRoot = ProjectRoot();
        string invariantResources = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.resx"));
        string frenchResources = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.fr.resx"));

        frenchResources.ShouldContain("Tenants.Detail.Title");
        frenchResources.ShouldContain("Tenants.Detail.State.Unauthorized.Message");
        frenchResources.ShouldContain("Tenants.Detail.Configuration.Summary");
        invariantResources.ShouldContain("Tenants.Configuration.Title");
        invariantResources.ShouldContain("Tenants.Configuration.State.Unavailable");
        invariantResources.ShouldContain("Tenants.Configuration.State.Unavailable");
        invariantResources.ShouldContain("Tenants.Configuration.Management.Unavailable.NoScope");
        frenchResources.ShouldContain("Tenants.Configuration.Title");
        frenchResources.ShouldContain("Tenants.Configuration.State.Unavailable");
        frenchResources.ShouldContain("Tenants.Configuration.State.Unavailable");
        frenchResources.ShouldContain("Tenants.Configuration.Management.Unavailable.NoScope");
        invariantResources.ShouldContain("Tenants.Configuration.Set.Title");
        invariantResources.ShouldContain("Tenants.Configuration.Set.State.ProjectionPending");
        frenchResources.ShouldContain("Tenants.Configuration.Set.Title");
        frenchResources.ShouldContain("Tenants.Configuration.Set.State.ProjectionPending");
        invariantResources.ShouldContain("Tenants.Configuration.Remove.Title");
        invariantResources.ShouldContain("Tenants.Configuration.Remove.State.ProjectionPending");
        frenchResources.ShouldContain("Tenants.Configuration.Remove.Title");
        frenchResources.ShouldContain("Tenants.Configuration.Remove.State.ProjectionPending");
        invariantResources.ShouldContain("Tenants.Members.Title");
        invariantResources.ShouldContain("Tenants.Members.UnavailableReason.MissingPermission");
        frenchResources.ShouldContain("Tenants.Members.Title");
        frenchResources.ShouldContain("Tenants.Members.UnavailableReason.MissingPermission");
        invariantResources.ShouldContain("Tenants.Lifecycle.Title");
        invariantResources.ShouldContain("Tenants.Lifecycle.Unavailable.Governance");
        invariantResources.ShouldContain("Tenants.Lifecycle.Unavailable.AlreadyActive");
        frenchResources.ShouldContain("Tenants.Lifecycle.Title");
        frenchResources.ShouldContain("Tenants.Lifecycle.Unavailable.Governance");
        frenchResources.ShouldContain("Tenants.Lifecycle.Unavailable.AlreadyActive");
        invariantResources.ShouldContain("Tenants.AddMember.Title");
        invariantResources.ShouldContain("Tenants.AddMember.State.ProjectionPending");
        frenchResources.ShouldContain("Tenants.AddMember.Title");
        frenchResources.ShouldContain("Tenants.AddMember.State.ProjectionPending");
        invariantResources.ShouldContain("Tenants.ChangeRole.Title");
        invariantResources.ShouldContain("Tenants.ChangeRole.State.AlreadyApplied");
        frenchResources.ShouldContain("Tenants.ChangeRole.Title");
        frenchResources.ShouldContain("Tenants.ChangeRole.State.AlreadyApplied");
        invariantResources.ShouldContain("Tenants.RemoveMember.Title");
        invariantResources.ShouldContain("Tenants.RemoveMember.State.ProjectionPending");
        frenchResources.ShouldContain("Tenants.RemoveMember.Title");
        frenchResources.ShouldContain("Tenants.RemoveMember.State.ProjectionPending");
        invariantResources.ShouldContain("Tenants.Copy.Action");
        invariantResources.ShouldContain("Tenants.Copy.Feedback.Unsafe");
        frenchResources.ShouldContain("Tenants.Copy.Action");
        frenchResources.ShouldContain("Tenants.Copy.Feedback.Unsafe");
    }

    [Fact]
    public void Configuration_resources_have_full_invariant_and_french_parity()
    {
        string projectRoot = ProjectRoot();
        HashSet<string> invariantKeys = ConfigurationResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.resx"));
        HashSet<string> frenchKeys = ConfigurationResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.fr.resx"));

        invariantKeys.ShouldNotBeEmpty();
        frenchKeys.ShouldBe(invariantKeys, ignoreOrder: true);
    }

    [Fact]
    public void Member_resources_have_full_invariant_and_french_parity()
    {
        string projectRoot = ProjectRoot();
        HashSet<string> invariantKeys = MemberResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.resx"));
        HashSet<string> frenchKeys = MemberResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.fr.resx"));

        invariantKeys.ShouldNotBeEmpty();
        frenchKeys.ShouldBe(invariantKeys, ignoreOrder: true);
    }

    [Fact]
    public void Lifecycle_resources_have_full_invariant_and_french_parity()
    {
        string projectRoot = ProjectRoot();
        HashSet<string> invariantKeys = LifecycleResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.resx"));
        HashSet<string> frenchKeys = LifecycleResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.fr.resx"));

        invariantKeys.ShouldNotBeEmpty();
        frenchKeys.ShouldBe(invariantKeys, ignoreOrder: true);
    }

    [Fact]
    public void Copy_resources_have_full_invariant_and_french_parity()
    {
        string projectRoot = ProjectRoot();
        HashSet<string> invariantKeys = CopyResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.resx"));
        HashSet<string> frenchKeys = CopyResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.fr.resx"));

        invariantKeys.ShouldNotBeEmpty();
        frenchKeys.ShouldBe(invariantKeys, ignoreOrder: true);
    }

    [Fact]
    public void Add_member_resources_have_full_invariant_and_french_parity()
    {
        string projectRoot = ProjectRoot();
        HashSet<string> invariantKeys = AddMemberResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.resx"));
        HashSet<string> frenchKeys = AddMemberResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.fr.resx"));

        invariantKeys.ShouldNotBeEmpty();
        frenchKeys.ShouldBe(invariantKeys, ignoreOrder: true);
    }

    [Fact]
    public void Change_role_resources_have_full_invariant_and_french_parity()
    {
        string projectRoot = ProjectRoot();
        HashSet<string> invariantKeys = ChangeRoleResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.resx"));
        HashSet<string> frenchKeys = ChangeRoleResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.fr.resx"));

        invariantKeys.ShouldNotBeEmpty();
        frenchKeys.ShouldBe(invariantKeys, ignoreOrder: true);
    }

    [Fact]
    public void Remove_member_resources_have_full_invariant_and_french_parity()
    {
        string projectRoot = ProjectRoot();
        HashSet<string> invariantKeys = RemoveMemberResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.resx"));
        HashSet<string> frenchKeys = RemoveMemberResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.fr.resx"));

        invariantKeys.ShouldNotBeEmpty();
        frenchKeys.ShouldBe(invariantKeys, ignoreOrder: true);
    }

    [Fact]
    public void Copy_source_uses_clipboard_module_without_browser_backend_or_legacy_fallbacks()
    {
        string projectRoot = ProjectRoot();
        string component = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Shared",
            "SupportSafeCopyButton.razor"));
        string script = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "wwwroot",
            "js",
            "tenantsClipboard.js"));

        component.ShouldContain("IJSRuntime");
        component.ShouldContain("JSDisconnectedException");
        component.ShouldNotContain("HttpClient");
        component.ShouldNotContain("ILogger");
        component.ShouldNotContain("Console.");
        component.ShouldNotContain("JsonSerializer");
        component.ShouldNotContain("localStorage", Case.Insensitive);
        component.ShouldNotContain("sessionStorage", Case.Insensitive);
        script.ShouldContain("navigator.clipboard.writeText");
        script.ShouldNotContain("document.execCommand", Case.Insensitive);
        script.ShouldNotContain("GET /api/", Case.Insensitive);
        script.ShouldNotContain("fetch(", Case.Insensitive);
        script.ShouldNotContain("XMLHttpRequest", Case.Insensitive);
        script.ShouldNotContain("sendBeacon", Case.Insensitive);
        script.ShouldNotContain("console.", Case.Insensitive);
        script.ShouldNotContain("JSON.stringify", Case.Insensitive);
        script.ShouldNotContain("access_token", Case.Insensitive);
        script.ShouldNotContain("localStorage", Case.Insensitive);
        script.ShouldNotContain("sessionStorage", Case.Insensitive);
    }

    private static HashSet<string> ConfigurationResourceKeys(string resourcePath)
        => Regex.Matches(File.ReadAllText(resourcePath), "name=\"(Tenants\\.Configuration[^\"]+)\"")
            .Select(static match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> MemberResourceKeys(string resourcePath)
        => Regex.Matches(File.ReadAllText(resourcePath), "name=\"(Tenants\\.Members[^\"]+)\"")
            .Select(static match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> LifecycleResourceKeys(string resourcePath)
        => Regex.Matches(File.ReadAllText(resourcePath), "name=\"(Tenants\\.Lifecycle[^\"]+)\"")
            .Select(static match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> CopyResourceKeys(string resourcePath)
        => Regex.Matches(File.ReadAllText(resourcePath), "name=\"(Tenants\\.Copy[^\"]+)\"")
            .Select(static match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> AddMemberResourceKeys(string resourcePath)
        => Regex.Matches(File.ReadAllText(resourcePath), "name=\"(Tenants\\.AddMember[^\"]+)\"")
            .Select(static match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> ChangeRoleResourceKeys(string resourcePath)
        => Regex.Matches(File.ReadAllText(resourcePath), "name=\"(Tenants\\.ChangeRole[^\"]+)\"")
            .Select(static match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> RemoveMemberResourceKeys(string resourcePath)
        => Regex.Matches(File.ReadAllText(resourcePath), "name=\"(Tenants\\.RemoveMember[^\"]+)\"")
            .Select(static match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

    private void RegisterComponentServices()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();

        // The workspace only restores retained protected paging on an interactive render pass.
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private void RegisterServices(TenantListSnapshot snapshot)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(snapshot));
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();

        // The workspace only restores retained protected paging on an interactive render pass.
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private void RegisterListServices(Func<NSubstitute.Core.CallInfo, Task<TenantListSnapshot>> resultFactory)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(resultFactory);
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();

        // The workspace only restores retained protected paging on an interactive render pass.
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private void RegisterServices(Func<NSubstitute.Core.CallInfo, Task<TenantDetailSnapshot>> detailFactory)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        TenantDetail? observedDetail = null;
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                TenantDetailSnapshot snapshot = await detailFactory(call);
                observedDetail = snapshot.Detail;
                return snapshot;
            });
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                TenantUsersRequest request = call.Arg<TenantUsersRequest>()
                    ?? throw new InvalidOperationException("A tenant-users request is required.");
                return Task.FromResult(MemberSnapshot(observedDetail ?? Detail(request.TenantId)));
            });
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();
    }

    private static TenantDetailSnapshot Snapshot(TenantDetailSurfaceKind kind)
        => kind switch
        {
            TenantDetailSurfaceKind.Stale => TenantDetailSnapshot.Stale(Detail("tenant.alpha"), "\"etag\""),
            TenantDetailSurfaceKind.Degraded => TenantDetailSnapshot.Degraded(Detail("tenant.alpha"), "Tenant detail projection is degraded."),
            TenantDetailSurfaceKind.Unauthorized => TenantDetailSnapshot.Unauthorized("tenant.alpha"),
            TenantDetailSurfaceKind.NotFound => TenantDetailSnapshot.NotFound("tenant.alpha"),
            TenantDetailSurfaceKind.Unavailable => TenantDetailSnapshot.Unavailable("Tenant detail query gateway is unavailable."),
            TenantDetailSurfaceKind.Unknown => TenantDetailSnapshot.Unknown("Tenant detail projection returned no payload."),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

    private static string ProjectRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static TenantDetail Detail(string tenantId)
        => Detail(tenantId, new Dictionary<string, string>
        {
            ["billing.mode"] = "trial",
        });

    [Fact]
    public void Configuration_view_never_renders_error_metadata_correlation_ids_tokens_stack_traces_or_pii()
    {
        // Restores the support-safety regression guard deleted by the read/management split. Its ten
        // assertions were the only executable evidence for the story's "never expose correlations,
        // exceptions, stack traces, tokens or PII" clause on this surface, and they were removed in the
        // same change that added new exception-handling paths through the gateway and composer.
        RegisterComponentServices();

        IRenderedComponent<TenantConfigurationView> cut = Render<TenantConfigurationView>(parameters => parameters
            .Add(view => view.Model, TenantConfigurationSafeModel.Unavailable("tenant.alpha"))
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Unavailable)
            .Add(view => view.Freshness, ReadModelFreshnessState.Unknown));

        foreach (string forbidden in new[]
        {
            "correlation-123",
            "raw-cursor",
            "InvalidOperationException",
            "stack trace",
            "at Hexalith.",
            "eyJhbGciOiJIUzI1NiJ9",
            "Bearer ",
            "jane.doe@example.test",
            "EventStore metadata",
            "ProjectedAt",
        })
        {
            cut.Markup.ShouldNotContain(forbidden, Case.Insensitive);
        }
    }

    [Fact]
    public void Configuration_filter_matches_literally_rather_than_by_culture_collation()
    {
        // Every other comparison in the feature is ordinal. Culture-sensitive Contains applies ICU
        // collation, which normalizes: the decomposed filter below matched the composed key even though
        // the policy treats those as different keys, and a zero-width space matched every row because
        // ICU treats it as fully ignorable. Code points are spelled out so the intent cannot be lost to
        // editor normalization.
        const string composed = "billing.caf\u00E9";      // NFC: e-acute as one code point
        const string decomposed = "caf\u0065\u0301";      // NFD: e + combining acute
        const string zeroWidthSpace = "\u200B";

        RegisterComponentServices();
        TenantConfigurationSafeModel model = SafeConfiguration(("billing", composed, "trial"));

        IRenderedComponent<TenantConfigurationView> cut = Render<TenantConfigurationView>(parameters => parameters
            .Add(view => view.Model, model)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-read-filter']").Change(decomposed);
        cut.FindAll("[data-testid='tenants-config-read-group']").ShouldBeEmpty();

        cut.Find("[data-testid='tenants-config-read-filter']").Change(zeroWidthSpace);
        cut.FindAll("[data-testid='tenants-config-read-group']").ShouldBeEmpty();

        cut.Find("[data-testid='tenants-config-read-filter']").Change("caf\u00E9");
        cut.FindAll("[data-testid='tenants-config-read-group']").Count.ShouldBe(1);
    }

    private static TenantConfigurationSafeModel SafeConfiguration(
        params (string Namespace, string Key, string Value)[] rows)
        => TenantConfigurationSafeModel.Available(
            "tenant.alpha",
            rows.Select(static row => new TenantConfigurationSafeRow(row.Namespace, row.Key, row.Value)));

    private static TenantDetailSnapshot ReadyWithSafeConfiguration(
        TenantDetail detail,
        ProjectionLifecycleState lifecycle = ProjectionLifecycleState.Unknown,
        string? projectionVersion = null)
    {
        TenantConfigurationSafeRow[] rows = detail.Configuration
            .Select(static item => new TenantConfigurationSafeRow(NamespaceFrom(item.Key), item.Key, item.Value))
            .ToArray();
        TenantConfigurationComposition composition = new(
            TenantConfigurationSafeComposer.SanitizeDetail(detail),
            TenantConfigurationSafeModel.Available(detail.TenantId, rows),
            TenantConfigurationManagementContext.Available(
                detail.TenantId,
                detail.Status,
                isGlobalAdministrator: false,
                rows.Select(static row => row.Namespace).Distinct(StringComparer.Ordinal),
                rows));
        return TenantDetailSnapshot.Ready(
            composition,
            "\"etag\"",
            ReadModelFreshnessState.Current,
            lifecycle,
            projectionVersion);
    }

    private static TenantUsersSnapshot MemberSnapshot(TenantDetail detail)
        => detail.Members.Count == 0
            ? TenantUsersSnapshot.Empty(
                detail.TenantId,
                isAuthorizationScoped: true,
                eTag: "members-etag",
                projectionVersion: "v1",
                ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current)
            : TenantUsersSnapshot.Ready(
                detail.TenantId,
                detail.Members,
                nextCursor: null,
                hasMore: false,
                eTag: "members-etag",
                projectionVersion: "v1",
                ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current);

    private static string NamespaceFrom(string key)
    {
        int separator = key.IndexOf('.', StringComparison.Ordinal);
        return separator > 0 ? key[..separator] : key;
    }

    private static TenantDetail Detail(string tenantId, IReadOnlyDictionary<string, string> configuration)
        => Detail(
            tenantId,
            configuration,
            TenantStatus.Active,
            [
                new TenantMember("owner-user", TenantRole.TenantOwner),
                new TenantMember("reader-user", TenantRole.TenantReader),
            ]);

    private static TenantDetail Detail(
        string tenantId,
        IReadOnlyDictionary<string, string> configuration,
        TenantStatus status,
        IReadOnlyList<TenantMember> members)
        => new(
            tenantId,
            "Alpha",
            "Tenant alpha description",
            status,
            members,
            configuration,
            DateTimeOffset.Parse("2026-06-01T12:00:00Z", CultureInfo.InvariantCulture));

    private sealed class StubTenantCommandGateway : ITenantCommandGateway
    {
        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRole request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(SetTenantConfiguration request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> RemoveTenantConfigurationAsync(RemoveTenantConfiguration request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandStatusResult> GetStatusAsync(TenantCommandTrackingHandle handle, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandStatusResult.Unknown("Tenant command status is unavailable."));
    }

    private sealed class StubTenantsBffComposition : ITenantsBffComposition
    {
        public bool IsReadSurfaceConnected => true;

        public bool IsCommandSurfaceConnected => true;
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        public LocalizedString this[string name] => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values.TryGetValue(name, out string? value) ? value : name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(v => new LocalizedString(v.Key, v.Value));

        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.Detail.Back"] = "Back to tenants",
            ["Tenants.Detail.Configuration.Empty"] = "No visible configuration is available in this detail projection.",
            ["Tenants.Detail.Configuration.Summary"] = "{0} visible configuration keys across {1} namespaces.",
            ["Tenants.Detail.Configuration.Title"] = "Configuration summary",
            ["Tenants.Detail.CreatedAtLabel"] = "Created",
            ["Tenants.Detail.FreshnessLabel"] = "Freshness",
            ["Tenants.Detail.FullTenantIdLabel"] = "Full tenant identifier {0}",
            ["Tenants.Detail.IdentityLabel"] = "Tenant identity",
            ["Tenants.Detail.LifecycleLabel"] = "Lifecycle",
            ["Tenants.Detail.Members.Summary"] = "{0} members, including {1} owners.",
            ["Tenants.Detail.Members.VisiblePageSummary"] = "{0} members visible on this page. Owner context is unavailable until both reads are current and version-consistent.",
            ["Tenants.Detail.Members.VisiblePageSummaryWithOwners"] = "{0} members visible on this page; authoritative tenant detail reports {1} owners.",
            ["Tenants.Detail.Members.Title"] = "Member summary",
            ["Tenants.Detail.OverviewLabel"] = "Tenant overview",
            ["Tenants.Detail.State.Degraded.Message"] = "Some tenant detail evidence is degraded. Treat the overview as incomplete until the server-side projection recovers.",
            ["Tenants.Detail.State.Degraded.Title"] = "Tenant detail is degraded",
            ["Tenants.Detail.State.Loading.Message"] = "Tenant detail is loading through the server-side query gateway.",
            ["Tenants.Detail.State.Loading.Title"] = "Loading tenant detail",
            ["Tenants.Detail.State.NotFound.Message"] = "The requested tenant was not found or is no longer visible to this operator.",
            ["Tenants.Detail.State.NotFound.Title"] = "Tenant not found",
            ["Tenants.Detail.State.Stale.Message"] = "The latest freshness evidence says this tenant detail is stale. Do not treat it as current.",
            ["Tenants.Detail.State.Stale.Title"] = "Tenant detail is stale",
            ["Tenants.Detail.State.Unauthorized.Message"] = "This operator is not authorized to view the requested tenant detail.",
            ["Tenants.Detail.State.Unauthorized.Title"] = "Tenant detail unauthorized",
            ["Tenants.Detail.State.Unavailable.Message"] = "Tenant detail cannot be loaded because the server-side query gateway is unavailable.",
            ["Tenants.Detail.State.Unavailable.Title"] = "Tenant detail unavailable",
            ["Tenants.Detail.Status.Active"] = "Active",
            ["Tenants.Detail.Status.Disabled"] = "Disabled",
            ["Tenants.Detail.Status.Unknown"] = "Unknown",
            ["Tenants.Detail.StatusAccessibleLabel"] = "Tenant status {0}",
            ["Tenants.Detail.StatusLabel"] = "Status",
            ["Tenants.Detail.Title"] = "Tenant detail",
            ["Tenants.Lifecycle.Unavailable.AlreadyActive"] = "{1} is unavailable for tenant {0} because the current projection already shows Active. If submitted by another surface, the safe domain outcome is {2}; continue read-only or refresh.",
            ["Tenants.Configuration.Announcement.Results"] = "{0} visible configuration entries across {1} namespace groups.",
            ["Tenants.Configuration.ClearFilter"] = "Clear",
            ["Tenants.Configuration.Description"] = "Read-only visible configuration from the authorized tenant detail projection.",
            ["Tenants.Configuration.Filter.Help"] = "Scan visible namespaces and literal keys. Prefix ownership is not inferred from this read model.",
            ["Tenants.Configuration.Filter.Label"] = "Filter visible configuration",
            ["Tenants.Configuration.Filter.Placeholder"] = "Namespace or key",
            ["Tenants.Configuration.GroupLabel"] = "Namespace {0}",
            ["Tenants.Configuration.Header.Freshness"] = "Freshness",
            ["Tenants.Configuration.Header.Key"] = "Key",
            ["Tenants.Configuration.Header.Namespace"] = "Namespace",
            ["Tenants.Configuration.Header.Value"] = "Value",
            ["Tenants.Configuration.Header.Actions"] = "Actions",
            ["Tenants.Configuration.KeyAccessible"] = "Full configuration key {0}",
            ["Tenants.Configuration.ScopeNotice"] = "Only configuration approved by the current namespace and display policy is shown.",
            ["Tenants.Configuration.State.Loading"] = "Configuration evidence is loading.",
            ["Tenants.Configuration.State.Loading.Title"] = "Configuration loading",
            ["Tenants.Configuration.State.Ready"] = "Configuration evidence is current.",
            ["Tenants.Configuration.State.Ready.Title"] = "Configuration current",
            ["Tenants.Configuration.State.Unknown"] = "Configuration evidence cannot be verified.",
            ["Tenants.Configuration.State.Unknown.Title"] = "Configuration evidence unknown",
            ["Tenants.Configuration.State.Degraded"] = "Configuration evidence is degraded.",
            ["Tenants.Configuration.State.Empty"] = "No visible configuration is available in this tenant detail projection.",
            ["Tenants.Configuration.State.Empty.Title"] = "No visible configuration",
            ["Tenants.Configuration.State.FilteredEmpty"] = "No visible configuration matches the current namespace filter.",
            ["Tenants.Configuration.State.FilteredEmpty.Title"] = "No visible configuration matches filters",
            ["Tenants.Configuration.State.Stale"] = "Configuration evidence is stale.",
            ["Tenants.Configuration.Table.Caption"] = "Visible tenant configuration grouped by namespace",
            ["Tenants.Configuration.Table.AccessibleLabel"] = "Authorized tenant configuration values",
            ["Tenants.Configuration.Title"] = "Visible configuration",
            ["Tenants.Configuration.ValueAccessible"] = "Visible configuration value {0}",
            ["Tenants.Configuration.Management.Title"] = "Configuration management",
            ["Tenants.Configuration.Management.Description"] = "Set configuration within current authorized prefixes or remove a current safe target.",
            ["Tenants.Configuration.Management.Unavailable.Policy"] = "Configuration management is unavailable because current authorization policy cannot be verified.",
            ["Tenants.Configuration.Management.Unavailable.NoScope"] = "Configuration management is unavailable because no configuration namespace is granted to you for this tenant.",
            ["Tenants.Configuration.Management.Unavailable.ProjectionState"] = "Refresh available tenant detail before managing configuration.",
            ["Tenants.Configuration.Management.Unavailable.Freshness"] = "Refresh current tenant detail before managing configuration.",
            ["Tenants.Configuration.Management.Unavailable.TenantLifecycle"] = "This tenant lifecycle state does not allow configuration management.",
            ["Tenants.Configuration.Management.Unavailable.CommandSurface"] = "Tenant command support is unavailable.",
            ["Tenants.Configuration.Management.Empty"] = "No current safe configuration targets are available for removal. Setting within an authorized prefix remains available.",
            ["Tenants.Configuration.Management.TargetsAccessibleLabel"] = "Configuration keys available for removal",
            ["Tenants.Configuration.Remove.Open"] = "Remove",
            ["Tenants.Configuration.Remove.OpenAccessible"] = "Remove configuration key {0}",
            ["Tenants.Configuration.Remove.Title"] = "Remove configuration",
            ["Tenants.Configuration.Remove.Description"] = "Prepare a scoped configuration removal for tenant {0} with projection confirmation.",
            ["Tenants.Configuration.Remove.Submit"] = "Confirm removal",
            ["Tenants.Configuration.Remove.Refresh"] = "Refresh status",
            ["Tenants.Configuration.Remove.Cancel"] = "Cancel",
            ["Tenants.Configuration.Remove.Confirmation.Label"] = "Type the full configuration key to confirm removal",
            ["Tenants.Configuration.Remove.Confirmation.Help"] = "Type {0} exactly. Cancel or Escape closes without submitting.",
            ["Tenants.Configuration.Remove.Lifecycle.Title"] = "Configuration removal lifecycle",
            ["Tenants.Configuration.Remove.Unavailable.Authorization"] = "You are not authorized to remove configuration for this tenant.",
            ["Tenants.Configuration.Remove.Unavailable.ProjectionState"] = "Tenant detail is unavailable or degraded. Refresh current tenant detail before removing configuration.",
            ["Tenants.Configuration.Remove.Unavailable.Freshness"] = "Refresh current tenant detail before removing configuration.",
            ["Tenants.Configuration.Remove.Unavailable.TenantLifecycle"] = "This tenant lifecycle state does not allow configuration removal.",
            ["Tenants.Configuration.Remove.Unavailable.CommandSurface"] = "Tenant command support is unavailable.",
            ["Tenants.Configuration.Remove.Unavailable.InFlight"] = "A tenant command is already in progress.",
            ["Tenants.Configuration.Remove.Unavailable.Identity"] = "Tenant identity is unavailable, so configuration removal fails closed.",
            ["Tenants.Configuration.Remove.Unavailable.Scope"] = "No authorized namespace prefix evidence is available from the current projection.",
            ["Tenants.Configuration.Remove.Unavailable.Target"] = "The selected configuration key is not visible in the current authorized projection. Refresh tenant detail before trying again.",
            ["Tenants.Configuration.Remove.Unavailable.Narrow"] = "Configuration removal is unavailable on narrow layouts because preview, tenant identity, freshness, target key, and confirmed configuration context must remain visible together.",
            ["Tenants.Configuration.Remove.Validation.KeyRequired"] = "Select a visible configuration key before previewing removal.",
            ["Tenants.Configuration.Remove.Validation.KeyVisible"] = "The selected key cannot be proven from the current authorized projection.",
            ["Tenants.Configuration.Remove.Validation.NamespaceScope"] = "The namespace prefix cannot be proven from the current authorized projection.",
            ["Tenants.Configuration.Remove.Validation.ConfirmationRequired"] = "Type {0} exactly before removing this configuration key.",
            ["Tenants.Configuration.Remove.Preview.Title"] = "Consequence preview",
            ["Tenants.Configuration.Remove.Preview.Blocked.Required"] = "Complete tenant identity, namespace, key, current state, freshness, authorization, and scope evidence before submitting.",
            ["Tenants.Configuration.Remove.Preview.Tenant"] = "Tenant",
            ["Tenants.Configuration.Remove.Preview.Namespace"] = "Namespace",
            ["Tenants.Configuration.Remove.Preview.Key"] = "Full key",
            ["Tenants.Configuration.Remove.Preview.CurrentState"] = "Current known state",
            ["Tenants.Configuration.Remove.Preview.IntendedEffect"] = "Intended effect",
            ["Tenants.Configuration.Remove.Preview.IntendedEffect.Value"] = "The selected configuration key will be removed only after command acceptance and projection proof.",
            ["Tenants.Configuration.Remove.Preview.Freshness"] = "Freshness evidence",
            ["Tenants.Configuration.Remove.Preview.Authorization"] = "Authorization and scope evidence",
            ["Tenants.Configuration.Remove.Preview.Authorization.Value"] = "The namespace prefix and key are visible in the authorized tenant projection; backend authorization still enforces the command.",
            ["Tenants.Configuration.Remove.Preview.KnownConsequences"] = "Known consequences",
            ["Tenants.Configuration.Remove.Preview.KnownConsequences.Value"] = "Consumers that own this prefix may lose the configured value after projection catches up.",
            ["Tenants.Configuration.Remove.Preview.KnownUnknowns"] = "Known unknowns",
            ["Tenants.Configuration.Remove.Preview.KnownUnknowns.Value"] = "This UI cannot prove downstream consumer impact or audit receipt availability.",
            ["Tenants.Configuration.Remove.Preview.AuditExpectation"] = "Audit expectation",
            ["Tenants.Configuration.Remove.Preview.AuditExpectation.Value"] = "Audit evidence is pending until the Epic 5 evidence source exists.",
            ["Tenants.Configuration.Remove.Preview.RecoveryPath"] = "Recovery path",
            ["Tenants.Configuration.Remove.Preview.RecoveryPath.Value"] = "Refresh tenant detail, retry only from current projection proof, or submit a forward correction to set the key again.",
            ["Tenants.Configuration.Remove.Freshness.Current"] = "Current",
            ["Tenants.Configuration.Remove.Freshness.Aging"] = "Aging",
            ["Tenants.Configuration.Remove.Freshness.Refreshing"] = "Refreshing",
            ["Tenants.Configuration.Remove.Freshness.Stale"] = "Stale",
            ["Tenants.Configuration.Remove.Freshness.Unknown"] = "Unknown",
            ["Tenants.Configuration.Remove.DuplicatePrevented.Message"] = "A configuration removal command is already in progress.",
            ["Tenants.Configuration.Remove.State.Idle"] = "No configuration removal command submitted.",
            ["Tenants.Configuration.Remove.State.Previewed"] = "Configuration removal preview ready.",
            ["Tenants.Configuration.Remove.State.RequestSent"] = "Configuration removal request sent.",
            ["Tenants.Configuration.Remove.State.Accepted"] = "Accepted by EventStore; waiting for configuration removal processing.",
            ["Tenants.Configuration.Remove.State.ProjectionPending"] = "Projection pending; the key is not confirmed removed yet.",
            ["Tenants.Configuration.Remove.State.Confirmed"] = "Projection confirmed the selected configuration key is removed.",
            ["Tenants.Configuration.Remove.State.Rejected"] = "Configuration removal command rejected.",
            ["Tenants.Configuration.Remove.State.AlreadyApplied"] = "Already applied.",
            ["Tenants.Configuration.Remove.State.DuplicatePrevented"] = "Duplicate configuration removal prevented.",
            ["Tenants.Configuration.Remove.State.Failed"] = "Configuration removal submission failed.",
            ["Tenants.Configuration.Remove.State.Degraded"] = "Configuration removal result is degraded and needs review.",
            ["Tenants.Configuration.Remove.State.UnableToVerify"] = "Unable to verify the configuration removal result.",
            ["Tenants.Configuration.Remove.Audit.NotStarted"] = "Audit evidence not started.",
            ["Tenants.Configuration.Remove.Audit.AuditPending"] = "Audit evidence pending.",
            ["Tenants.Configuration.Remove.Audit.AuditDelayed"] = "Audit evidence delayed.",
            ["Tenants.Configuration.Remove.Audit.AuditUnavailable"] = "Audit evidence unavailable.",
            ["Tenants.Configuration.Remove.Audit.MissingSupport"] = "Audit evidence support is missing until Epic 5 implements the evidence source.",
            ["Tenants.Configuration.Remove.Recovery.Idle"] = "Choose a visible key when current projection evidence and namespace scope are available.",
            ["Tenants.Configuration.Remove.Recovery.Previewed"] = "Confirm removal, cancel, or continue read-only.",
            ["Tenants.Configuration.Remove.Recovery.RequestSent"] = "Wait for command status and projection refresh.",
            ["Tenants.Configuration.Remove.Recovery.Accepted"] = "Wait, refresh status, or continue read-only until projection confirms removal.",
            ["Tenants.Configuration.Remove.Recovery.ProjectionPending"] = "Refresh tenant detail; do not display removal as complete until the key is absent from projection.",
            ["Tenants.Configuration.Remove.Recovery.Confirmed"] = "Continue read-only or inspect audit when evidence becomes available.",
            ["Tenants.Configuration.Remove.Recovery.Rejected"] = "Refresh projection evidence, request permission, start correction, or escalate.",
            ["Tenants.Configuration.Remove.Recovery.AlreadyApplied"] = "Refresh projection evidence before treating the missing key as already removed.",
            ["Tenants.Configuration.Remove.Recovery.DuplicatePrevented"] = "Wait for the in-flight command, retry status lookup, or continue read-only.",
            ["Tenants.Configuration.Remove.Recovery.Failed"] = "Retry after checking current projection evidence or escalate.",
            ["Tenants.Configuration.Remove.Recovery.Degraded"] = "Wait, retry status lookup, inspect audit when available, or escalate.",
            ["Tenants.Configuration.Remove.Recovery.UnableToVerify"] = "Refresh, retry status lookup, continue read-only, or escalate.",
            ["Tenants.Copy.Action"] = "Copy",
            ["Tenants.Copy.Label.ConfigurationKey"] = "Copy configuration key {0}",
            ["Tenants.Copy.Label.ConfigurationValue"] = "Copy visible configuration value for {0}",
            ["Tenants.Copy.Label.TenantId"] = "Copy tenant identifier {0}",
            ["Tenants.Copy.Label.UserId"] = "Copy user identifier {0}",
            ["Tenants.Copy.Feedback.Copied"] = "Copied.",
            ["Tenants.Copy.Feedback.Disconnected"] = "Clipboard disconnected. Copy was not completed. Select the value and copy it manually.",
            ["Tenants.Copy.Feedback.Empty"] = "Nothing is available to copy.",
            ["Tenants.Copy.Feedback.Failed"] = "Copy failed. Select the value and copy it manually.",
            ["Tenants.Copy.Feedback.Unavailable"] = "Clipboard unavailable. Select the value and copy it manually.",
            ["Tenants.Copy.Feedback.Unsafe"] = "This value is not support-safe to copy.",
            ["Tenants.AddMember.Title"] = "Add tenant member",
            ["Tenants.AddMember.Description"] = "Add a literal user id to tenant {0}. Current visible owner count is {1}.",
            ["Tenants.AddMember.UserId.Label"] = "User id",
            ["Tenants.AddMember.UserId.Help"] = "Use the exact caller-supplied user id.",
            ["Tenants.AddMember.Role.Label"] = "Tenant role",
            ["Tenants.AddMember.Role.Placeholder"] = "Select a role",
            ["Tenants.AddMember.Role.TenantOwner"] = "Tenant owner",
            ["Tenants.AddMember.Role.TenantContributor"] = "Tenant contributor",
            ["Tenants.AddMember.Role.TenantReader"] = "Tenant reader",
            ["Tenants.AddMember.Submit"] = "Add member",
            ["Tenants.AddMember.Refresh"] = "Refresh status",
            ["Tenants.AddMember.Lifecycle.Title"] = "Add member command lifecycle",
            ["Tenants.AddMember.Validation.UserIdRequired"] = "User id is required.",
            ["Tenants.AddMember.Validation.RoleRequired"] = "Select TenantOwner, TenantContributor, or TenantReader before adding a member.",
            ["Tenants.AddMember.Unavailable.Authorization"] = "You are not authorized to add members to this tenant.",
            ["Tenants.AddMember.Unavailable.Freshness"] = "Refresh current tenant detail before adding a member.",
            ["Tenants.AddMember.Unavailable.TenantLifecycle"] = "This tenant lifecycle state does not allow adding members.",
            ["Tenants.AddMember.Unavailable.CommandSurface"] = "Tenant command support is unavailable.",
            ["Tenants.AddMember.Unavailable.InFlight"] = "A tenant command is already in progress.",
            ["Tenants.AddMember.State.Idle"] = "No add-member command submitted.",
            ["Tenants.AddMember.State.RequestSent"] = "Add-member request sent.",
            ["Tenants.AddMember.State.Accepted"] = "Accepted by EventStore; waiting for member processing.",
            ["Tenants.AddMember.State.ProjectionPending"] = "Projection pending; the member role is not confirmed visible yet.",
            ["Tenants.AddMember.State.Confirmed"] = "Projection confirmed the user is a tenant member with the requested role.",
            ["Tenants.AddMember.State.Rejected"] = "Add-member command rejected.",
            ["Tenants.AddMember.State.Failed"] = "Add-member command submission failed.",
            ["Tenants.AddMember.State.Degraded"] = "Add-member command result is degraded and needs review.",
            ["Tenants.AddMember.State.UnableToVerify"] = "Unable to verify the add-member command result.",
            ["Tenants.AddMember.Audit.NotStarted"] = "Audit evidence not started.",
            ["Tenants.AddMember.Audit.AuditPending"] = "Audit evidence pending.",
            ["Tenants.AddMember.Audit.AuditUnavailable"] = "Audit evidence unavailable.",
            ["Tenants.AddMember.Audit.MissingSupport"] = "Audit support is missing for this flow.",
            ["Tenants.ChangeRole.Title"] = "Change tenant member role",
            ["Tenants.ChangeRole.Description"] = "Change the role for user {1} in tenant {0}. The current confirmed role is {2}.",
            ["Tenants.ChangeRole.UserId.Label"] = "User id",
            ["Tenants.ChangeRole.CurrentRole.Label"] = "Current confirmed role",
            ["Tenants.ChangeRole.OwnerContext.Label"] = "Owner context",
            ["Tenants.ChangeRole.OwnerContext.NoOwners"] = "0 visible owners; owner context is unavailable.",
            ["Tenants.ChangeRole.OwnerContext.LastOwner"] = "{0} visible owner; changing this owner can leave the tenant with zero visible owners.",
            ["Tenants.ChangeRole.OwnerContext.MultipleOwners"] = "{0} visible owners.",
            ["Tenants.ChangeRole.NewRole.Label"] = "New role",
            ["Tenants.ChangeRole.NewRole.Help"] = "Select TenantOwner, TenantContributor, or TenantReader. Selecting the current role records an already applied state.",
            ["Tenants.ChangeRole.Role.TenantOwner"] = "Tenant owner",
            ["Tenants.ChangeRole.Role.TenantContributor"] = "Tenant contributor",
            ["Tenants.ChangeRole.Role.TenantReader"] = "Tenant reader",
            ["Tenants.ChangeRole.Submit"] = "Change role",
            ["Tenants.ChangeRole.Refresh"] = "Refresh status",
            ["Tenants.ChangeRole.Cancel"] = "Close",
            ["Tenants.ChangeRole.Lifecycle.Title"] = "Change role command lifecycle",
            ["Tenants.ChangeRole.Validation.RoleRequired"] = "Select TenantOwner, TenantContributor, or TenantReader before changing a role.",
            ["Tenants.ChangeRole.Unavailable.Authorization"] = "You are not authorized to change member roles in this tenant.",
            ["Tenants.ChangeRole.Unavailable.Freshness"] = "Refresh current tenant detail before changing a member role.",
            ["Tenants.ChangeRole.Unavailable.TenantLifecycle"] = "This tenant lifecycle state does not allow changing member roles.",
            ["Tenants.ChangeRole.Unavailable.CommandSurface"] = "Tenant command support is unavailable.",
            ["Tenants.ChangeRole.Unavailable.InFlight"] = "A tenant command is already in progress.",
            ["Tenants.ChangeRole.Unavailable.UnknownRole"] = "The current role is unknown, so role change fails closed until projection evidence is refreshed.",
            ["Tenants.ChangeRole.OwnerRisk.LastOwner"] = "Warning: {0} visible owner remains. This change can reduce the visible owner count to zero, but the command is not blocked solely for that reason.",
            ["Tenants.ChangeRole.AlreadyApplied.Message"] = "User {0} already has role {1}; no role-change command was submitted.",
            ["Tenants.ChangeRole.State.Idle"] = "No change-role command submitted.",
            ["Tenants.ChangeRole.State.RequestSent"] = "Change-role request sent.",
            ["Tenants.ChangeRole.State.Accepted"] = "Accepted by EventStore; waiting for member role processing.",
            ["Tenants.ChangeRole.State.ProjectionPending"] = "Projection pending; the requested role is not confirmed visible yet.",
            ["Tenants.ChangeRole.State.Confirmed"] = "Projection confirmed the target user has the requested role.",
            ["Tenants.ChangeRole.State.Rejected"] = "Change-role command rejected.",
            ["Tenants.ChangeRole.State.AlreadyApplied"] = "Already applied; the confirmed role already matches the selected role.",
            ["Tenants.ChangeRole.State.Failed"] = "Change-role command submission failed.",
            ["Tenants.ChangeRole.State.Degraded"] = "Change-role command result is degraded and needs review.",
            ["Tenants.ChangeRole.State.UnableToVerify"] = "Unable to verify the change-role command result.",
            ["Tenants.ChangeRole.Audit.NotStarted"] = "Audit evidence not started.",
            ["Tenants.ChangeRole.Audit.AuditPending"] = "Audit evidence pending.",
            ["Tenants.ChangeRole.Audit.AuditUnavailable"] = "Audit evidence unavailable.",
            ["Tenants.ChangeRole.Audit.MissingSupport"] = "Audit support is missing for this flow.",
            ["Tenants.Members.Action.AddMember"] = "Add member",
            ["Tenants.Members.Action.ChangeRole"] = "Change role",
            ["Tenants.Members.Action.RemoveMember"] = "Remove member",
            ["Tenants.Members.Action.Unavailable"] = "Unavailable",
            ["Tenants.Members.ActionSlotAccessible"] = "{0} is unavailable for {1}: {2}",
            ["Tenants.Members.Column.Actions"] = "Action availability",
            ["Tenants.Members.Column.Freshness"] = "Freshness",
            ["Tenants.Members.Column.OwnerContext"] = "Owner context",
            ["Tenants.Members.Column.Role"] = "Role",
            ["Tenants.Members.Column.Status"] = "Tenant status",
            ["Tenants.Members.Column.UserId"] = "User id",
            ["Tenants.Members.Description"] = "Read-only member access context from the dedicated authorized member projection.",
            ["Tenants.Members.Empty.Message"] = "No visible members are available. This state does not reveal hidden memberships, and actions remain unavailable until visibility and freshness are verified: {0}.",
            ["Tenants.Members.Empty.Title"] = "No visible members",
            ["Tenants.Members.Freshness.Aging"] = "Aging",
            ["Tenants.Members.Freshness.Current"] = "Current",
            ["Tenants.Members.Freshness.Refreshing"] = "Refreshing",
            ["Tenants.Members.Freshness.Stale"] = "Stale",
            ["Tenants.Members.Freshness.Unknown"] = "Unknown",
            ["Tenants.Members.OwnerContext.LastOwner"] = "{0} visible owner; last-owner changes require a later high-impact flow.",
            ["Tenants.Members.OwnerContext.MultipleOwners"] = "{0} visible owners.",
            ["Tenants.Members.OwnerContext.NoOwners"] = "0 visible owners; owner context is unavailable.",
            ["Tenants.Members.ReasonCatalogLabel"] = "Canonical unavailable action reason categories",
            ["Tenants.Members.ReasonListLabel"] = "Unavailable action reasons for {0}",
            ["Tenants.Members.Role.TenantContributor"] = "Tenant contributor",
            ["Tenants.Members.Role.TenantOwner"] = "Tenant owner",
            ["Tenants.Members.Role.TenantReader"] = "Tenant reader",
            ["Tenants.Members.Role.Unknown"] = "Unknown role",
            ["Tenants.Members.RoleAccessible"] = "Role: {0}",
            ["Tenants.Members.ScopeNotice"] = "Visible members only. Orphan context is unavailable in this read model; disabled lifecycle is shown from tenant status.",
            ["Tenants.Members.State.Degraded"] = "Member evidence is degraded.",
            ["Tenants.Members.State.Stale"] = "Member evidence is stale.",
            ["Tenants.Members.Status.Active"] = "Active",
            ["Tenants.Members.Status.Disabled"] = "Disabled",
            ["Tenants.Members.Status.Unknown"] = "Unknown",
            ["Tenants.Members.StatusAccessible"] = "Tenant status {0}",
            ["Tenants.Members.Table.Caption"] = "Visible tenant members and read-only action availability",
            ["Tenants.Members.Title"] = "Member access review",
            ["Tenants.Members.UnavailableReason.HighImpactFlowNotReady"] = "high-impact flow not ready",
            ["Tenants.Members.UnavailableReason.MissingAuditProof"] = "missing audit proof",
            ["Tenants.Members.UnavailableReason.MissingConsequencePreview"] = "missing consequence preview",
            ["Tenants.Members.UnavailableReason.MissingLifecycleSupport"] = "missing lifecycle support",
            ["Tenants.Members.UnavailableReason.MissingPermission"] = "missing permission",
            ["Tenants.Members.UnavailableReason.StaleData"] = "stale data",
            ["Tenants.Members.UserIdAccessible"] = "Literal member user identifier {0}",
            ["Tenants.List.Column.Freshness"] = "Truth state",
            ["Tenants.List.Column.Members"] = "Members",
            ["Tenants.List.Column.Owners"] = "Owners",
            ["Tenants.List.Column.Pending"] = "Pending",
            ["Tenants.List.Column.Status"] = "Status",
            ["Tenants.List.Column.Tenant"] = "Tenant",
            ["Tenants.List.DetailLinkLabel"] = "Open tenant details for {0}",
            ["Tenants.List.Freshness.Current"] = "Current",
            ["Tenants.List.Pending.None"] = "No pending changes",
            ["Tenants.List.Pending.Unknown"] = "Pending state unknown",
            ["Tenants.List.ReturnContext"] = "Returned from tenant {0}. Filters, sort, and selection were restored on the authorized first page.",
            ["Tenants.List.Title"] = "Tenants",
            ["Tenants.Workspace.Eyebrow"] = "Tenant workspace",
        };
    }
}
