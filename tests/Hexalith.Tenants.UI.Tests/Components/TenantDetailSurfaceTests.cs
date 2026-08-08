using System.Globalization;
using System.Collections;
using System.Reflection;
using System.Resources;
using System.Security.Claims;
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
using Hexalith.Tenants.UI.Components.Tenants.Metadata;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services;
using Hexalith.Tenants.UI.Services.Configuration;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.TenantUsers;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
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
        // Asserted past the shared prefix: both summary variants begin "{0} members visible on this page",
        // so the previous ShouldContain("2 members") passed on either branch. This fixture builds the detail
        // snapshot with the default Current lifecycle and no projection version, so the evidence is not
        // version-consistent and the owner-context variant is correctly withheld. Both branches are covered
        // by Member_governance_claims_require_current_lifecycle_and_a_stated_matching_projection_version.
        cut.Find("[data-testid='tenants-detail-member-summary']").TextContent
            .ShouldContain("2 members visible on this page. Owner context is unavailable");
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

    [Fact]
    public void Detail_lifecycle_actions_use_the_async_circuit_authorization_decision()
    {
        RegisterServices(
            _ => Task.FromResult(ReadyWithSafeConfiguration(Detail("tenant.alpha"))),
            new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='tenants-lifecycle-disable']")
                .GetAttribute("aria-disabled")
                .ShouldBe("false"));
    }

    [Fact]
    public void Detail_lifecycle_actions_reauthorize_on_circuit_authentication_transitions()
    {
        var authentication = new MutableAuthenticationStateProvider();
        var composition = new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized);
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        RegisterServices(
            _ => Task.FromResult(ReadyWithSafeConfiguration(Detail("tenant.alpha"))),
            composition);

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='tenants-lifecycle-disable']")
                .GetAttribute("aria-disabled")
                .ShouldBe("false"));

        // Even an administrator-shaped event is only a transition signal. The strict composition result
        // remains authoritative and revokes the action until it is corroborated again.
        composition.Reflection = TenantLifecycleAuthorizationReflectionState.MissingPermission;
        authentication.Notify(AdministratorPrincipal());
        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='tenants-lifecycle-disable']")
                .GetAttribute("aria-disabled")
                .ShouldBe("true"));

        composition.Reflection = TenantLifecycleAuthorizationReflectionState.Authorized;
        authentication.Notify(AdministratorPrincipal());
        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='tenants-lifecycle-disable']")
                .GetAttribute("aria-disabled")
                .ShouldBe("false"));
    }

    /// <summary>
    /// Lifecycle authorization sets Indeterminate before the async resolve completes. Without that fail-closed
    /// window, actions could render as available while evidence was still pending.
    /// </summary>
    [Fact]
    public async Task Detail_lifecycle_actions_fail_closed_while_authorization_is_pending()
    {
        var composition = new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        composition.ResolutionGate = gate;
        RegisterServices(
            _ => Task.FromResult(ReadyWithSafeConfiguration(Detail("tenant.alpha"))),
            composition);

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        await composition.ResolutionEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cut.Find("[data-testid='tenants-lifecycle-disable']")
            .GetAttribute("aria-disabled")
            .ShouldBe("true");

        gate.SetResult();
        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='tenants-lifecycle-disable']")
                .GetAttribute("aria-disabled")
                .ShouldBe("false"));
    }

    /// <summary>
    /// A superseded lifecycle authorization generation must not overwrite a newer transition result.
    /// </summary>
    [Fact]
    public async Task Detail_lifecycle_authorization_discards_a_superseded_resolution()
    {
        var authentication = new MutableAuthenticationStateProvider();
        var composition = new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized);
        var firstGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        composition.ResolutionGate = firstGate;
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        RegisterServices(
            _ => Task.FromResult(ReadyWithSafeConfiguration(Detail("tenant.alpha"))),
            composition);

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        await composition.ResolutionEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        composition.Reflection = TenantLifecycleAuthorizationReflectionState.MissingPermission;
        var secondGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        composition.ResolutionGate = secondGate;
        authentication.Notify(AdministratorPrincipal());
        await WaitUntilAsync(() => composition.AsyncResolutionCount >= 2, TimeSpan.FromSeconds(5));
        secondGate.SetResult();
        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='tenants-lifecycle-disable']")
                .GetAttribute("aria-disabled")
                .ShouldBe("true"));

        composition.Reflection = TenantLifecycleAuthorizationReflectionState.Authorized;
        firstGate.SetResult();
        await Task.Delay(250);

        cut.Find("[data-testid='tenants-lifecycle-disable']")
            .GetAttribute("aria-disabled")
            .ShouldBe("true");
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
    public async Task Route_change_rebinds_the_notification_lease_to_the_new_tenant()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        IProjectionSubscription backendSubscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        backendSubscription
            .SubscribeAsync("tenants", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        backendSubscription
            .UnsubscribeAsync("tenants", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                string tenantId = call.Arg<TenantDetailRequest>()!.TenantId;
                return Task.FromResult(ReadyWithSafeConfiguration(
                    Detail(tenantId),
                    ProjectionLifecycleState.Current,
                    "projection-v1"));
            });
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(MemberSnapshot(Detail(call.Arg<TenantUsersRequest>()!.TenantId))));
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
        cut.WaitForElement("[data-testid='tenants-detail-identity']");
        await backendSubscription.Received(1)
            .SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());

        cut.Render(parameters => parameters.Add(page => page.TenantId, "tenant.beta"));
        cut.WaitForAssertion(() => cut.Find(".tenant-detail__literal").TextContent.ShouldBe("tenant.beta"));

        await backendSubscription.Received(1)
            .UnsubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());
        await backendSubscription.Received(1)
            .SubscribeAsync("tenants", "tenant.beta", Arg.Any<CancellationToken>());
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

    [Fact]
    public async Task Faulted_member_refresh_clears_refreshing_and_retains_last_confirmed_rows_as_degraded()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        TenantDetailSnapshot detail = ReadyWithSafeConfiguration(
            Detail("tenant.alpha"),
            ProjectionLifecycleState.Current,
            "projection-v1");
        TenantUsersSnapshot members = TenantUsersSnapshot.Ready(
            "tenant.alpha",
            [new TenantMember("last-confirmed-user", TenantRole.TenantReader)],
            nextCursor: null,
            hasMore: false,
            eTag: "members-v1-etag",
            projectionVersion: "projection-v1",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current);
        int memberReads = 0;
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(detail));
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Interlocked.Increment(ref memberReads) == 1
                ? Task.FromResult(members)
                : Task.FromException<TenantUsersSnapshot>(new InvalidOperationException("transport detail must stay contained")));

        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-member-table']");

        await cut.InvokeAsync(() => cut.FindComponent<MemberAccessReview>().Instance
            .OnProjectionRefreshRequested.InvokeAsync());

        cut.WaitForAssertion(() =>
        {
            MemberAccessReview memberReview = cut.FindComponent<MemberAccessReview>().Instance;
            memberReview.Members.Kind.ShouldBe(TenantUsersSurfaceKind.Degraded);
            memberReview.Members.Reason.ShouldBe(TenantUsersReason.GatewayFailure);
            cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("last-confirmed-user");
            cut.FindAll("[data-testid='tenants-member-refreshing']").ShouldBeEmpty();
        });
    }

    [Fact]
    public void Confirmed_member_command_refreshes_parent_detail_and_member_projections()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        int detailReads = 0;
        int memberReads = 0;
        TenantDetail initialDetail = Detail("tenant.alpha");
        TenantDetail refreshedDetail = Detail(
            "tenant.alpha",
            new Dictionary<string, string>(),
            TenantStatus.Active,
            [
                .. initialDetail.Members,
                new TenantMember("new-member", TenantRole.TenantReader),
            ]);
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(ReadyWithSafeConfiguration(
                Interlocked.Increment(ref detailReads) == 1 ? initialDetail : refreshedDetail,
                ProjectionLifecycleState.Current,
                "projection-v1")));
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(MemberSnapshot(
                Interlocked.Increment(ref memberReads) == 1 ? initialDetail : refreshedDetail) with
            {
                ProjectionVersion = "projection-v1",
            }));
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway
        {
            AddMemberSubmission = TenantCommandSubmissionResult.Accepted(
                "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                "correlation-456"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed),
        });
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-add-member-user-id']").Change("new-member");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("[data-testid='tenants-add-member-flow'] form").Submit();

        cut.WaitForAssertion(() =>
        {
            detailReads.ShouldBe(2);
            memberReads.ShouldBe(2);
            cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("new-member");
        });
    }

    [Fact]
    public void Same_aggregate_membership_command_makes_sibling_membership_surfaces_unavailable()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var admissionGate = new TenantAggregateCommandAdmissionGate();
        TaskCompletionSource<TenantCommandSubmissionResult> pendingSubmission = new();
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(ReadyWithSafeConfiguration(
                Detail("tenant.alpha"),
                ProjectionLifecycleState.Current,
                "projection-v1")));
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(MemberSnapshot(Detail("tenant.alpha")) with
            {
                ProjectionVersion = "projection-v1",
            }));
        Services.AddSingleton(gateway);
        Services.AddSingleton(admissionGate);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new TrackingMembershipCommandGateway(pendingSubmission.Task));
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-add-member-user-id']").Change("new-member");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("[data-testid='tenants-add-member-flow'] form").Submit();

        cut.WaitForAssertion(() =>
            admissionGate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue());

        cut.Find("[data-testid='tenants-change-role-open']").Click();
        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='tenants-change-role-unavailable-reason']")
                .TextContent.ShouldContain("command support is unavailable", Case.Insensitive));

        pendingSubmission.SetResult(TenantCommandSubmissionResult.Failed("Command submission cancelled by the test."));
    }

    [Fact]
    public void Detail_read_refresh_nudges_in_flight_membership_flow_without_confirming_from_notification()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        int detailReads = 0;
        TenantDetail refreshedDetail = Detail(
            "tenant.alpha",
            new Dictionary<string, string> { ["billing.mode"] = "trial" },
            TenantStatus.Active,
            [
                new TenantMember("owner-user", TenantRole.TenantOwner),
                new TenantMember("reader-user", TenantRole.TenantReader),
                new TenantMember("new-member", TenantRole.TenantReader),
            ]);
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                int call = Interlocked.Increment(ref detailReads);
                return Task.FromResult(ReadyWithSafeConfiguration(
                    call == 1 ? Detail("tenant.alpha") : refreshedDetail,
                    ProjectionLifecycleState.Current,
                    call == 1 ? "projection-v1" : "projection-v2"));
            });
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(MemberSnapshot(detailReads <= 1 ? Detail("tenant.alpha") : refreshedDetail) with
            {
                ProjectionVersion = detailReads <= 1 ? "projection-v1" : "projection-v2",
            }));
        var commandGateway = new TrackingMembershipCommandGateway(
            Task.FromResult(TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-456")),
            new TenantCommandStatusResult(CommandStatus.Received));
        Services.AddSingleton(gateway);
        Services.AddSingleton(new TenantAggregateCommandAdmissionGate());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-add-member-user-id']").Change("new-member");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("[data-testid='tenants-add-member-flow'] form").Submit();

        cut.WaitForAssertion(() =>
            cut.FindComponent<AddTenantMemberFlow>().Instance.Snapshot.State
                .ShouldBe(TenantCommandLifecycleState.Accepted));

        cut.InvokeAsync(() => cut.FindComponent<MemberAccessReview>().Instance
            .OnProjectionRefreshRequested.InvokeAsync());

        cut.WaitForAssertion(() => detailReads.ShouldBeGreaterThan(1));
        cut.FindComponent<AddTenantMemberFlow>().Instance.Snapshot.State
            .ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        commandGateway.AddMemberCallCount.ShouldBe(1);
        commandGateway.StatusCallCount.ShouldBeGreaterThan(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Initial_detail_load_contains_each_fault_and_observes_the_sibling_read(bool faultDetailRead)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        int detailReads = 0;
        int memberReads = 0;
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref detailReads);
                return faultDetailRead
                    ? Task.FromException<TenantDetailSnapshot>(new InvalidOperationException("detail transport detail"))
                    : Task.FromResult(ReadyWithSafeConfiguration(
                        Detail("tenant.alpha"),
                        ProjectionLifecycleState.Current,
                        "projection-v1"));
            });
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref memberReads);
                return faultDetailRead
                    ? Task.FromResult(MemberSnapshot(Detail("tenant.alpha")))
                    : Task.FromException<TenantUsersSnapshot>(new InvalidOperationException("member transport detail"));
            });
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));

        cut.WaitForElement(faultDetailRead
            ? "[data-testid='tenants-detail-error']"
            : "[data-testid='tenants-member-unavailable']");
        detailReads.ShouldBe(1);
        memberReads.ShouldBe(1);
        if (!faultDetailRead)
        {
            cut.Find("[data-testid='tenants-detail-identity']");
        }
    }

    /// <summary>
    /// Combined refresh must observe each read independently on the refresh path, not only on initial load.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Combined_detail_refresh_contains_each_fault_and_observes_the_sibling_read(bool faultDetailRead)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
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
        int detailReads = 0;
        int memberReads = 0;
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                int call = Interlocked.Increment(ref detailReads);
                if (call == 1)
                {
                    return Task.FromResult(initialDetail);
                }

                return faultDetailRead
                    ? Task.FromException<TenantDetailSnapshot>(new InvalidOperationException("detail transport detail"))
                    : Task.FromResult(initialDetail);
            });
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                int call = Interlocked.Increment(ref memberReads);
                if (call == 1)
                {
                    return Task.FromResult(initialMembers);
                }

                return faultDetailRead
                    ? Task.FromResult(initialMembers)
                    : Task.FromException<TenantUsersSnapshot>(new InvalidOperationException("member transport detail"));
            });
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-detail-identity']");
        cut.WaitForElement("[data-testid='tenants-member-table']");
        detailReads.ShouldBe(1);
        memberReads.ShouldBe(1);

        await cut.InvokeAsync(() => cut.FindComponent<MemberAccessReview>().Instance
            .OnProjectionRefreshRequested.InvokeAsync());

        cut.WaitForAssertion(() =>
        {
            detailReads.ShouldBe(2);
            memberReads.ShouldBe(2);
            if (faultDetailRead)
            {
                cut.Find("[data-testid='tenants-detail-degraded']");
                cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("last-confirmed-user");
            }
            else
            {
                cut.Find("[data-testid='tenants-detail-identity']");
                MemberAccessReview memberReview = cut.FindComponent<MemberAccessReview>().Instance;
                memberReview.Members.Kind.ShouldBe(TenantUsersSurfaceKind.Degraded);
                memberReview.Members.Reason.ShouldBe(TenantUsersReason.GatewayFailure);
                cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("last-confirmed-user");
                cut.FindAll("[data-testid='tenants-member-refreshing']").ShouldBeEmpty();
            }
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

        // The denial must be announced, not merely rendered. The only assertion covering this lived in the
        // Tier 3 route-smoke class, which carries [DaprFact] + SkipIfUnavailable() and runs
        // continue-on-error -- so a green report there was never proof it ran. It is asserted here, in a
        // blocking lane, as an element attribute rather than as an ordered raw-markup substring.
        if (kind is TenantDetailSurfaceKind.Unauthorized)
        {
            cut.Find($"[data-testid='{selector}']").GetAttribute("role").ShouldBe("alert");
        }

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
    public void Detail_page_reauthorizes_configuration_management_through_the_bff_on_set_submit()
    {
        // The page must call BffComposition.ReauthorizeConfigurationManagementAsync rather than returning
        // the render-time snapshot context. Returning `_snapshot.ConfigurationManagement` would fail open:
        // the render-time grant would authorize the submit even after policy revoked it.
        JSInterop.Mode = JSRuntimeMode.Loose;
        StubTenantCommandGateway gateway = new()
        {
            SetConfigurationSubmission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-config"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        StubTenantsBffComposition composition = new()
        {
            ReauthorizeConfigurationManagement = static (tenantId, status, _)
                => TenantConfigurationManagementContext.Unavailable(tenantId, status),
        };
        ITenantQueryGateway queryGateway = Substitute.For<ITenantQueryGateway>();
        queryGateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(ReadyWithSafeConfiguration(
                Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))));
        queryGateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(MemberSnapshot(Detail("tenant.alpha"))));
        Services.AddSingleton(queryGateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);
        Services.AddSingleton<ITenantsBffComposition>(composition);
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-config-set-open']");

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("billing.mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("production");
        // Scope to the set-flow form: the detail page hosts other forms, and clicking the Submit button
        // is a no-op while IsSubmitDisabled is true (preview not yet complete in some bUnit sequences).
        // EditForm.Submit() invokes OnSubmit directly, matching the standalone flow tests.
        cut.Find("[data-testid='tenants-config-set-flow'] form").Submit();

        composition.ReauthorizeConfigurationManagementCallCount.ShouldBeGreaterThan(0);
        gateway.SetConfigurationCallCount.ShouldBe(0);
    }

    [Fact]
    public void Detail_page_configuration_summary_renders_unavailable_when_policy_cannot_be_verified()
    {
        // Deleting the `!configuration.IsAvailable` branch would render the absence claim for unverifiable
        // policy. The sibling read landmark already pins this distinction; the page summary must too.
        TenantDetail detail = Detail("tenant.alpha");
        TenantConfigurationComposition unavailableComposition = new(
            TenantConfigurationSafeComposer.SanitizeDetail(detail),
            TenantConfigurationSafeModel.Unavailable(detail.TenantId),
            TenantConfigurationManagementContext.Unavailable(detail.TenantId, detail.Status));
        RegisterServices(_ => Task.FromResult(TenantDetailSnapshot.Ready(
            unavailableComposition,
            "\"etag\"",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            "projection-v1")));

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-detail-configuration-summary']");

        string summary = cut.Find("[data-testid='tenants-detail-configuration-summary']").TextContent;
        summary.ShouldContain("Configuration unavailable");
        summary.ShouldNotContain("No visible configuration");
    }

    [Fact]
    public void Detail_page_configuration_summary_renders_valid_empty_without_unavailable_copy()
    {
        RegisterServices(_ => Task.FromResult(ReadyWithSafeConfiguration(
            Detail("tenant.alpha", new Dictionary<string, string>()))));

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-detail-configuration-summary']");

        string summary = cut.Find("[data-testid='tenants-detail-configuration-summary']").TextContent;
        summary.ShouldContain("No visible configuration");
        summary.ShouldNotContain("Configuration unavailable");
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
        // aria-label is prohibited on role=code, so the literal must reach AT as text content instead.
        cut.Find("[data-testid='tenants-config-read-key']").TextContent.ShouldBe("billing.mode");
        cut.Find("[data-testid='tenants-config-read-value']").TextContent.ShouldBe("trial");
        cut.Find("[data-testid='tenants-config-read-key']").GetAttribute("aria-label").ShouldBeNull();
        cut.Find("[data-testid='tenants-config-read-value']").GetAttribute("aria-label").ShouldBeNull();
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
        empty.Find("[data-testid='tenants-config-read-empty']").GetAttribute("aria-describedby")
            .ShouldBe("tenants-config-read-announcer");
        empty.Find("[data-testid='tenants-config-read-empty']").HasAttribute("role").ShouldBeFalse();
        empty.Markup.ShouldNotContain("tenants-config-read-filtered-empty");

        IRenderedComponent<TenantConfigurationView> filtered = Render<TenantConfigurationView>(parameters => parameters
            .Add(view => view.Model, SafeConfiguration(("billing", "billing.mode", "trial")))
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current));

        filtered.Find("[data-testid='tenants-config-read-filter']").Change("missing");

        filtered.Find("[data-testid='tenants-config-read-filtered-empty']").TextContent.ShouldContain("No visible configuration matches");
        filtered.Find("[data-testid='tenants-config-read-announcer']").TextContent.ShouldContain("0 visible configuration entries");

        // Both empty panels are described by the announcer, and neither carries a role of its own on that
        // basis. Nothing asserted the association, so deleting either attribute left the suite green while
        // the panels' own justification for having no role silently stopped being true. The announcer's id
        // is asserted too: aria-describedby pointing at an element that does not exist is the same defect
        // one level down.
        filtered.Find("[data-testid='tenants-config-read-announcer']").GetAttribute("id")
            .ShouldBe("tenants-config-read-announcer");
        filtered.Find("[data-testid='tenants-config-read-filtered-empty']").GetAttribute("aria-describedby")
            .ShouldBe("tenants-config-read-announcer");
        filtered.Find("[data-testid='tenants-config-read-filtered-empty']").HasAttribute("role").ShouldBeFalse();
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

        // Routine result counts stay polite; assertive belongs to the truth state, which owns its own region.
        cut.Find("[data-testid='tenants-config-read-announcer']").GetAttribute("aria-live").ShouldBe("polite");
        cut.Find("[data-testid='tenants-config-read-filter']").GetAttribute("aria-describedby").ShouldBe("tenants-config-read-filter-help");
        cut.Find("[data-testid='tenants-config-read-clear-filter']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-config-read-table']").GetAttribute("tabindex").ShouldBe("0");
        cut.Find("[data-testid='tenants-config-read-table']").GetAttribute("aria-label").ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    // data-state is the text-independent discriminator the production comment promises, so the expected
    // value is named per row: asserting only ShouldNotBeNullOrWhiteSpace passed for any constant, including
    // a hardcoded one, because the value is a substring of a prefix that is never blank.
    //
    // role and aria-live are asserted together. role="status" carries an implicit polite live region, so an
    // assertive escalation has to switch the role to "alert" or the two contradict each other; asserting a
    // fixed "status" pinned the contradiction rather than the behaviour.
    [InlineData(TenantDetailSurfaceKind.Stale, ReadModelFreshnessState.Stale, "stale", "Stale", "status", "polite")]
    [InlineData(TenantDetailSurfaceKind.Degraded, ReadModelFreshnessState.Unknown, "degraded", "Degraded", "alert", "assertive")]
    [InlineData(TenantDetailSurfaceKind.Unknown, ReadModelFreshnessState.Unknown, "Unknown", "Unknown", "alert", "assertive")]
    public void Configuration_read_view_surfaces_non_current_truth_without_collapsing_to_success(
        TenantDetailSurfaceKind kind,
        ReadModelFreshnessState freshness,
        string expectedText,
        string expectedStateKind,
        string expectedRole,
        string expectedPoliteness)
    {
        RegisterComponentServices();

        IRenderedComponent<TenantConfigurationView> cut = Render<TenantConfigurationView>(parameters => parameters
            .Add(view => view.Model, SafeConfiguration(("billing", "billing.mode", "trial")))
            .Add(view => view.SurfaceKind, kind)
            .Add(view => view.Freshness, freshness)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current));

        // The truth sentence has one owner: the state section, which is also the only live region for it.
        // It used to be duplicated into the header badge row and was never announced from there.
        cut.Find("[data-testid='tenants-config-read-state']").TextContent.ShouldContain(expectedText, Case.Insensitive);
        cut.Find("[data-testid='tenants-config-read-state']").GetAttribute("data-state").ShouldBe(expectedStateKind);
        cut.Find("[data-testid='tenants-config-read-state']").GetAttribute("role").ShouldBe(expectedRole);
        cut.Find("[data-testid='tenants-config-read-state']").GetAttribute("aria-live").ShouldBe(expectedPoliteness);

        // Asserted over rendered text, not raw markup. ShouldNotContain is case-insensitive, so scanning the
        // markup also scans class tokens and attribute names -- the Current lifecycle badge carries a
        // success-toned class -- which is the incidental-markup coupling the project rules forbid. What must
        // never appear is a success claim the operator can read.
        cut.Find("section.tenant-config").TextContent.ShouldNotContain("Success", Case.Insensitive);
    }

    [Theory]
    // The lifecycle read state had no test at all: deleting `|| HasNonCurrentLifecycle` from HasNonCurrentState
    // and collapsing the ProjectionLifecycle arm to Ready both survived the whole suite. The reachable shape
    // is Freshness=Current with Lifecycle=Unknown -- a response with no lifecycle header on the wire.
    //
    // The copy is asserted as a whole localized string, not a substring: the title used to claim "Projection
    // rebuilding" for all six non-Current lifecycles, including Unknown, which asserts a rebuild that is not
    // happening over a surface whose real problem is that it has no lifecycle evidence.
    [InlineData(ProjectionLifecycleState.Unknown)]
    [InlineData(ProjectionLifecycleState.Rebuilding)]
    [InlineData(ProjectionLifecycleState.Degraded)]
    [InlineData(ProjectionLifecycleState.Unavailable)]
    [InlineData(ProjectionLifecycleState.LocalOnly)]
    public void Configuration_read_view_reports_a_non_current_projection_lifecycle_without_claiming_a_rebuild(
        ProjectionLifecycleState lifecycle)
    {
        RegisterComponentServices();

        IRenderedComponent<TenantConfigurationView> cut = Render<TenantConfigurationView>(parameters => parameters
            .Add(view => view.Model, SafeConfiguration(("billing", "billing.mode", "trial")))
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, lifecycle));

        IElement state = cut.Find("[data-testid='tenants-config-read-state']");
        state.GetAttribute("data-state").ShouldBe("ProjectionLifecycle");
        state.TextContent.ShouldContain("Projection not current");
        state.TextContent.ShouldContain("cannot be treated as authoritative");
        state.TextContent.ShouldNotContain("rebuilding", Case.Insensitive);
        state.TextContent.ShouldNotContain("evidence is current", Case.Insensitive);

        // An Unavailable lifecycle is the one that escalates the live region, and role has to follow it or
        // role="status"'s implicit polite live region contradicts the assertive attribute. Replacing
        // LivePoliteness with the literal "polite" made this escalation disappear silently: the only
        // aria-live assertion in this file was on the announcer, a different element.
        bool expectAssertive = lifecycle is ProjectionLifecycleState.Unavailable;
        state.GetAttribute("aria-live").ShouldBe(expectAssertive ? "assertive" : "polite");
        state.GetAttribute("role").ShouldBe(expectAssertive ? "alert" : "status");
    }

    /// <summary>
    /// Accordion groups are keyed by namespace, so a reorder moves the group, not its expanded state.
    /// </summary>
    /// <remarks>
    /// Without <c>@key</c> Blazor reuses accordion items positionally, so the component's own DOM-held
    /// collapsed state follows position instead of namespace: collapse "billing", let the projection return
    /// the namespaces in a different order, and "identity" is now the collapsed one. The mutation that
    /// deletes the key survived because no test drove expansion across two renders.
    /// </remarks>
    [Fact]
    public void Configuration_group_identity_follows_the_namespace_across_a_reorder()
    {
        RegisterComponentServices();
        IRenderedComponent<TenantConfigurationView> cut = Render<TenantConfigurationView>(parameters => parameters
            .Add(view => view.Model, SafeConfiguration(
                ("billing", "billing.mode", "trial"),
                ("identity", "identity.region", "eu")))
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current));

        IReadOnlyList<IElement> initial = cut.FindAll("fluent-accordion-item");
        initial.Count.ShouldBe(2);
        string?[] initialKeys = [.. initial.Select(static item => item.GetAttribute("id"))];

        // Same namespaces, opposite order: what identifies a group is its namespace, not its position.
        cut.Render(parameters => parameters
            .Add(view => view.Model, SafeConfiguration(
                ("identity", "identity.region", "eu"),
                ("billing", "billing.mode", "trial")))
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current));

        IReadOnlyList<IElement> reordered = cut.FindAll("fluent-accordion-item");
        reordered.Count.ShouldBe(2);

        // Keyed rendering moves the existing element with its namespace, so the identity assigned to the
        // "billing" group in the first render is the one still attached to it after the reorder. Positional
        // reuse would instead leave the first slot's identity in place while its content changed.
        string?[] reorderedKeys = [.. reordered.Select(static item => item.GetAttribute("id"))];
        reorderedKeys.ShouldBe(initialKeys.Reverse().ToArray());

        // ...and the rows travel with their group rather than being re-bound onto the other one.
        reordered[0].TextContent.ShouldContain("identity.region");
        reordered[1].TextContent.ShouldContain("billing.mode");
    }

    [Fact]
    public void Configuration_read_view_reports_ready_only_when_lifecycle_and_freshness_are_both_current()
    {
        RegisterComponentServices();

        IRenderedComponent<TenantConfigurationView> cut = Render<TenantConfigurationView>(parameters => parameters
            .Add(view => view.Model, SafeConfiguration(("billing", "billing.mode", "trial")))
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current));

        cut.FindAll("[data-testid='tenants-config-read-state']").ShouldBeEmpty();
    }

    [Fact]
    public async Task Management_keeps_an_open_remove_flow_mounted_after_its_target_row_leaves_the_context()
    {
        // The refresh that proves a removal drops the row from RemovableRows. Unmounting the flow on that
        // signal destroyed the Confirmed state, the audit entry point and the recovery text at the exact
        // moment the projection proved the removal, and orphaned focus on the destroyed subtree.
        RegisterComponentServices();
        TenantConfigurationSafeRow row = new("billing", "billing.mode", "trial");
        TenantConfigurationManagementContext withRow = TenantConfigurationManagementContext.Available(
            "tenant.alpha", TenantStatus.Active, false, ["billing"], [row]);

        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, withRow)
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-management-remove-open']").Click();
        cut.Find("[data-testid='tenants-config-remove-flow']");

        // The flow reports its command activity before the parent refresh lands.
        IRenderedComponent<RemoveTenantConfigurationFlow> flow = cut.FindComponent<RemoveTenantConfigurationFlow>();
        await cut.InvokeAsync(() => flow.Instance.OnCommandActivityChanged.InvokeAsync(true));

        TenantConfigurationManagementContext withoutRow = TenantConfigurationManagementContext.Available(
            "tenant.alpha", TenantStatus.Active, false, ["billing"], []);
        cut.Render(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, withoutRow)
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.FindAll("[data-testid='tenants-config-remove-flow']").ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Management_keeps_both_flows_mounted_when_the_command_surface_goes_unavailable_under_its_own_command()
    {
        // The command surface goes unavailable *because* a flow submitted: the flow raises activity, the
        // detail page sets _commandInFlight, and IsCommandSurfaceAvailable comes straight back down here.
        // Without the in-flight exception on the CommandSurface clause, this branch replaced both flows with
        // a static paragraph at the moment of submit, so Confirmed, Rejected, UnableToVerify, the safe
        // message and the audit entry point were never renderable for a configuration command -- the
        // non-collapse guarantee, defeated one level above where both flows already implement it.
        RegisterComponentServices();
        TenantConfigurationSafeRow row = new("billing", "billing.mode", "trial");
        TenantConfigurationManagementContext context = TenantConfigurationManagementContext.Available(
            "tenant.alpha", TenantStatus.Active, false, ["billing"], [row]);

        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, context)
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.IsCommandSurfaceAvailable, true));

        cut.Find("[data-testid='tenants-config-management-remove-open']").Click();
        IRenderedComponent<RemoveTenantConfigurationFlow> flow = cut.FindComponent<RemoveTenantConfigurationFlow>();
        await cut.InvokeAsync(() => flow.Instance.OnCommandActivityChanged.InvokeAsync(true));

        // The page reflects the in-flight command back as an unavailable command surface.
        cut.Render(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, context)
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.IsCommandSurfaceAvailable, false));

        cut.FindAll("[data-testid='tenants-config-remove-flow']").ShouldNotBeEmpty();
        cut.FindAll("[data-testid='tenants-config-management-unavailable']").ShouldBeEmpty();

        // Once the command settles, the ordinary unavailable rule applies again.
        await cut.InvokeAsync(() => flow.Instance.OnCommandActivityChanged.InvokeAsync(false));
        cut.Render(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, context)
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.IsCommandSurfaceAvailable, false));

        cut.FindAll("[data-testid='tenants-config-remove-flow']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-config-management-unavailable']").ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Dismissing_remove_does_not_release_a_sibling_set_command_lease()
    {
        RegisterComponentServices();
        TenantConfigurationManagementContext context = TenantConfigurationManagementContext.Available(
            "tenant.alpha",
            TenantStatus.Active,
            false,
            ["billing"],
            [new TenantConfigurationSafeRow("billing", "billing.mode", "trial")]);

        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, context)
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.IsCommandSurfaceAvailable, true));

        IRenderedComponent<SetTenantConfigurationFlow> setFlow = cut.FindComponent<SetTenantConfigurationFlow>();
        await cut.InvokeAsync(() => setFlow.Instance.OnCommandActivityChanged.InvokeAsync(true));
        cut.Render(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, context)
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.IsCommandSurfaceAvailable, false));

        cut.Find("[data-testid='tenants-config-management-remove-open']").Click();
        IRenderedComponent<RemoveTenantConfigurationFlow> removeFlow = cut.FindComponent<RemoveTenantConfigurationFlow>();
        await cut.InvokeAsync(() => removeFlow.Instance.OnCloseRequested.InvokeAsync());

        cut.FindAll("[data-testid='tenants-config-remove-flow']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-config-set-flow']").ShouldNotBeEmpty();
        cut.FindAll("[data-testid='tenants-config-management-unavailable']").ShouldBeEmpty();

        setFlow = cut.FindComponent<SetTenantConfigurationFlow>();
        await cut.InvokeAsync(() => setFlow.Instance.OnCommandActivityChanged.InvokeAsync(false));
        cut.Render(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, context)
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.IsCommandSurfaceAvailable, false));
        cut.FindAll("[data-testid='tenants-config-management-unavailable']").ShouldNotBeEmpty();
    }

    [Fact]
    public void Management_associates_its_unavailable_reason_with_the_landmark_it_disables()
    {
        // An inline reason has to be programmatically associated, not merely adjacent. The paragraph carried
        // an id that nothing referenced, so assistive technology never connected it to the region whose
        // controls it explains -- and deleting the binding survived the whole suite.
        RegisterComponentServices();
        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, TenantConfigurationManagementContext.Available(
                "tenant.alpha", TenantStatus.Active, false, ["billing"],
                [new TenantConfigurationSafeRow("billing", "billing.mode", "trial")]))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Stale)
            .Add(p => p.Freshness, ReadModelFreshnessState.Stale));

        string reasonId = cut.Find("[data-testid='tenants-config-management-unavailable']").Id.ShouldNotBeNull();
        cut.Find("[data-testid='tenants-config-management-section']")
            .GetAttribute("aria-describedby").ShouldBe(reasonId);
    }

    [Fact]
    public void Management_drops_the_association_when_nothing_is_disabled()
    {
        RegisterComponentServices();
        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, TenantConfigurationManagementContext.Available(
                "tenant.alpha", TenantStatus.Active, false, ["billing"],
                [new TenantConfigurationSafeRow("billing", "billing.mode", "trial")]))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.FindAll("[data-testid='tenants-config-management-unavailable']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-config-management-section']")
            .GetAttribute("aria-describedby").ShouldBeNull();
    }

    [Fact]
    public async Task Management_moves_focus_to_the_landmark_heading_when_the_row_it_came_from_is_gone()
    {
        // A successful removal deletes the row whose launch control focus would return to. The fallback was
        // armed only in OnParametersSet, but CloseRemoveFlowAsync runs as an EventCallback from the child --
        // which re-renders this component without running SetParametersAsync -- so on the one path that
        // needs it, neither branch of OnAfterRenderAsync ran and focus fell to <body>.
        //
        // Focus goes through JS interop, so the discriminator is that a focus call is made at all on this
        // path: with the fallback neutered, none is.
        RegisterComponentServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        TenantConfigurationSafeRow row = new("billing", "billing.mode", "trial");

        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, TenantConfigurationManagementContext.Available(
                "tenant.alpha", TenantStatus.Active, false, ["billing"], [row]))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-management-remove-open']").Click();
        IRenderedComponent<RemoveTenantConfigurationFlow> flow = cut.FindComponent<RemoveTenantConfigurationFlow>();
        await cut.InvokeAsync(() => flow.Instance.OnCommandActivityChanged.InvokeAsync(true));

        // The projection proves the removal: the row leaves, and with it the launch control focus would
        // otherwise return to.
        cut.Render(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, TenantConfigurationManagementContext.Available(
                "tenant.alpha", TenantStatus.Active, false, ["billing"], []))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        int focusCallsBeforeClose = FocusedElementIds().Count;

        await cut.InvokeAsync(() => flow.Instance.OnCloseRequested.InvokeAsync());

        // WHICH element received focus, not merely that a focus call happened. ElementReference.FocusAsync
        // routes every element through the same interop identifier, and this component has two focus paths
        // (_focusRemoveLaunchKey -> the row's launch span, _focusHeadingPending -> the landmark heading), so
        // counting calls could not tell them apart -- nor could it tell either of them from a dialog's own
        // focus sentinels. bUnit renders the element-reference marker without its id, so the target cannot be
        // correlated through markup; the component's captured reference is read directly instead.
        IReadOnlyList<string> focusedAfterClose = FocusedElementIds();
        focusedAfterClose.Count.ShouldBe(focusCallsBeforeClose + 1);
        focusedAfterClose[^1].ShouldBe(CapturedElementReferenceId(cut.Instance, "_headingElement"));

        // The fallback target has to be programmatically focusable.
        cut.Find("#tenants-config-management-heading").GetAttribute("tabindex").ShouldBe("-1");

        // A later projection render must not replay the one-shot fallback and steal focus from whatever the
        // operator moved to after dismissal.
        cut.Render(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, TenantConfigurationManagementContext.Available(
                "tenant.alpha", TenantStatus.Active, false, ["billing"], []))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));
        FocusedElementIds().Count.ShouldBe(focusedAfterClose.Count);
    }

    [Fact]
    public void Management_reports_a_failed_read_rather_than_the_projection_lifecycle()
    {
        // Decision D-F clause ordering at the landmark: an Unavailable surface also carries a non-Current
        // lifecycle, so with the lifecycle clause first the operator was told to refresh the projection
        // lifecycle when the read had simply failed.
        RegisterComponentServices();
        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Unavailable)
            .Add(p => p.Context, TenantConfigurationManagementContext.Available(
                "tenant.alpha", TenantStatus.Active, false, ["billing"],
                [new TenantConfigurationSafeRow("billing", "billing.mode", "trial")]))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Unavailable)
            .Add(p => p.Freshness, ReadModelFreshnessState.Unknown));

        string reason = cut.Find("[data-testid='tenants-config-management-unavailable']").TextContent.Trim();
        reason.ShouldBe("Refresh available tenant detail before managing configuration.");
    }

    /// <summary>
    /// The metadata flow reports a failed read rather than the projection lifecycle.
    /// </summary>
    /// <remarks>
    /// Decision D-F applies at five gate sites and requires a test per site pinning the reason string for a
    /// failed-read snapshot. This was the unguarded one: restoring the pre-D-F clause order here left the
    /// suite green, while the identical mutation on <c>TenantConfigurationManagement</c> was killed by the
    /// sibling above. Every failed-read snapshot also carries a non-Current lifecycle, so with the lifecycle
    /// clause first an operator whose read had simply failed was told to refresh the projection lifecycle.
    /// </remarks>
    [Fact]
    public void Metadata_flow_reports_a_failed_read_rather_than_the_projection_lifecycle()
    {
        RegisterComponentServices();
        IRenderedComponent<EditTenantMetadataFlow> cut = Render<EditTenantMetadataFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Unavailable)
            .Add(p => p.Freshness, ReadModelFreshnessState.Unknown)
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Unavailable));

        string reason = cut.Find("[data-testid='tenants-edit-metadata-unavailable-reason']").TextContent.Trim();
        reason.ShouldBe("Refresh current tenant detail before editing metadata.");
    }

    [Fact]
    public void Management_closes_an_untouched_remove_flow_when_its_target_row_disappears()
    {
        // The stale-target reset still applies while no command has been submitted: nothing is settling,
        // so a flow pointing at a row that no longer exists is just stale.
        RegisterComponentServices();
        TenantConfigurationSafeRow row = new("billing", "billing.mode", "trial");
        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, TenantConfigurationManagementContext.Available(
                "tenant.alpha", TenantStatus.Active, false, ["billing"], [row]))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-management-remove-open']").Click();
        cut.Find("[data-testid='tenants-config-remove-flow']");

        cut.Render(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, TenantConfigurationManagementContext.Available(
                "tenant.alpha", TenantStatus.Active, false, ["billing"], []))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.FindAll("[data-testid='tenants-config-remove-flow']").ShouldBeEmpty();
    }

    [Fact]
    public async Task Management_switching_tenants_drops_every_open_interaction_even_mid_command()
    {
        RegisterComponentServices();
        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, TenantConfigurationManagementContext.Available(
                "tenant.alpha", TenantStatus.Active, false, ["billing"],
                [new TenantConfigurationSafeRow("billing", "billing.mode", "trial")]))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-management-remove-open']").Click();
        IRenderedComponent<RemoveTenantConfigurationFlow> flow = cut.FindComponent<RemoveTenantConfigurationFlow>();
        await cut.InvokeAsync(() => flow.Instance.OnCommandActivityChanged.InvokeAsync(true));

        cut.Render(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, TenantConfigurationManagementContext.Available(
                "tenant.beta", TenantStatus.Active, false, ["billing"],
                [new TenantConfigurationSafeRow("billing", "billing.mode", "trial")]))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.FindAll("[data-testid='tenants-config-remove-flow']").ShouldBeEmpty();
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
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
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
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(component => component.Context, context)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-management-remove-open']").Click();
        int focusCallsBeforeClose = FocusedElementIds().Count;

        cut.Find("[data-testid='tenants-config-remove-cancel']").Click();

        // The row is still present here, so focus must return to ITS launch control -- the other of the two
        // focus paths, and the one the sibling test above proves is not taken when the row is gone.
        IReadOnlyList<string> focusedAfterCancel = FocusedElementIds();
        focusedAfterCancel.Count.ShouldBe(focusCallsBeforeClose + 1);
        focusedAfterCancel[^1].ShouldBe(CapturedLaunchElementReferenceId(cut.Instance, "billing.mode"));
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
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
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
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
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
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
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
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(component => component.Context, empty)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current));

        validEmpty.Find("[data-testid='tenants-config-management-empty']");
        validEmpty.Find("[data-testid='tenants-config-set-flow']");
        validEmpty.FindAll("[data-testid='tenants-config-management-unavailable']").ShouldBeEmpty();

        IRenderedComponent<TenantConfigurationManagement> policyUnavailable = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(component => component.Context, TenantConfigurationManagementContext.Unavailable("tenant.alpha"))
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current));

        policyUnavailable.Find("[data-testid='tenants-config-management-unavailable']");
        policyUnavailable.FindAll("[data-testid='tenants-config-management-empty']").ShouldBeEmpty();
        policyUnavailable.FindAll("[data-testid='tenants-config-set-flow']").ShouldBeEmpty();

        IRenderedComponent<TenantConfigurationManagement> stale = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(component => component.Context, empty)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Stale)
            .Add(component => component.Freshness, ReadModelFreshnessState.Stale));

        stale.Find("[data-testid='tenants-config-management-unavailable']");
        stale.FindAll("[data-testid='tenants-config-management-empty']").ShouldBeEmpty();
        stale.FindAll("[data-testid='tenants-config-set-flow']").ShouldBeEmpty();
    }

    [Fact]
    public void Configuration_management_explains_noncurrent_projection_lifecycle_and_hides_mutation_flows()
    {
        RegisterComponentServices();
        TenantConfigurationManagementContext context = TenantConfigurationManagementContext.Available(
            "tenant.alpha",
            TenantStatus.Active,
            false,
            ["billing"],
            [new TenantConfigurationSafeRow("billing", "billing.mode", "trial")]);

        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(component => component.Context, context)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.Lifecycle, ProjectionLifecycleState.Stale));

        cut.Find("[data-testid='tenants-config-management-unavailable']").TextContent
            .ShouldContain("projection-confirmed lifecycle", Case.Insensitive);
        cut.FindAll("[data-testid='tenants-config-set-flow']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-config-management-remove-open']").ShouldBeEmpty();
    }

    [Fact]
    public void Detail_page_composes_member_access_review_without_replacing_existing_surfaces()
    {
        RegisterServices(_ => Task.FromResult(ReadyWithSafeConfiguration(Detail("tenant.alpha"))));

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-member-table']");

        // Asserted past the shared prefix: both summary variants begin "{0} members visible on this page",
        // so the previous ShouldContain("2 members") passed on either branch. This fixture builds the detail
        // snapshot with the default Current lifecycle and no projection version, so the evidence is not
        // version-consistent and the owner-context variant is correctly withheld. Both branches are covered
        // by Member_governance_claims_require_current_lifecycle_and_a_stated_matching_projection_version.
        cut.Find("[data-testid='tenants-detail-member-summary']").TextContent
            .ShouldContain("2 members visible on this page. Owner context is unavailable");
        cut.Find("[data-testid='tenants-member-section']").TextContent.ShouldContain("Member access review");
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("owner-user");
        cut.Find("[data-testid='tenants-config-read-table']").TextContent.ShouldContain("billing.mode");
        cut.Markup.ShouldContain("data-testid=\"tenants-member-truth-badge\"");
        cut.Markup.ShouldNotContain("tenants-list-truth-state");
    }

    [Fact]
    public void Detail_page_keeps_freshness_and_projection_lifecycle_as_separate_consumer_bindings()
    {
        RegisterServices(_ => Task.FromResult(ReadyWithSafeConfiguration(
            Detail("tenant.alpha"),
            ProjectionLifecycleState.Stale)));

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-detail-projection-lifecycle']");

        cut.Find("[data-testid='tenants-detail-truth-state']")
            .GetAttribute("class").ShouldNotBeNull().ShouldContain("truth-state-badge--current");
        cut.Find("[data-testid='tenants-detail-projection-lifecycle']")
            .GetAttribute("class").ShouldNotBeNull().ShouldContain("projection-lifecycle-badge--stale");
        cut.Find("[data-testid='tenants-detail-projection-lifecycle-status']")
            .GetAttribute("role").ShouldBe("status");

        // Badge classes are asserted alongside the localized label, not instead of it: a class name is
        // incidental markup, and asserting it alone is what let the resource keys behind these badges go
        // missing without any test noticing.
        cut.Find("[data-testid='tenants-detail-projection-lifecycle']").TextContent.ShouldContain("Stale");
    }

    [Fact]
    public void Detail_page_escalates_the_lifecycle_region_to_alert_only_when_the_projection_is_unavailable()
    {
        // role="status" is implicitly polite, so an assertive escalation must change the role rather than
        // add a contradicting aria-live. Only the "status" side of that ternary was covered, so collapsing
        // it to a constant survived the whole suite.
        RegisterServices(_ => Task.FromResult(ReadyWithSafeConfiguration(
            Detail("tenant.alpha"),
            ProjectionLifecycleState.Unavailable)));

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-detail-projection-lifecycle']");

        cut.Find("[data-testid='tenants-detail-projection-lifecycle-status']")
            .GetAttribute("role").ShouldBe("alert");
        cut.Find("[data-testid='tenants-detail-projection-lifecycle']").TextContent.ShouldContain("Unavailable");
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
        cut.Find("[data-testid='tenants-member-truth-badge']").TextContent.ShouldContain("Current");
        cut.Find("[data-testid='tenants-member-projection-lifecycle-status']").GetAttribute("role").ShouldBe("status");
        cut.Find("[data-testid='tenants-member-projection-lifecycle-badge']").TextContent.Trim().ShouldBe("Current");
        cut.Markup.ShouldContain("aria-describedby=\"tenants-member-reasons-0\"");
        cut.Markup.ShouldContain("aria-label=\"Literal member user identifier OWNER/User.01\"");
        cut.Find("[data-testid='tenants-member-reason-list']").GetAttribute("tabindex").ShouldBe("0");
    }

    [Fact]
    public void Member_access_review_keeps_lifecycle_badge_independent_of_freshness()
    {
        RegisterComponentServices();
        TenantDetail detail = Detail(
            "tenant.alpha",
            new Dictionary<string, string>(),
            TenantStatus.Active,
            [new TenantMember("owner-user", TenantRole.TenantOwner)]);

        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, MemberSnapshot(detail) with
            {
                Freshness = ReadModelFreshnessState.Current,
                Lifecycle = ProjectionLifecycleState.Stale,
            }));

        cut.Find("[data-testid='tenants-member-truth-badge']").TextContent.ShouldContain("Current");
        cut.Find("[data-testid='tenants-member-projection-lifecycle-badge']").TextContent.Trim().ShouldBe("Stale");
        (cut.Find("[data-testid='tenants-member-projection-lifecycle-badge']").GetAttribute("class") ?? string.Empty)
            .ShouldContain("projection-lifecycle-badge--stale");
        cut.Find("[data-testid='tenants-member-row-projection-lifecycle']").TextContent.Trim().ShouldBe("Stale");
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
    public void Member_access_review_wires_complete_ga_standing_into_known_platform_friction()
    {
        RegisterComponentServices();
        TenantDetail detail = Detail("tenant.alpha");
        int readerIndex = detail.Members
            .Select((member, index) => (member.UserId, index))
            .First(pair => string.Equals(pair.UserId, "reader-user", StringComparison.Ordinal))
            .index;
        GlobalAdministratorsSnapshot completeGa = GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("reader-user", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"ga\"",
            freshness: ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            IsCompleteEvidence = true,
        };

        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, MemberSnapshot(detail))
            .Add(view => view.GlobalAdministrators, completeGa));

        cut.FindAll("[data-testid='tenants-remove-member-open']")[readerIndex].Click();

        cut.Find("[data-testid='tenants-remove-member-platform-standing']").TextContent
            .ShouldContain("Also a global administrator");
        cut.Find("[data-testid='tenants-remove-member-global-admin-risk']").TextContent
            .ShouldContain("will not remove global-administrator authority");
    }

    [Fact]
    public void Member_access_review_keeps_platform_standing_unknown_when_ga_evidence_is_incomplete()
    {
        RegisterComponentServices();
        TenantDetail detail = Detail("tenant.alpha");
        int readerIndex = detail.Members
            .Select((member, index) => (member.UserId, index))
            .First(pair => string.Equals(pair.UserId, "reader-user", StringComparison.Ordinal))
            .index;
        GlobalAdministratorsSnapshot incompleteGa = GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("reader-user", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"ga\"",
            freshness: ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            IsCompleteEvidence = false,
        };

        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, MemberSnapshot(detail))
            .Add(view => view.GlobalAdministrators, incompleteGa));

        cut.FindAll("[data-testid='tenants-remove-member-open']")[readerIndex].Click();

        cut.Find("[data-testid='tenants-remove-member-platform-standing']").TextContent
            .ShouldContain("unproven");
        cut.Markup.ShouldNotContain("Not reflected as a global administrator", Case.Insensitive);
        cut.FindAll("[data-testid='tenants-remove-member-global-admin-risk']").ShouldBeEmpty();
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

    /// <summary>
    /// Paging must not unmount an open member command flow.
    /// </summary>
    /// <remarks>
    /// ActiveChangeRoleMember and ActiveRemoveMember resolve against Members.Rows, so once the next page no
    /// longer contains the target the @if guard tears the flow component down mid-command and its lifecycle
    /// state, receipt and projection confirmation are destroyed with no announcement. The pager consulted
    /// only CanGoPrevious/HasMore/IsRefreshing and never the open-flow state.
    /// </remarks>
    [Fact]
    public void Member_paging_is_blocked_while_a_member_command_flow_is_open()
    {
        RegisterComponentServices();
        TenantDetail detail = Detail("tenant.alpha");
        TenantUsersSnapshot members = MemberSnapshot(detail) with { HasMore = true, NextCursor = "page-2" };

        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, members));

        cut.Find("[data-testid='tenants-member-next']").HasAttribute("disabled").ShouldBeFalse();

        cut.Find("[data-testid='tenants-change-role-open']").Click();
        cut.Find("[data-testid='tenants-change-role-flow']");

        cut.Find("[data-testid='tenants-member-next']").HasAttribute("disabled").ShouldBeTrue(
            "paging away from an open change-role flow destroys its lifecycle state and receipt");
    }

    [Fact]
    public void Member_command_flow_closes_when_tenant_identity_changes_with_the_same_cursor()
    {
        RegisterComponentServices();
        TenantDetail alpha = Detail("tenant.alpha");
        TenantDetail beta = Detail("tenant.beta");
        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, alpha)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, MemberSnapshot(alpha)));
        cut.FindAll("[data-testid='tenants-change-role-open']")[0].Click();
        cut.Find("[data-testid='tenants-change-role-flow']");

        cut.Render(parameters => parameters
            .Add(view => view.Detail, beta)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, MemberSnapshot(beta)));

        cut.FindAll("[data-testid='tenants-change-role-flow']").ShouldBeEmpty();
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

    // AC6: empty, unauthorized, not-found, error, degraded, stale and unknown remain distinct. The surface
    // previously routed every non-Empty kind through one "tenants-member-state" id and one
    // "Member read unavailable" title, so an authorization denial, a missing tenant, an expired cursor and a
    // failed read were indistinguishable to an operator and to any selector-based test.
    [Theory]
    [InlineData(TenantUsersSurfaceKind.Unauthorized, "tenants-member-unauthorized", "not authorized to view these members")]
    [InlineData(TenantUsersSurfaceKind.NotFound, "tenants-member-not-found", "member read was not found")]
    [InlineData(TenantUsersSurfaceKind.Invalid, "tenants-member-invalid", "no longer valid")]
    [InlineData(TenantUsersSurfaceKind.Unavailable, "tenants-member-unavailable", "cannot be loaded right now")]
    [InlineData(TenantUsersSurfaceKind.Error, "tenants-member-error", "member read failed")]
    [InlineData(TenantUsersSurfaceKind.Loading, "tenants-member-loading", "Loading visible members")]
    public void Member_access_review_renders_each_non_empty_read_state_distinctly(
        TenantUsersSurfaceKind kind,
        string expectedTestId,
        string expectedMessageFragment)
    {
        RegisterComponentServices();
        TenantDetail detail = Detail("tenant.alpha");

        // Built through the production factories, not `MemberSnapshot(detail) with { Kind = kind }`. Every
        // terminal read state routes through TenantUsersSnapshot.EmptyState, which forces ETag,
        // ProjectionVersion, Freshness and Lifecycle to their no-evidence values. The `with` form kept
        // eTag "members-etag", projectionVersion "v1" and Current/Current, so ActionsAreEvidenceBacked
        // evaluated TRUE against evidence no producer can emit and no regression in the evidence gate for a
        // terminal read state was catchable here.
        TenantUsersSnapshot members = TerminalMemberSnapshot(kind, detail.TenantId);

        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, members));

        cut.Find($"[data-testid='{expectedTestId}']").TextContent.ShouldContain(expectedMessageFragment);

        // Never the authorization-safe absence id, which claims the tenant genuinely has no visible members.
        cut.FindAll("[data-testid='tenants-member-empty']").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("tenants-member-row");

        // The member evidence gate must be closed for every one of these states: the detail read is fully
        // current, so only the member read can close it, and it can only do so because the fixture no longer
        // carries invented member evidence.
        members.ETag.ShouldBeNull();
        members.ProjectionVersion.ShouldBeNull();
        members.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
        members.Lifecycle.ShouldBe(ProjectionLifecycleState.Unknown);
        cut.Find("[data-testid='tenants-add-member-unavailable-reason']");
    }

    /// <summary>
    /// Builds a terminal member read state the way production does.
    /// </summary>
    private static TenantUsersSnapshot TerminalMemberSnapshot(TenantUsersSurfaceKind kind, string tenantId)
        => kind switch
        {
            TenantUsersSurfaceKind.Unauthorized => TenantUsersSnapshot.Unauthorized(tenantId),
            TenantUsersSurfaceKind.NotFound => TenantUsersSnapshot.NotFound(tenantId),
            TenantUsersSurfaceKind.Invalid => TenantUsersSnapshot.Invalid(tenantId),
            TenantUsersSurfaceKind.Unavailable => TenantUsersSnapshot.Unavailable(tenantId),
            TenantUsersSurfaceKind.Error => TenantUsersSnapshot.Error(tenantId),
            TenantUsersSurfaceKind.Loading => TenantUsersSnapshot.Loading(tenantId),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Not a terminal member read state."),
        };

    // The theory above omits Degraded, Stale and Unknown, so the three switch arms added for them could be
    // collapsed into the `_ => "tenants-member-state"` default with the suite green -- restoring exactly the
    // indistinguishable-state defect AC6 forbids. A repo-wide grep for these three ids returned a single
    // ShouldBeEmpty() negative before this test existed. Degraded and Stale share the generic title, so the
    // id is the only discriminator and is what this pins.
    [Theory]
    [InlineData(TenantUsersSurfaceKind.Degraded, "tenants-member-degraded")]
    [InlineData(TenantUsersSurfaceKind.Stale, "tenants-member-stale")]
    [InlineData(TenantUsersSurfaceKind.Unknown, "tenants-member-unknown")]
    public void Member_access_review_gives_degraded_stale_and_unknown_their_own_state_ids(
        TenantUsersSurfaceKind kind,
        string expectedTestId)
    {
        RegisterComponentServices();
        TenantDetail detail = Detail("tenant.alpha");

        // Degraded comes straight from its factory. Stale and Unknown reach a row-less surface through the
        // 304 branch of MapTenantUsersResponse, which re-kinds a retained row-less snapshot from the
        // response freshness -- so the shape below (no rows, no ETag, no projection version, freshness and
        // lifecycle agreeing) is what the producer can actually deliver. The previous
        // `MemberSnapshot(detail) with { Kind = kind }` form carried live ETag/version/Current evidence that
        // no factory emits for these kinds.
        TenantUsersSnapshot members = kind switch
        {
            TenantUsersSurfaceKind.Degraded => TenantUsersSnapshot.Degraded(
                detail.TenantId,
                previous: null,
                TenantUsersReason.GatewayFailure),
            TenantUsersSurfaceKind.Stale => TenantUsersSnapshot.Ready(
                detail.TenantId,
                [],
                nextCursor: null,
                hasMore: false,
                eTag: null,
                projectionVersion: null,
                ReadModelFreshnessState.Stale,
                ProjectionLifecycleState.Stale),
            _ => TenantUsersSnapshot.Ready(
                detail.TenantId,
                [],
                nextCursor: null,
                hasMore: false,
                eTag: null,
                projectionVersion: null,
                ReadModelFreshnessState.Unknown,
                ProjectionLifecycleState.Unknown),
        };
        members.Kind.ShouldBe(kind);
        members.IsAuthorizationScopedEmpty.ShouldBeFalse();

        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, members));

        cut.Find($"[data-testid='{expectedTestId}']");
        cut.FindAll("[data-testid='tenants-member-state']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-member-empty']").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("tenants-member-row");
    }

    // The shared MemberSnapshot fixture is built FROM detail.Members, so every test using it would still pass
    // if Rows, ActiveChangeRoleMember and ActiveRemoveMember were reverted to the pre-1.10 Detail.Members
    // source -- the exact defect this story exists to fix. This fixture is deliberately disjoint so the
    // authoritative read is the only thing that can produce the rendered rows and the flow targets.
    [Fact]
    public void Member_rows_and_command_flow_targets_come_only_from_the_authoritative_member_read()
    {
        RegisterComponentServices();
        TenantDetail detail = Detail(
            "tenant.alpha",
            new Dictionary<string, string>(),
            TenantStatus.Active,
            [new TenantMember("user.embedded", TenantRole.TenantOwner)]);
        TenantUsersSnapshot authoritative = TenantUsersSnapshot.Ready(
            "tenant.alpha",
            [new TenantMember("user.authoritative", TenantRole.TenantOwner)],
            nextCursor: null,
            hasMore: false,
            eTag: "members-etag",
            projectionVersion: "v1",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current);

        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, authoritative));

        cut.FindAll("[data-testid='tenants-member-row']").Count.ShouldBe(1);
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("user.authoritative");

        // The embedded detail member must not reach any rendered surface, including the flow targets.
        cut.Markup.ShouldNotContain("user.embedded");
    }

    [Fact]
    public void Member_access_review_announces_page_one_recovery_after_an_expired_cursor()
    {
        RegisterComponentServices();
        TenantDetail detail = Detail("tenant.alpha");
        TenantUsersSnapshot members = MemberSnapshot(detail) with
        {
            PagingRecovered = true,
            Reason = TenantUsersReason.ListRefreshed,
        };

        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, members));

        // epic-1-context: invalid or stale cursor state restarts at page 1 with an honest localized notice.
        IElement notice = cut.Find("[data-testid='tenants-member-page-recovered']");
        notice.GetAttribute("aria-live").ShouldBe("polite");
        notice.TextContent.ShouldContain("restarted at the first page");
    }

    // The owner context is computed from Detail.Members, which is a different read at a possibly different
    // projection version than the authoritative member page. When the two disagree it must not be rendered
    // as fact. This guard previously had no assertion anywhere in the suite.
    [Fact]
    public void Member_access_review_withholds_owner_context_when_member_and_detail_evidence_disagree()
    {
        RegisterComponentServices();
        TenantDetail detail = Detail("tenant.alpha");
        TenantUsersSnapshot mismatched = MemberSnapshot(detail) with { ProjectionVersion = "members-v2" };

        IRenderedComponent<MemberAccessReview> mismatch = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "detail-v1")
            .Add(view => view.Members, mismatched));

        string withheld = mismatch.Find("[data-testid='tenants-member-owner-context']").TextContent;
        withheld.ShouldContain("unavailable");
        withheld.ShouldNotContain("visible owner");

        IRenderedComponent<MemberAccessReview> agreeing = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, MemberSnapshot(detail)));

        agreeing.Find("[data-testid='tenants-member-owner-context']").TextContent
            .ShouldNotContain("unavailable");
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
    public void Invalid_member_cursor_recovers_page_one_and_clears_failed_history()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        List<TenantUsersRequest> memberRequests = [];
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadyWithSafeConfiguration(
                Detail("tenant.alpha"),
                ProjectionLifecycleState.Current,
                "projection-v1")));
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                TenantUsersRequest request = call.Arg<TenantUsersRequest>()
                    ?? throw new InvalidOperationException("A tenant-users request is required.");
                memberRequests.Add(request);
                return Task.FromResult(memberRequests.Count switch
                {
                    1 => TenantUsersSnapshot.Ready(
                        "tenant.alpha",
                        [new TenantMember("page-1-before", TenantRole.TenantOwner)],
                        nextCursor: "expired-page-2",
                        hasMore: true,
                        eTag: "members-page-1-before",
                        projectionVersion: "projection-v1",
                        ReadModelFreshnessState.Current,
                        ProjectionLifecycleState.Current),
                    2 => TenantUsersSnapshot.Invalid("tenant.alpha", TenantUsersReason.InvalidCursor),
                    3 => TenantUsersSnapshot.Ready(
                        "tenant.alpha",
                        [new TenantMember("page-1-recovered", TenantRole.TenantOwner)],
                        nextCursor: null,
                        hasMore: false,
                        eTag: "members-page-1-recovered",
                        projectionVersion: "projection-v1",
                        ReadModelFreshnessState.Current,
                        ProjectionLifecycleState.Current),
                    _ => throw new InvalidOperationException("Unexpected tenant-users request."),
                });
            });
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-member-next']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-member-table']")
            .TextContent.ShouldContain("page-1-recovered"));
        memberRequests.Select(static request => request.Cursor).ShouldBe([null, "expired-page-2", null]);
        cut.FindAll("[data-testid='tenants-member-previous']").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("page-1-before");
    }

    [Fact]
    public void Member_paging_authorization_failure_discards_previously_visible_rows()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        int memberReads = 0;
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadyWithSafeConfiguration(
                Detail("tenant.alpha"),
                ProjectionLifecycleState.Current,
                "projection-v1")));
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(Interlocked.Increment(ref memberReads) == 1
                ? TenantUsersSnapshot.Ready(
                    "tenant.alpha",
                    [new TenantMember("must-not-remain-visible", TenantRole.TenantOwner)],
                    "page-2",
                    true,
                    "members-page-1",
                    "projection-v1",
                    ReadModelFreshnessState.Current,
                    ProjectionLifecycleState.Current)
                : TenantUsersSnapshot.Unauthorized("tenant.alpha")));
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-member-next']").Click();

        cut.WaitForElement("[data-testid='tenants-member-unauthorized']");
        cut.Markup.ShouldNotContain("must-not-remain-visible");
    }

    [Fact]
    public async Task Notification_refresh_recovers_an_expired_member_cursor_at_page_one()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        IProjectionSubscription backendSubscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        backendSubscription
            .SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        List<TenantUsersRequest> memberRequests = [];
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadyWithSafeConfiguration(
                Detail("tenant.alpha"),
                ProjectionLifecycleState.Current,
                "projection-v1")));
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                TenantUsersRequest request = call.Arg<TenantUsersRequest>()!;
                memberRequests.Add(request);
                return Task.FromResult(memberRequests.Count switch
                {
                    1 => TenantUsersSnapshot.Ready(
                        "tenant.alpha",
                        [new TenantMember("page-1-before", TenantRole.TenantOwner)],
                        "page-2",
                        true,
                        "members-1",
                        "projection-v1",
                        ReadModelFreshnessState.Current,
                        ProjectionLifecycleState.Current),
                    2 => TenantUsersSnapshot.Ready(
                        "tenant.alpha",
                        [new TenantMember("page-2-before", TenantRole.TenantReader)],
                        null,
                        false,
                        "members-2",
                        "projection-v1",
                        ReadModelFreshnessState.Current,
                        ProjectionLifecycleState.Current),
                    3 => TenantUsersSnapshot.Invalid("tenant.alpha", TenantUsersReason.InvalidCursor),
                    4 => TenantUsersSnapshot.Ready(
                        "tenant.alpha",
                        [new TenantMember("page-1-recovered", TenantRole.TenantOwner)],
                        null,
                        false,
                        "members-recovered",
                        "projection-v1",
                        ReadModelFreshnessState.Current,
                        ProjectionLifecycleState.Current),
                    _ => throw new InvalidOperationException("Unexpected member read."),
                });
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
        cut.WaitForElement("[data-testid='tenants-member-next']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("page-2-before"));
        await backendSubscription.Received(1)
            .SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());

        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.alpha");

        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-member-table']")
            .TextContent.ShouldContain("page-1-recovered"));
        memberRequests.Select(static request => request.Cursor).ShouldBe([null, "page-2", "page-2", null]);
        cut.FindAll("[data-testid='tenants-member-previous']").ShouldBeEmpty();
    }

    /// <summary>
    /// A page-one recovery performed inside the gateway must reset the page's own cursor and history.
    /// </summary>
    /// <remarks>
    /// The gateway recovers an expired member cursor itself and returns page one. The page branched only on
    /// an explicit <c>InvalidCursor</c> reason, which a recovered read never carries, so it treated the
    /// answer as the requested page: it committed the expired cursor, pushed a history entry -- enabling
    /// Previous on a page-one view -- and re-sent the dead cursor on every later refresh. Three mutations
    /// survived the suite before this test existed.
    /// </remarks>
    [Fact]
    public async Task Gateway_side_member_page_recovery_resets_the_page_cursor_and_history()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        IProjectionSubscription backendSubscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        backendSubscription
            .SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        List<TenantUsersRequest> memberRequests = [];
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadyWithSafeConfiguration(
                Detail("tenant.alpha"),
                ProjectionLifecycleState.Current,
                "projection-v1")));
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                TenantUsersRequest request = call.Arg<TenantUsersRequest>()!;
                memberRequests.Add(request);
                return Task.FromResult(memberRequests.Count == 1
                    ? TenantUsersSnapshot.Ready(
                        "tenant.alpha",
                        [new TenantMember("page-1-user", TenantRole.TenantOwner)],
                        "page-2",
                        true,
                        "members-1",
                        "projection-v1",
                        ReadModelFreshnessState.Current,
                        ProjectionLifecycleState.Current)

                    // The shape the gateway produces after recovering internally: page one rows, no request
                    // cursor, ListRefreshed, PagingRecovered. Never an InvalidCursor reason.
                    : TenantUsersSnapshot.Ready(
                        "tenant.alpha",
                        [new TenantMember("page-1-recovered", TenantRole.TenantOwner)],
                        null,
                        false,
                        "members-recovered",
                        "projection-v1",
                        ReadModelFreshnessState.Current,
                        ProjectionLifecycleState.Current) with
                    {
                        Reason = TenantUsersReason.ListRefreshed,
                        PagingRecovered = true,
                    });
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
        cut.WaitForElement("[data-testid='tenants-member-next']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-member-table']")
            .TextContent.ShouldContain("page-1-recovered"));

        // The click really did ask for the now-dead cursor: the reset comes from the response, not the request.
        memberRequests[1].Cursor.ShouldBe("page-2");

        // The operator is told the list restarted...
        cut.Find("[data-testid='tenants-member-page-recovered']");

        // ...Previous is gone, because this is page one and the history was cleared...
        cut.FindAll("[data-testid='tenants-member-previous']").ShouldBeEmpty();

        // ...and a later notification refresh re-reads page one, not the dead cursor.
        await backendSubscription.Received(1)
            .SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.alpha");
        cut.WaitForAssertion(() => memberRequests.Count.ShouldBe(3));
        memberRequests[2].Cursor.ShouldBeNull();
    }

    [Fact]
    public async Task Repeated_member_next_click_while_loading_starts_only_one_page_read()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        List<TenantUsersRequest> memberRequests = [];
        var pendingPage = new TaskCompletionSource<TenantUsersSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadyWithSafeConfiguration(
                Detail("tenant.alpha"),
                ProjectionLifecycleState.Current,
                "projection-v1")));
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                TenantUsersRequest request = call.Arg<TenantUsersRequest>()
                    ?? throw new InvalidOperationException("A tenant-users request is required.");
                memberRequests.Add(request);
                return memberRequests.Count == 1
                    ? Task.FromResult(TenantUsersSnapshot.Ready(
                        "tenant.alpha",
                        [new TenantMember("page-1-user", TenantRole.TenantOwner)],
                        nextCursor: "cursor-page-2",
                        hasMore: true,
                        eTag: "members-page-1",
                        projectionVersion: "projection-v1",
                        ReadModelFreshnessState.Current,
                        ProjectionLifecycleState.Current))
                    : pendingPage.Task;
            });
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        IElement next = cut.WaitForElement("[data-testid='tenants-member-next']");

        Task firstClick = next.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        cut.WaitForAssertion(() =>
        {
            memberRequests.Count.ShouldBe(2);
            cut.Find("[data-testid='tenants-member-next']").HasAttribute("disabled").ShouldBeTrue();
        });
        await cut.Find("[data-testid='tenants-member-next']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        memberRequests.Count.ShouldBe(2);

        pendingPage.SetResult(TenantUsersSnapshot.Ready(
            "tenant.alpha",
            [new TenantMember("page-2-user", TenantRole.TenantReader)],
            nextCursor: null,
            hasMore: false,
            eTag: "members-page-2",
            projectionVersion: "projection-v1",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current));
        await firstClick;

        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-member-table']")
            .TextContent.ShouldContain("page-2-user"));
        memberRequests.Select(static request => request.Cursor).ShouldBe([null, "cursor-page-2"]);
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
        Services.AddSingleton<ITenantQueryGateway>(Substitute.For<ITenantQueryGateway>());
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

    private void RegisterServices(
        Func<NSubstitute.Core.CallInfo, Task<TenantDetailSnapshot>> detailFactory,
        ITenantsBffComposition? bffComposition = null)
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
        Services.AddSingleton(bffComposition ?? new StubTenantsBffComposition());
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

    /// <summary>
    /// Returns the element-reference id passed to each focus interop call, in call order.
    /// </summary>
    /// <remarks>
    /// <c>ElementReference.FocusAsync</c> routes every element through one interop identifier, so counting
    /// calls proves only that <em>something</em> was focused. The id identifies which element it was.
    /// </remarks>
    private IReadOnlyList<string> FocusedElementIds()
        => [.. JSInterop.Invocations
            .Where(invocation => invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase))
            .Select(invocation => invocation.Arguments.Count > 0 && invocation.Arguments[0] is ElementReference reference
                ? reference.Id
                : string.Empty)];

    /// <summary>
    /// Reads an <see cref="ElementReference"/> a component captured with <c>@ref</c>.
    /// </summary>
    /// <remarks>
    /// Read-only, and deliberately narrow. bUnit renders the <c>blazor:elementreference</c> marker without
    /// its id, so a focus target cannot be correlated to a DOM element through markup and there is no public
    /// surface exposing it. The alternative -- asserting only that some focus call happened -- cannot
    /// distinguish the two focus paths this component has, which is the whole point of the assertion.
    /// </remarks>
    private static string CapturedElementReferenceId(object component, string fieldName)
    {
        object value = component.GetType()
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(component)
            ?? throw new InvalidOperationException(
                $"'{fieldName}' is not a field of {component.GetType().Name}; the focus assertion cannot "
                + "identify its target and would otherwise silently weaken to a call count.");
        return ((ElementReference)value).Id;
    }

    /// <summary>Reads the captured launch-control reference for one configuration row.</summary>
    private static string CapturedLaunchElementReferenceId(object component, string key)
    {
        object value = component.GetType()
            .GetField("_removeLaunchElements", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(component)
            ?? throw new InvalidOperationException("'_removeLaunchElements' is not a field of the component.");
        Dictionary<string, ElementReference> elements = (Dictionary<string, ElementReference>)value;
        elements.TryGetValue(key, out ElementReference reference).ShouldBeTrue(
            $"No launch control was captured for '{key}', so focus cannot have returned to it.");
        return reference.Id;
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + timeout;
        while (!condition())
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException("The awaited condition was not met within the timeout.");
            }

            await Task.Delay(10);
        }
    }

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
        ProjectionLifecycleState lifecycle = ProjectionLifecycleState.Current,
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

    private sealed class TrackingMembershipCommandGateway(
        Task<TenantCommandSubmissionResult> addMemberSubmission,
        TenantCommandStatusResult? status = null) : ITenantCommandGateway
    {
        public int AddMemberCallCount { get; private set; }

        public int StatusCallCount { get; private set; }

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenant request, string? messageId = null, CancellationToken cancellationToken = default)
        {
            AddMemberCallCount++;
            return addMemberSubmission;
        }

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRole request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(SetTenantConfiguration request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> RemoveTenantConfigurationAsync(RemoveTenantConfiguration request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandStatusResult> GetStatusAsync(TenantCommandTrackingHandle handle, CancellationToken cancellationToken = default)
        {
            StatusCallCount++;
            return Task.FromResult(status ?? TenantCommandStatusResult.Unknown("Command status is unavailable."));
        }
    }

    private sealed class StubTenantCommandGateway : ITenantCommandGateway
    {
        public TenantCommandSubmissionResult AddMemberSubmission { get; init; }
            = TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable.");

        public TenantCommandSubmissionResult SetConfigurationSubmission { get; init; }
            = TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable.");

        public TenantCommandStatusResult Status { get; init; }
            = TenantCommandStatusResult.Unknown("Tenant command status is unavailable.");

        public int SetConfigurationCallCount { get; private set; }

        public SetTenantConfiguration? LastSetConfigurationRequest { get; private set; }

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(AddMemberSubmission);

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRole request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(SetTenantConfiguration request, CancellationToken cancellationToken = default)
        {
            SetConfigurationCallCount++;
            LastSetConfigurationRequest = request;
            return Task.FromResult(SetConfigurationSubmission);
        }

        public Task<TenantCommandSubmissionResult> RemoveTenantConfigurationAsync(RemoveTenantConfiguration request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandStatusResult> GetStatusAsync(TenantCommandTrackingHandle handle, CancellationToken cancellationToken = default)
            => Task.FromResult(Status);
    }

    private sealed class StubTenantsBffComposition(
        TenantLifecycleAuthorizationReflectionState lifecycleReflection
            = TenantLifecycleAuthorizationReflectionState.Indeterminate) : ITenantsBffComposition
    {
        private TenantLifecycleAuthorizationReflectionState _reflection = lifecycleReflection;

        public bool IsReadSurfaceConnected => true;

        public bool IsCommandSurfaceConnected => true;

        public int ReauthorizeConfigurationManagementCallCount { get; private set; }

        public Func<string, TenantStatus, TenantConfigurationSafeModel, TenantConfigurationManagementContext>?
            ReauthorizeConfigurationManagement { get; set; }

        /// <summary>Arms a one-shot suspension of the next lifecycle authorization resolve.</summary>
        public TaskCompletionSource? ResolutionGate { get; set; }

        /// <summary>Completes when a gated lifecycle authorization resolve has entered and suspended.</summary>
        public TaskCompletionSource ResolutionEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Counts async lifecycle authorization resolutions.</summary>
        public int AsyncResolutionCount { get; private set; }

        public TenantLifecycleAuthorizationReflectionState Reflection
        {
            get => _reflection;
            set => _reflection = value;
        }

        public async ValueTask<TenantLifecycleAuthorizationReflectionState> ResolveLifecycleAuthorizationAsync(
            CancellationToken cancellationToken = default)
        {
            AsyncResolutionCount++;
            TenantLifecycleAuthorizationReflectionState answer = Reflection;
            TaskCompletionSource? gate = ResolutionGate;
            if (gate is not null)
            {
                ResolutionGate = null;
                _ = ResolutionEntered.TrySetResult();
                await gate.Task.ConfigureAwait(false);
            }

            return answer;
        }

        public ValueTask<TenantConfigurationManagementContext> ReauthorizeConfigurationManagementAsync(
            string tenantId,
            TenantStatus tenantStatus,
            TenantConfigurationSafeModel safeModel,
            CancellationToken cancellationToken = default)
        {
            ReauthorizeConfigurationManagementCallCount++;
            if (ReauthorizeConfigurationManagement is not null)
            {
                return ValueTask.FromResult(ReauthorizeConfigurationManagement(tenantId, tenantStatus, safeModel));
            }

            return ValueTask.FromResult(TenantConfigurationManagementContext.Unavailable(tenantId, tenantStatus));
        }
    }

    private sealed class MutableAuthenticationStateProvider : AuthenticationStateProvider
    {
        private AuthenticationState _state = new(AdministratorPrincipal());

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(_state);

        public void Notify(ClaimsPrincipal principal)
        {
            _state = new AuthenticationState(principal);
            NotifyAuthenticationStateChanged(Task.FromResult(_state));
        }
    }

    private static ClaimsPrincipal AdministratorPrincipal()
        => new(new ClaimsIdentity(
        [
            new Claim("sub", "operator.alpha"),
            new Claim("eventstore:tenant", "system"),
            new Claim("global_admin", "true"),
        ], "test"));

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        // Unknown keys used to echo back as `name`, which made a missing resource indistinguishable from a
        // present one: tests rendered the literal key as user-visible copy and asserted substrings of it
        // (`"Unknown"` is a substring of `"Tenants.ProjectionLifecycle.Unknown"`), so the assertion passed
        // whether or not the string existed. Three keys added in this range were absent from Values for
        // exactly that reason and nothing failed.
        //
        // Resolution order is now: explicit override, then the real TenantsResources.resx, then throw. The
        // overrides keep the short, stable copy these tests assert on; falling through to the real resource
        // means a test can assert shipped copy without hand-copying it here; and a key that exists in
        // neither is a defect, not a silent echo.
        private static readonly ResourceManager RealResources = new(
            "Hexalith.Tenants.UI.Resources.TenantsResources",
            typeof(TenantsResources).Assembly);

        public LocalizedString this[string name] => new(name, Resolve(name));

        public LocalizedString this[string name, params object[] arguments]
            // No arguments means no substitution. Formatting unconditionally threw FormatException the
            // moment Resolve started falling through to a real .resx string containing `{0}` -- the stub
            // used to echo the placeholder-free key back, so the path could not be reached before.
            => new(name, arguments.Length == 0
                ? Resolve(name)
                : string.Format(CultureInfo.CurrentCulture, Resolve(name), arguments));

        // CurrentUICulture, not InvariantCulture. Pinning the invariant culture made this stub answer in
        // English no matter what culture a test rendered under, so a component that had hard-coded English
        // copy was indistinguishable from one that reads the localizer -- and no test could prove the French
        // resources are ever reached.
        private static string Resolve(string name)
            => Values.TryGetValue(name, out string? value)
                ? value
                : RealResources.GetString(name, CultureInfo.CurrentUICulture)
                    ?? throw new KeyNotFoundException(
                        $"Resource key '{name}' is defined neither in this stub nor in TenantsResources.resx. "
                        + "The stub must not echo an undefined key back as user-visible copy.");

        /// <summary>
        /// Enumerates everything <see cref="Resolve"/> can return, not just the overrides. Returning
        /// <c>Values</c> alone made enumeration and lookup disagree: a key resolvable through the real
        /// resource set was absent from the enumeration, so any caller reasoning about "the available
        /// strings" saw a set the indexer did not agree with. Overrides win, matching resolution order.
        /// </summary>
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            Dictionary<string, string> all = new(StringComparer.Ordinal);
            // NOT disposed: GetResourceSet returns the ResourceManager's own cached set, so disposing it
            // corrupts every subsequent lookup for the whole process -- Resolve then throws its
            // KeyNotFoundException for keys that do exist, and every bUnit WaitForAssertion downstream burns
            // its full timeout instead of failing.
            ResourceSet? real = RealResources.GetResourceSet(
                CultureInfo.CurrentUICulture,
                createIfNotExists: true,
                tryParents: includeParentCultures);
            if (real is not null)
            {
                foreach (DictionaryEntry entry in real)
                {
                    if (entry.Key is string key && entry.Value is string text)
                    {
                        all[key] = text;
                    }
                }
            }

            foreach (KeyValuePair<string, string> over in Values)
            {
                all[over.Key] = over.Value;
            }

            return all.Select(entry => new LocalizedString(entry.Key, entry.Value));
        }

        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            // Without these, ProjectionLifecycleBadge rendered the literal resource key back through this
            // echoing stub, so the badge's label was never really asserted and only its CSS class was --
            // which project rules forbid relying on alone.
            ["Tenants.ProjectionLifecycle.Label"] = "Projection lifecycle",
            ["Tenants.ProjectionLifecycle.Current"] = "Current",
            ["Tenants.ProjectionLifecycle.Stale"] = "Stale",
            ["Tenants.ProjectionLifecycle.Unknown"] = "Unknown",
            ["Tenants.ProjectionLifecycle.Rebuilding"] = "Rebuilding",
            ["Tenants.ProjectionLifecycle.Degraded"] = "Degraded",
            ["Tenants.ProjectionLifecycle.Unavailable"] = "Unavailable",
            ["Tenants.ProjectionLifecycle.LocalOnly"] = "Local only",
            ["Tenants.Detail.Back"] = "Back to tenants",
            ["Tenants.Detail.Configuration.Empty"] = "No visible configuration is available in this detail projection.",
            ["Tenants.Detail.Configuration.Unavailable"] = "Configuration unavailable",
            ["Tenants.Detail.Configuration.Summary"] = "{0} visible configuration keys across {1} namespaces.",
            ["Tenants.Detail.Configuration.Title"] = "Configuration summary",
            ["Tenants.Detail.CreatedAtLabel"] = "Created",
            ["Tenants.Detail.FreshnessLabel"] = "Freshness",
            ["Tenants.Detail.FullTenantIdLabel"] = "Full tenant identifier {0}",
            ["Tenants.Detail.IdentityLabel"] = "Tenant identity",
            ["Tenants.Detail.LifecycleLabel"] = "Lifecycle",
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
            ["Tenants.Configuration.Header.Key"] = "Key",
            ["Tenants.Configuration.Header.Namespace"] = "Namespace",
            ["Tenants.Configuration.Header.Value"] = "Value",
            ["Tenants.Configuration.Header.Actions"] = "Actions",
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
            ["Tenants.Configuration.Management.Title"] = "Configuration management",
            ["Tenants.Configuration.Management.Description"] = "Set configuration within current authorized prefixes or remove a current safe target.",
            ["Tenants.Configuration.Management.Unavailable.Policy"] = "Configuration management is unavailable because current authorization policy cannot be verified.",
            ["Tenants.Configuration.Management.Unavailable.NoScope"] = "Configuration management is unavailable because no configuration namespace is granted to you for this tenant.",
            ["Tenants.Configuration.Management.Unavailable.ProjectionState"] = "Refresh available tenant detail before managing configuration.",
            ["Tenants.Configuration.Management.Unavailable.ProjectionLifecycle"] = "Configuration management requires a current, projection-confirmed lifecycle.",
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
            ["Tenants.AddMember.Confirm.UnableToVerify.MissingProvenance"] = "Member projection already matched without provenance that this attempt advanced it. Refresh status or continue read-only.",
            ["Tenants.AddMember.Action.ContinueReadOnly"] = "Continue read-only",
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
            ["Tenants.ChangeRole.Confirm.AlreadyApplied.PreExisting"] = "The requested role was already applied before this attempt; no new role change is asserted.",
            ["Tenants.ChangeRole.Confirm.UnableToVerify.MissingBaseline"] = "Role projection matched without a pre-submit baseline, so this attempt cannot be confirmed.",
            ["Tenants.ChangeRole.Confirm.UnableToVerify.MissingTarget"] = "The member projection no longer contains the target user.",
            ["Tenants.ChangeRole.Action.ContinueReadOnly"] = "Continue read-only",
            ["Tenants.Members.Action.AddMember"] = "Add member",
            ["Tenants.Members.Action.ChangeRole"] = "Change role",
            ["Tenants.Members.Action.RemoveMember"] = "Remove member",
            ["Tenants.Members.Action.Unavailable"] = "Unavailable",
            ["Tenants.RemoveMember.Title"] = "Remove tenant member",
            ["Tenants.RemoveMember.Description"] = "Preview removal of user {1} from tenant {0}. Current confirmed role is {2}.",
            ["Tenants.RemoveMember.Preview.Title"] = "Consequence preview",
            ["Tenants.RemoveMember.Preview.Tenant"] = "Tenant",
            ["Tenants.RemoveMember.Preview.TargetUser"] = "Target user",
            ["Tenants.RemoveMember.Preview.CurrentRole"] = "Current role",
            ["Tenants.RemoveMember.Preview.OwnerCount"] = "Owner count",
            ["Tenants.RemoveMember.Preview.AccessPath"] = "Affected access path",
            ["Tenants.RemoveMember.Preview.AccessPath.Value"] = "Tenant membership for the visible tenant only.",
            ["Tenants.RemoveMember.Preview.Freshness"] = "Freshness",
            ["Tenants.RemoveMember.Preview.RecoveryPath"] = "Recovery path",
            ["Tenants.RemoveMember.Preview.RecoveryPath.Value"] = "Wait, refresh, inspect audit when available, or submit a forward correction to restore intended access.",
            ["Tenants.RemoveMember.Preview.AuditExpectation"] = "Audit expectation",
            ["Tenants.RemoveMember.Preview.AuditExpectation.Value"] = "Audit evidence is pending or unavailable until the Epic 5 evidence source exists.",
            ["Tenants.RemoveMember.Preview.PlatformStanding"] = "Platform standing",
            ["Tenants.RemoveMember.Preview.PlatformStanding.Known"] = "Also a global administrator. Tenant membership removal does not change platform administrator authority.",
            ["Tenants.RemoveMember.Preview.PlatformStanding.NotReflected"] = "Not reflected as a global administrator in the current complete projection.",
            ["Tenants.RemoveMember.Preview.PlatformStanding.Unknown"] = "Global-administrator standing is unproven in this view and is not guessed.",
            ["Tenants.RemoveMember.Preview.ConsequencesVersusUnknowns"] = "Known consequences versus unknowns",
            ["Tenants.RemoveMember.Preview.ConsequencesVersusUnknowns.Value"] = "Known consequence: tenant membership is removed only after projection confirmation proves the target user is absent. Known unknowns: session revocation, downstream enforcement, and token invalidation are not proven by this flow.",
            ["Tenants.RemoveMember.Preview.Blocked.Required"] = "Consequence preview is incomplete. Refresh current tenant detail before confirming removal.",
            ["Tenants.RemoveMember.Freshness.Current"] = "Current",
            ["Tenants.RemoveMember.OwnerContext.MultipleOwners"] = "{0} visible owners.",
            ["Tenants.RemoveMember.OwnerContext.LastOwner"] = "{0} visible owner; removing this member can leave zero visible owners.",
            ["Tenants.RemoveMember.OwnerContext.NoOwners"] = "0 visible owners; owner context is unavailable.",
            ["Tenants.RemoveMember.OwnerRisk.LastOwner"] = "Warning: {0} visible owner remains. Last-owner tenant membership removal is allowed, but it needs deliberate confirmation.",
            ["Tenants.RemoveMember.OwnerRisk.Accessible"] = "Elevated last-owner removal warning for {0} visible owner.",
            ["Tenants.RemoveMember.GlobalAdminRisk.Known"] = "Platform administrator authority is reflected for this user. This flow removes tenant membership only and will not remove global-administrator authority.",
            ["Tenants.RemoveMember.GlobalAdminRisk.Accessible"] = "Platform authority risk context.",
            ["Tenants.RemoveMember.Confirmation.Label"] = "Type the target user id to confirm removal",
            ["Tenants.RemoveMember.Confirmation.Elevated.Label"] = "Elevated risk: type the target user id exactly to confirm removal",
            ["Tenants.RemoveMember.Confirmation.Help"] = "Type {0} exactly. Cancel or Escape closes without submitting.",
            ["Tenants.RemoveMember.Confirmation.Elevated.Help"] = "Elevated risk: type {0} exactly. Cancel or Escape closes without submitting.",
            ["Tenants.RemoveMember.Confirm"] = "Remove member",
            ["Tenants.RemoveMember.Refresh"] = "Refresh status",
            ["Tenants.RemoveMember.Cancel"] = "Cancel",
            ["Tenants.RemoveMember.Lifecycle.Title"] = "Remove member command lifecycle",
            ["Tenants.RemoveMember.Unavailable.Narrow"] = "Member removal is unavailable on narrow layouts because the complete preview, risk context, and lifecycle must remain visible together. Widen the viewport or continue read-only.",
            ["Tenants.RemoveMember.Unavailable.Freshness"] = "Refresh current tenant detail before removing a member.",
            ["Tenants.RemoveMember.Role.TenantOwner"] = "Tenant owner",
            ["Tenants.RemoveMember.Role.TenantContributor"] = "Tenant contributor",
            ["Tenants.RemoveMember.Role.TenantReader"] = "Tenant reader",
            ["Tenants.RemoveMember.Role.Unknown"] = "Unknown role",
            ["Tenants.RemoveMember.State.Idle"] = "No remove-member preview opened.",
            ["Tenants.RemoveMember.State.Previewed"] = "Consequence preview ready; no command has been submitted.",
            ["Tenants.RemoveMember.Recovery.Idle"] = "Open the preview when current projection evidence is available.",
            ["Tenants.RemoveMember.Recovery.Previewed"] = "Confirm deliberately, cancel, or continue read-only.",
            ["Tenants.RemoveMember.Audit.NotStarted"] = "Audit evidence not started.",
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
            ["Tenants.Members.State.Loading"] = "Loading visible members.",
            ["Tenants.Members.State.Unknown"] = "Member freshness cannot be established.",
            ["Tenants.Members.State.Unauthorized"] = "You are not authorized to view these members.",
            ["Tenants.Members.State.NotFound"] = "The tenant member read was not found.",
            ["Tenants.Members.State.Invalid"] = "The member page reference is no longer valid.",
            ["Tenants.Members.State.Error"] = "The member read failed. Retry, or refresh the tenant.",
            ["Tenants.Members.State.Unavailable"] = "Visible members cannot be loaded right now.",
            ["Tenants.Members.State.Title"] = "Member read unavailable",
            ["Tenants.Members.State.Loading.Title"] = "Loading members",
            ["Tenants.Members.State.Invalid.Title"] = "Member page reference expired",
            ["Tenants.Members.State.NotFound.Title"] = "Members not found",
            ["Tenants.Members.State.Unauthorized.Title"] = "Members not available to you",
            ["Tenants.Members.Recovery.PageRecovered"] = "The member list restarted at the first page because the previous page reference expired.",
            ["Tenants.Members.OwnerContext.Unavailable"] = "Owner context is unavailable until tenant detail and member evidence are current and version-consistent.",
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
            ["Tenants.List.Column.Freshness"] = "Freshness",
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

    [Theory]
    // Each row removes exactly one clause of MemberEvidenceIsVersionConsistent / ActionsAreEvidenceBacked.
    // Every one of these survived the whole suite before: the two summary assertions matched on a prefix
    // both variants share, and nothing pinned the lifecycle or blank-version clauses at all.
    [InlineData(ProjectionLifecycleState.Current, ProjectionLifecycleState.Current, "projection-v1", "projection-v1", true)]
    [InlineData(ProjectionLifecycleState.Unknown, ProjectionLifecycleState.Current, "projection-v1", "projection-v1", false)]
    [InlineData(ProjectionLifecycleState.Current, ProjectionLifecycleState.Unknown, "projection-v1", "projection-v1", false)]
    [InlineData(ProjectionLifecycleState.Current, ProjectionLifecycleState.Current, null, null, false)]
    [InlineData(ProjectionLifecycleState.Current, ProjectionLifecycleState.Current, "  ", "  ", false)]
    [InlineData(ProjectionLifecycleState.Current, ProjectionLifecycleState.Current, "projection-v1", "projection-v2", false)]
    public void Member_governance_claims_require_current_lifecycle_and_a_stated_matching_projection_version(
        ProjectionLifecycleState detailLifecycle,
        ProjectionLifecycleState memberLifecycle,
        string? detailVersion,
        string? memberVersion,
        bool expectOwnerContext)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        TenantDetail detail = Detail(
            "tenant.alpha",
            new Dictionary<string, string> { ["billing.mode"] = "trial" },
            TenantStatus.Active,
            [
                new TenantMember("owner-user", TenantRole.TenantOwner),
                new TenantMember("reader-user", TenantRole.TenantReader),
            ]);
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadyWithSafeConfiguration(detail, detailLifecycle, detailVersion)));
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantUsersSnapshot.Ready(
                "tenant.alpha",
                detail.Members,
                nextCursor: null,
                hasMore: false,
                eTag: "members-etag",
                projectionVersion: memberVersion,
                ReadModelFreshnessState.Current,
                memberLifecycle)));
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-detail-member-summary']");

        string summary = cut.Find("[data-testid='tenants-detail-member-summary']").TextContent;
        if (expectOwnerContext)
        {
            summary.ShouldContain("2 members visible on this page; authoritative tenant detail reports 1 owners.");
        }
        else
        {
            summary.ShouldContain("2 members visible on this page. Owner context is unavailable");
            summary.ShouldNotContain("reports 1 owners");
        }

        // The same evidence gate drives member action availability, so a short clause must also close the
        // change-role and remove launchers rather than only the summary wording.
        cut.FindAll("[data-testid='tenants-change-role-open']").Count.ShouldBe(expectOwnerContext ? 2 : 0);
    }

    [Fact]
    public void Member_page_with_unresolvable_freshness_is_applied_and_advances_paging()
    {
        // Unknown freshness is a successful read whose projection lifecycle could not be resolved. Excluding
        // it from IsUsableMemberPage sent the page down the degraded-retention path, which re-presented page
        // one's rows and left the cursor untouched -- so Next could never leave page one.
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        List<TenantUsersRequest> memberRequests = [];
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadyWithSafeConfiguration(
                Detail("tenant.alpha"),
                ProjectionLifecycleState.Current,
                "projection-v1")));
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                TenantUsersRequest request = call.Arg<TenantUsersRequest>()
                    ?? throw new InvalidOperationException("A tenant-users request is required.");
                memberRequests.Add(request);
                return memberRequests.Count == 1
                    ? Task.FromResult(TenantUsersSnapshot.Ready(
                        "tenant.alpha",
                        [new TenantMember("page-1-user", TenantRole.TenantOwner)],
                        nextCursor: "cursor-page-2",
                        hasMore: true,
                        eTag: "members-page-1",
                        projectionVersion: "projection-v1",
                        ReadModelFreshnessState.Current,
                        ProjectionLifecycleState.Current))
                    : Task.FromResult(TenantUsersSnapshot.Ready(
                        "tenant.alpha",
                        [new TenantMember("page-2-user", TenantRole.TenantReader)],
                        nextCursor: null,
                        hasMore: false,
                        eTag: "members-page-2",
                        projectionVersion: "projection-v1",
                        ReadModelFreshnessState.Unknown,
                        ProjectionLifecycleState.Unknown) with { RequestCursor = request.Cursor });
            });
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-member-next']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-member-table']")
            .TextContent.ShouldContain("page-2-user"));

        // The requested page rendered, so paging state committed: Previous is now available.
        cut.Find("[data-testid='tenants-member-previous']").HasAttribute("disabled").ShouldBeFalse();
        memberRequests.Select(static request => request.Cursor).ShouldBe([null, "cursor-page-2"]);
    }

    [Fact]
    public void Degraded_member_page_describing_another_cursor_is_not_committed_as_the_requested_page()
    {
        // The reject side of the Degraded clause in IsUsableMemberPage. A degraded snapshot retaining the
        // prior page's rows and RequestCursor must not advance _memberCursor to a page that never rendered.
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        List<TenantUsersRequest> memberRequests = [];
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadyWithSafeConfiguration(
                Detail("tenant.alpha"),
                ProjectionLifecycleState.Current,
                "projection-v1")));
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                TenantUsersRequest request = call.Arg<TenantUsersRequest>()
                    ?? throw new InvalidOperationException("A tenant-users request is required.");
                memberRequests.Add(request);
                return memberRequests.Count == 1
                    ? Task.FromResult(TenantUsersSnapshot.Ready(
                        "tenant.alpha",
                        [new TenantMember("page-1-user", TenantRole.TenantOwner)],
                        nextCursor: "cursor-page-2",
                        hasMore: true,
                        eTag: "members-page-1",
                        projectionVersion: "projection-v1",
                        ReadModelFreshnessState.Current,
                        ProjectionLifecycleState.Current) with { RequestCursor = null })
                    // Degraded retention of page one: rows and RequestCursor still describe the PRIOR page.
                    : Task.FromResult(TenantUsersSnapshot.Ready(
                        "tenant.alpha",
                        [new TenantMember("page-1-user", TenantRole.TenantOwner)],
                        nextCursor: "cursor-page-2",
                        hasMore: true,
                        eTag: "members-page-1",
                        projectionVersion: "projection-v1",
                        ReadModelFreshnessState.Current,
                        ProjectionLifecycleState.Current) with
                    {
                        Kind = TenantUsersSurfaceKind.Degraded,
                        RequestCursor = null,
                        Reason = TenantUsersReason.GatewayUnavailable,
                    });
            });
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-member-next']").Click();

        cut.WaitForAssertion(() => memberRequests.Count.ShouldBe(2));

        // Paging state must not have advanced: Previous stays unavailable because page one is still current.
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='tenants-member-previous']")
            .All(static element => element.HasAttribute("disabled")).ShouldBeTrue());
    }

    [Fact]
    public void Member_next_is_a_no_op_when_has_more_carries_no_continuation_cursor()
    {
        // Spec rule: reject HasMore without a usable continuation cursor. No fixture in the suite paired
        // HasMore = true with a blank cursor, so the guard was never exercised.
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        List<TenantUsersRequest> memberRequests = [];
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadyWithSafeConfiguration(
                Detail("tenant.alpha"),
                ProjectionLifecycleState.Current,
                "projection-v1")));
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                memberRequests.Add(call.Arg<TenantUsersRequest>()!);
                return Task.FromResult(TenantUsersSnapshot.Ready(
                    "tenant.alpha",
                    [new TenantMember("page-1-user", TenantRole.TenantOwner)],
                    nextCursor: "   ",
                    hasMore: true,
                    eTag: "members-page-1",
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
        cut.WaitForElement("[data-testid='tenants-member-next']").Click();

        // One read only: the initial load. The click cannot start a page read without a usable cursor.
        memberRequests.Count.ShouldBe(1);
    }

    [Fact]
    public void Authorization_safe_empty_member_page_keeps_absence_wording_at_stale_freshness()
    {
        // A successful authorized-empty page at non-Current freshness maps onto the Stale/Unknown kind,
        // which used to select the "Member read unavailable" failure copy -- reporting a read that
        // succeeded and authoritatively returned nothing as a failure.
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadyWithSafeConfiguration(
                Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }, TenantStatus.Active, []),
                ProjectionLifecycleState.Current,
                "projection-v1")));
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantUsersSnapshot.Empty(
                "tenant.alpha",
                isAuthorizationScoped: true,
                eTag: "members-etag",
                projectionVersion: "projection-v1",
                ReadModelFreshnessState.Stale,
                ProjectionLifecycleState.Stale)));
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));

        IElement empty = cut.WaitForElement("[data-testid='tenants-member-empty']");
        empty.TextContent.ShouldContain("No visible members");

        // The freshness channel is not lost to the absence channel: both are stated.
        empty.TextContent.ShouldContain("stale");
        cut.FindAll("[data-testid='tenants-member-stale']").ShouldBeEmpty();
    }

    [Fact]
    public void Member_command_flow_survives_its_target_row_leaving_the_page_on_the_same_cursor()
    {
        // The confirmation re-query for a successful removal re-reads the SAME cursor and the target row is
        // gone. Resolving the open flow against Members.Rows unmounted it at exactly that moment, throwing
        // away the receipt and pending projection confirmation -- while the active user id survived, so
        // HasOpenMemberCommandFlow stayed true and BOTH pager buttons were disabled with no rendered flow
        // to close and no reset control. Only a full page reload recovered.
        RegisterComponentServices();
        TenantDetail detail = Detail("tenant.alpha");
        TenantUsersSnapshot page = TenantUsersSnapshot.Ready(
            detail.TenantId,
            detail.Members,
            nextCursor: "cursor-page-2",
            hasMore: true,
            eTag: "members-etag",
            projectionVersion: "v1",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current);
        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, page));

        cut.FindAll("[data-testid='tenants-remove-member-open']")[0].Click();
        cut.Find("[data-testid='tenants-remove-member-flow']");

        // Same tenant, same cursor, target row removed: the removal succeeded.
        TenantUsersSnapshot afterRemoval = page with
        {
            Rows = [.. detail.Members.Where(static member => member.UserId != "owner-user")],
        };
        cut.Render(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, afterRemoval));

        // The flow keeps rendering from its captured intent, so the receipt survives...
        cut.Find("[data-testid='tenants-remove-member-flow']");
        cut.FindAll("[data-testid='tenants-member-row']").Count.ShouldBe(1);

        // ...and closing it -- which is only possible because it is still rendered -- releases the pager.
        cut.Find("[data-testid='tenants-remove-member-cancel']").Click();
        cut.FindAll("[data-testid='tenants-remove-member-flow']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-member-next']").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void Member_command_flow_survives_a_refresh_that_empties_the_page_entirely()
    {
        // The flow regions render outside the empty/non-empty branch for the same reason the pager does.
        RegisterComponentServices();
        TenantDetail detail = Detail("tenant.alpha");
        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, MemberSnapshot(detail)));
        cut.FindAll("[data-testid='tenants-change-role-open']")[0].Click();
        cut.Find("[data-testid='tenants-change-role-flow']");

        cut.Render(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, MemberSnapshot(detail) with { Rows = [] }));

        cut.Find("[data-testid='tenants-change-role-flow']");
    }

    [Fact]
    public async Task Member_paging_handlers_reject_a_click_dispatched_before_the_disabled_attribute_renders()
    {
        // The Disabled bindings are observed by two existing tests, but the handler guards exist for the
        // click dispatched BEFORE the re-render lands. Invoking the callbacks directly is the only way to
        // observe them; removing any of the three guards previously kept the whole suite green.
        RegisterComponentServices();
        TenantDetail detail = Detail("tenant.alpha");
        int nextRequests = 0;
        int previousRequests = 0;
        TenantUsersSnapshot refreshing = TenantUsersSnapshot.Ready(
            detail.TenantId,
            detail.Members,
            nextCursor: "cursor-page-2",
            hasMore: true,
            eTag: "members-etag",
            projectionVersion: "v1",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current) with { IsRefreshing = true };

        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.CanGoPrevious, true)
            .Add(view => view.Members, refreshing)
            .Add(view => view.OnNextPageRequested, EventCallback.Factory.Create(this, () => nextRequests++))
            .Add(view => view.OnPreviousPageRequested, EventCallback.Factory.Create(this, () => previousRequests++)));

        // A load is in flight (IsRefreshing): neither handler may start a second page read.
        await cut.InvokeAsync(() => cut.Instance.RequestNextPageAsync());
        await cut.InvokeAsync(() => cut.Instance.RequestPreviousPageAsync());
        nextRequests.ShouldBe(0);
        previousRequests.ShouldBe(0);

        // Not refreshing, but a member command flow is open: still blocked, because paging unmounts it.
        cut.Render(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.CanGoPrevious, true)
            .Add(view => view.Members, refreshing with { IsRefreshing = false })
            .Add(view => view.OnNextPageRequested, EventCallback.Factory.Create(this, () => nextRequests++))
            .Add(view => view.OnPreviousPageRequested, EventCallback.Factory.Create(this, () => previousRequests++)));
        cut.FindAll("[data-testid='tenants-change-role-open']")[0].Click();

        await cut.InvokeAsync(() => cut.Instance.RequestNextPageAsync());
        await cut.InvokeAsync(() => cut.Instance.RequestPreviousPageAsync());
        nextRequests.ShouldBe(0);
        previousRequests.ShouldBe(0);

        // Nothing in flight and no open flow: both handlers proceed.
        cut.Find("[data-testid='tenants-change-role-cancel']").Click();
        await cut.InvokeAsync(() => cut.Instance.RequestNextPageAsync());
        await cut.InvokeAsync(() => cut.Instance.RequestPreviousPageAsync());
        nextRequests.ShouldBe(1);
        previousRequests.ShouldBe(1);

        // Finally the two boundary guards themselves. Every phase above holds HasMore and CanGoPrevious
        // true, so `Members.HasMore &&` and `CanGoPrevious &&` could both be deleted with the suite green --
        // a last page would then re-request a dead cursor and page one would walk off the front.
        cut.Render(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.CanGoPrevious, false)
            .Add(view => view.Members, MemberSnapshot(detail))
            .Add(view => view.OnNextPageRequested, EventCallback.Factory.Create(this, () => nextRequests++))
            .Add(view => view.OnPreviousPageRequested, EventCallback.Factory.Create(this, () => previousRequests++)));

        await cut.InvokeAsync(() => cut.Instance.RequestNextPageAsync());
        await cut.InvokeAsync(() => cut.Instance.RequestPreviousPageAsync());
        nextRequests.ShouldBe(1);
        previousRequests.ShouldBe(1);
    }

    /// <summary>
    /// A refresh arriving during a member page load must be routed through the read refresh, not silently
    /// abort it.
    /// </summary>
    /// <remarks>
    /// <c>BeginLoad</c> cancels the shared load token and clears the member in-flight flag, so a member page
    /// navigation that was in flight is aborted. The detail refresh never re-reads members, so the abort was
    /// silent: the refresh indicator vanished, the pager re-enabled (its Disabled binding reads
    /// <c>Members.IsRefreshing</c>, not the in-flight flag), and the table sat on the old page with no error
    /// and no retry, while the superseded load's <c>finally</c> could restore nothing because its generation
    /// no longer matched. Deleting the reroute survived the suite.
    /// </remarks>
    [Fact]
    public async Task A_refresh_during_a_member_page_load_reissues_the_member_read()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        List<TenantUsersRequest> memberRequests = [];
        var pendingPage = new TaskCompletionSource<TenantUsersSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadyWithSafeConfiguration(
                Detail("tenant.alpha"),
                ProjectionLifecycleState.Current,
                "projection-v1")));
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                TenantUsersRequest request = call.Arg<TenantUsersRequest>()!;
                memberRequests.Add(request);
                return memberRequests.Count switch
                {
                    1 => Task.FromResult(TenantUsersSnapshot.Ready(
                        "tenant.alpha",
                        [new TenantMember("page-1-user", TenantRole.TenantOwner)],
                        "page-2",
                        true,
                        "members-1",
                        "projection-v1",
                        ReadModelFreshnessState.Current,
                        ProjectionLifecycleState.Current)),
                    2 => pendingPage.Task,
                    _ => Task.FromResult(TenantUsersSnapshot.Ready(
                        "tenant.alpha",
                        [new TenantMember("refreshed-user", TenantRole.TenantOwner)],
                        null,
                        false,
                        "members-refreshed",
                        "projection-v1",
                        ReadModelFreshnessState.Current,
                        ProjectionLifecycleState.Current)),
                };
            });
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        IRenderedComponent<MemberAccessReview> members = cut.FindComponent<MemberAccessReview>();

        Task nextPage = cut.InvokeAsync(() => members.Instance.RequestNextPageAsync());
        cut.WaitForAssertion(() => memberRequests.Count.ShouldBe(2));

        // A detail refresh lands while the member page read is still open.
        // Raised through the metadata flow, whose callback is RefreshTenantDetailAsync -- the detail-only
        // path. That is the one that has to notice a member page load is open and reroute; the member
        // component's own callback already goes to the combined read.
        await cut.InvokeAsync(() => cut.FindComponent<EditTenantMetadataFlow>().Instance
            .OnProjectionRefreshRequested.InvokeAsync());

        // The member read must be reissued, not silently dropped.
        cut.WaitForAssertion(() => memberRequests.Count.ShouldBeGreaterThanOrEqualTo(3));
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-member-table']")
            .TextContent.ShouldContain("refreshed-user"));

        pendingPage.SetResult(TenantUsersSnapshot.Unavailable("tenant.alpha"));
        await nextPage;
    }

    /// <summary>
    /// An older detail load completing after a newer one must not overwrite the newer snapshot.
    /// </summary>
    /// <remarks>
    /// The generation check and the <c>_snapshot</c> assignment are marshalled together for exactly this
    /// case. Testing and assigning on the thread-pool continuation let a newer read that completed in
    /// between lose to an older one. The thread-affinity half is not observable under bUnit's
    /// single-threaded renderer; what is observable, and what this drives, is the interleaved completion:
    /// two overlapping loads whose results arrive in the opposite order to the one they were started in.
    /// </remarks>
    [Fact]
    public async Task An_older_detail_load_completing_last_does_not_overwrite_the_newer_snapshot()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        var firstLoad = new TaskCompletionSource<TenantDetailSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondLoad = new TaskCompletionSource<TenantDetailSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        int detailReads = 0;
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Interlocked.Increment(ref detailReads) switch
            {
                1 => Task.FromResult(ReadyWithSafeConfiguration(
                    Detail("tenant.alpha"),
                    ProjectionLifecycleState.Current,
                    "projection-v1")),
                2 => firstLoad.Task,
                _ => secondLoad.Task,
            });
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(MemberSnapshot(Detail(call.Arg<TenantUsersRequest>()!.TenantId))));
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-detail-identity']");

        EventCallback refresh = cut.FindComponent<EditTenantMetadataFlow>().Instance.OnProjectionRefreshRequested;
        Task older = cut.InvokeAsync(() => refresh.InvokeAsync());
        cut.WaitForAssertion(() => detailReads.ShouldBe(2));

        Task newer = cut.InvokeAsync(() => refresh.InvokeAsync());
        cut.WaitForAssertion(() => detailReads.ShouldBe(3));

        // The NEWER read lands first...
        secondLoad.SetResult(ReadyWithSafeConfiguration(
            Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "newer" }),
            ProjectionLifecycleState.Current,
            "projection-v3"));
        await newer;
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-config-read-table']")
            .TextContent.ShouldContain("newer"));

        // ...and the superseded one lands afterwards. It must be discarded, not applied.
        firstLoad.SetResult(ReadyWithSafeConfiguration(
            Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "superseded" }),
            ProjectionLifecycleState.Current,
            "projection-v2"));
        await older;

        cut.Find("[data-testid='tenants-config-read-table']").TextContent.ShouldContain("newer");
        cut.Find("[data-testid='tenants-config-read-table']").TextContent.ShouldNotContain("superseded");
    }

    /// <summary>
    /// Optional notification setup must stop retrying a backend that keeps failing.
    /// </summary>
    /// <remarks>
    /// <c>OnAfterRenderAsync</c> retries whenever no lease was recorded, and a faulting subscribe records
    /// neither <c>_readRefreshLease</c> nor <c>_subscriptionTenantId</c>, so every guard passed again on the
    /// next render -- two remote round trips per render, unbounded, for the life of the circuit. A new route
    /// still gets its own attempts.
    /// </remarks>
    [Fact]
    public void Read_refresh_setup_retries_are_bounded_per_route()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        IProjectionSubscription backendSubscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        backendSubscription
            .SubscribeAsync("tenants", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(new InvalidOperationException("subscription endpoint is down")));
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(ReadyWithSafeConfiguration(
                Detail(call.Arg<TenantDetailRequest>()!.TenantId),
                ProjectionLifecycleState.Current,
                "projection-v1")));
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(MemberSnapshot(Detail(call.Arg<TenantUsersRequest>()!.TenantId))));
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
        cut.WaitForElement("[data-testid='tenants-detail-identity']");

        for (int render = 0; render < 12; render++)
        {
            cut.Render();
        }

        int alphaAttempts = backendSubscription.ReceivedCalls()
            .Count(call => call.GetMethodInfo().Name == nameof(IProjectionSubscription.SubscribeAsync)
                && (string)call.GetArguments()[1]! == "tenant.alpha");
        // Exact, not a range. `<= 3` paired with `>= 1` cannot tell "bounded at three retries" from "never
        // retried at all", so reducing the budget to zero passed. Twelve renders follow the first failure,
        // so a correct budget spends exactly its three attempts and no more.
        alphaAttempts.ShouldBe(3, "A failing subscribe must not be retried on every render.");

        // A new route gets its own budget: the bound is per tenant, not per circuit. `>= 1` could not show
        // that -- a route change makes one subscribe attempt whatever the budget does -- so this asserts
        // beta genuinely re-spends a budget rather than inheriting alpha's exhausted one.
        cut.Render(parameters => parameters.Add(page => page.TenantId, "tenant.beta"));
        for (int render = 0; render < 12; render++)
        {
            cut.Render();
        }

        cut.WaitForAssertion(() => backendSubscription.ReceivedCalls()
            .Count(call => call.GetMethodInfo().Name == nameof(IProjectionSubscription.SubscribeAsync)
                && (string)call.GetArguments()[1]! == "tenant.beta")
            .ShouldBe(3));
    }

    [Fact]
    public async Task Same_route_member_refresh_reopens_the_notification_setup_budget()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        IProjectionSubscription backendSubscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        backendSubscription
            .SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(new InvalidOperationException("subscription endpoint is down")));
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadyWithSafeConfiguration(
                Detail("tenant.alpha"),
                ProjectionLifecycleState.Current,
                "projection-v1")));
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(MemberSnapshot(Detail("tenant.alpha"))));
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
        for (int render = 0; render < 12; render++)
        {
            cut.Render();
        }

        CountAttempts().ShouldBe(3);

        await cut.InvokeAsync(() => cut.FindComponent<MemberAccessReview>().Instance
            .OnProjectionRefreshRequested.InvokeAsync());
        for (int render = 0; render < 12; render++)
        {
            cut.Render();
        }

        cut.WaitForAssertion(() => CountAttempts().ShouldBe(6));

        int CountAttempts()
            => backendSubscription.ReceivedCalls()
                .Count(call => call.GetMethodInfo().Name == nameof(IProjectionSubscription.SubscribeAsync)
                    && (string)call.GetArguments()[1]! == "tenant.alpha");
    }

    /// <summary>
    /// A dispose racing a suspended subscribe continuation must not leave the lease attached.
    /// </summary>
    /// <remarks>
    /// The pre-assignment <c>_disposed</c> check is not enough: <c>DisposeAsync</c> sets <c>_disposed</c> and
    /// then reads a still-null <c>_readRefreshLease</c>, so a continuation that passed that check and was
    /// preempted assigned the lease afterwards — leaving the callback registered for the life of the circuit
    /// and invoking <c>InvokeAsync</c> on a disposed component.
    /// <para>
    /// Scope, stated honestly: this does NOT pin the post-assignment recheck. Gating inside
    /// <c>SubscribeAsync</c> means a dispose issued from a test always precedes the pre-assignment check, and
    /// the window between that check and the assignment is not reachable from outside the component. The
    /// theory drives both orderings that ARE reachable -- dispose before the subscribe completes, and dispose
    /// after the lease is fully attached -- and asserts the lease ends up released in each.
    /// </remarks>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task A_dispose_racing_the_subscribe_continuation_does_not_leave_the_lease_attached(
        bool disposeBeforeSubscribeCompletes)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        TaskCompletionSource subscribeGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        IProjectionSubscription backendSubscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();

        // Suspend inside SubscribeAsync so the dispose lands between the pre-assignment check and the
        // assignment itself — the exact window the recheck exists for.
        backendSubscription
            .SubscribeAsync("tenants", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => subscribeGate.Task);
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(ReadyWithSafeConfiguration(
                Detail(call.Arg<TenantDetailRequest>()!.TenantId),
                ProjectionLifecycleState.Current,
                "projection-v1")));
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(MemberSnapshot(Detail(call.Arg<TenantUsersRequest>()!.TenantId))));
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
        cut.WaitForElement("[data-testid='tenants-detail-identity']");
        cut.WaitForAssertion(() => backendSubscription.ReceivedCalls()
            .Count(call => call.GetMethodInfo().Name == nameof(IProjectionSubscription.SubscribeAsync))
            .ShouldBeGreaterThanOrEqualTo(1));

        // Both interleavings, run explicitly rather than hoped for. Gating inside SubscribeAsync means the
        // continuation cannot resume until the gate is released, so disposing first ALWAYS lands before the
        // pre-assignment check and the post-assignment recheck is never reached: the single-ordering version
        // of this test claimed to pin that recheck and could not, because nothing forces a dispose into the
        // window between the check and the assignment from outside the component. What is actually
        // guaranteed -- and is what matters to a user -- is that the lease ends up released under either
        // ordering, so both are now driven.
        if (disposeBeforeSubscribeCompletes)
        {
            // Dispose reaches the pre-assignment check first; the lease is never attached.
            await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());
            subscribeGate.SetResult();
        }
        else
        {
            // The lease is fully attached first, so disposal must detach it through the ordinary path.
            subscribeGate.SetResult();
            cut.WaitForAssertion(() => backendSubscription.ReceivedCalls()
                .Count(call => call.GetMethodInfo().Name == nameof(IProjectionSubscription.SubscribeAsync))
                .ShouldBeGreaterThanOrEqualTo(1));
            await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());
        }

        // Disposal runs through the renderer's dispatcher, as it does in production. Calling DisposeAsync
        // straight from the test thread bypassed the dispatcher entirely, so the path under test was not
        // running under the conditions it runs under for real.
        cut.WaitForAssertion(() => backendSubscription.ReceivedCalls()
            .Count(call => call.GetMethodInfo().Name == nameof(IProjectionSubscription.UnsubscribeAsync)
                && (string)call.GetArguments()[1]! == "tenant.alpha")
            .ShouldBe(1));
    }

    /// <summary>
    /// The qualified member absence copy must be one whole localized sentence per state.
    /// </summary>
    /// <remarks>
    /// The message was assembled at run time as
    /// <c>$"{Localizer["Tenants.Members.Empty.Message", …]} {StateMessage}"</c>, gluing two independently
    /// translated sentences with an ASCII space. Project rules require whole strings with placeholders: a
    /// translator cannot reorder, re-punctuate, or make the two halves agree, and no assertion could
    /// distinguish the composed form from a genuine resource.
    /// </remarks>
    [Theory]
    [InlineData(ReadModelFreshnessState.Stale, "Tenants.Members.Empty.Message.Stale")]
    [InlineData(ReadModelFreshnessState.Unknown, "Tenants.Members.Empty.Message.Unknown")]
    public void Qualified_member_absence_uses_one_whole_localized_string(
        ReadModelFreshnessState freshness,
        string expectedKey)
    {
        RegisterComponentServices();
        TenantDetail detail = Detail(
            "tenant.alpha",
            new Dictionary<string, string>(),
            TenantStatus.Active,
            []);
        TenantUsersSnapshot members = TenantUsersSnapshot.Empty(
            detail.TenantId,
            isAuthorizationScoped: true,
            eTag: "members-etag",
            projectionVersion: "v1",
            freshness,
            ProjectionLifecycleState.Unknown);

        var resources = new ResourceManager(
            "Hexalith.Tenants.UI.Resources.TenantsResources",
            typeof(TenantsResources).Assembly);
        string english = resources.GetString(expectedKey, CultureInfo.InvariantCulture)
            .ShouldNotBeNull();
        string french = resources.GetString(expectedKey, new CultureInfo("fr"))
            .ShouldNotBeNull();

        // Whole strings on both sides of the parity contract, with the one placeholder the reason label fills.
        english.ShouldContain("{0}");
        french.ShouldContain("{0}");
        french.ShouldNotBe(english);

        // Rendered under each culture in turn. Looking the resource up here and asserting the *English*
        // rendering contains it proved nothing about localization: a component that had hard-coded the
        // English literal produced byte-identical output and passed, and `french.ShouldNotBe(english)` only
        // showed the .fr.resx differs -- never that production reads it. Asserting the French rendering
        // carries French copy, and not the English copy, is what makes the localizer load-bearing.
        string renderedEnglish = RenderMemberEmptyText(CultureInfo.InvariantCulture);
        string renderedFrench = RenderMemberEmptyText(new CultureInfo("fr"));

        AssertCarriesWholeString(renderedEnglish, english);
        AssertCarriesWholeString(renderedFrench, french);
        renderedFrench.ShouldNotBe(renderedEnglish);
        renderedEnglish.ShouldNotContain(FirstSegment(french));

        string RenderMemberEmptyText(CultureInfo culture)
        {
            CultureInfo previousUi = CultureInfo.CurrentUICulture;
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentUICulture = culture;
                CultureInfo.CurrentCulture = culture;
                IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
                    .Add(view => view.Detail, detail)
                    .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
                    .Add(view => view.Freshness, ReadModelFreshnessState.Current)
                    .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
                    .Add(view => view.ProjectionVersion, "v1")
                    .Add(view => view.Members, members));
                return cut.Find("[data-testid='tenants-member-empty']").TextContent;
            }
            finally
            {
                CultureInfo.CurrentUICulture = previousUi;
                CultureInfo.CurrentCulture = previous;
            }
        }

        // Both segments must be non-empty, or the assertion silently half-disarms: `{0}` at the very start
        // or very end yields an empty slice, and ShouldContain("") always passes.
        static string FirstSegment(string resource)
            => resource[..resource.IndexOf("{0}", StringComparison.Ordinal)].Trim();

        static void AssertCarriesWholeString(string rendered, string resource)
        {
            string head = FirstSegment(resource);
            string tail = resource[(resource.IndexOf("{0}", StringComparison.Ordinal) + 3)..].Trim();
            head.ShouldNotBeNullOrWhiteSpace();
            tail.ShouldNotBeNullOrWhiteSpace();
            rendered.ShouldContain(head);
            rendered.ShouldContain(tail);
        }
    }

    /// <summary>
    /// A degraded member read over a previously authorization-scoped-empty page must not read as "No members".
    /// </summary>
    /// <remarks>
    /// <c>TenantUsersSnapshot.Degraded</c> retained the previous snapshot's <c>IsAuthorizationScopedEmpty</c>,
    /// and <c>IsAuthorizationSafeAbsence</c> short-circuits the whole state switch, so a read the gateway
    /// could not complete presented as a successful authorized absence -- the exact conflation AC6 forbids.
    /// </remarks>
    [Fact]
    public void Degraded_member_read_over_an_authorized_empty_page_is_not_an_authorization_safe_absence()
    {
        RegisterComponentServices();
        TenantDetail detail = Detail(
            "tenant.alpha",
            new Dictionary<string, string>(),
            TenantStatus.Active,
            []);
        TenantUsersSnapshot authorizedEmpty = TenantUsersSnapshot.Empty(
            detail.TenantId,
            isAuthorizationScoped: true,
            eTag: "members-etag",
            projectionVersion: "v1",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current);
        authorizedEmpty.IsAuthorizationScopedEmpty.ShouldBeTrue();

        TenantUsersSnapshot degraded = TenantUsersSnapshot.Degraded(
            detail.TenantId,
            authorizedEmpty,
            TenantUsersReason.GatewayUnavailable);
        degraded.IsAuthorizationScopedEmpty.ShouldBeFalse();

        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.Members, degraded));

        cut.Find("[data-testid='tenants-member-degraded']");
        cut.FindAll("[data-testid='tenants-member-empty']").ShouldBeEmpty();
    }

    /// <summary>
    /// A denial member refresh must release a captured command flow.
    /// </summary>
    /// <remarks>
    /// The flows fall back to their captured member once the row leaves the page, which is correct for a
    /// removal confirmation. On an Unauthorized, NotFound or Invalid refresh the replacement snapshot keeps
    /// <c>RequestCursor</c> null, so the cursor-change reset in <c>OnParametersSet</c> never fires: rows
    /// blanked to the denial state while the flow region kept rendering the captured member's identifier and
    /// role, and both pager buttons stayed disabled with no flow the operator could close.
    /// <para>
    /// The rows are what makes this correct rather than merely tidy: after a denial the caller may no longer
    /// see this membership at all, so continuing to render the captured member's identifier and role would
    /// leak what authorization-safe absence forbids. Review loop 12 cut Unavailable and Error out of this
    /// theory — see the outage sibling below for why they must do the opposite — and added Invalid, which
    /// production listed and no row exercised.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(TenantUsersSurfaceKind.Unauthorized, "tenants-member-unauthorized")]
    [InlineData(TenantUsersSurfaceKind.NotFound, "tenants-member-not-found")]
    [InlineData(TenantUsersSurfaceKind.Invalid, "tenants-member-invalid")]
    public void Terminal_member_refresh_releases_a_captured_command_flow(
        TenantUsersSurfaceKind kind,
        string expectedTestId)
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
        cut.FindAll("[data-testid='tenants-remove-member-open']")[0].Click();
        cut.Find("[data-testid='tenants-remove-member-flow']");

        cut.Render(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.CanGoPrevious, true)
            .Add(view => view.Members, TerminalMemberSnapshot(kind, detail.TenantId)));

        cut.Find($"[data-testid='{expectedTestId}']");
        cut.FindAll("[data-testid='tenants-remove-member-flow']").ShouldBeEmpty();

        // ...and the pager is released with it, rather than staying disabled behind a flow that is gone.
        cut.Find("[data-testid='tenants-member-previous']").HasAttribute("disabled").ShouldBeFalse();
    }

    /// <summary>
    /// An operational member-read failure must NOT tear down a captured command flow.
    /// </summary>
    /// <remarks>
    /// Unavailable and Error are outages, not denials: the caller's rights are unchanged and a command that
    /// was really submitted still owns a receipt to render. Folding them in with the denial kinds let one
    /// Tenants API blip destroy an in-flight member removal's Confirmed / Rejected / UnableToVerify state,
    /// its safe message and its audit entry point — and because no member flow implements disposal, the
    /// command-activity lease was never lowered either, so every command surface on the detail page stayed
    /// disabled for the life of the circuit.
    /// </remarks>
    [Theory]
    [InlineData(TenantUsersSurfaceKind.Unavailable)]
    [InlineData(TenantUsersSurfaceKind.Error)]
    public void An_operational_member_read_failure_keeps_a_captured_command_flow(TenantUsersSurfaceKind kind)
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
        cut.FindAll("[data-testid='tenants-remove-member-open']")[0].Click();
        cut.Find("[data-testid='tenants-remove-member-flow']");

        cut.Render(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, ReadModelFreshnessState.Current)
            .Add(view => view.Lifecycle, ProjectionLifecycleState.Current)
            .Add(view => view.ProjectionVersion, "v1")
            .Add(view => view.CanGoPrevious, true)
            .Add(view => view.Members, TerminalMemberSnapshot(kind, detail.TenantId)));

        cut.Find("[data-testid='tenants-remove-member-flow']");
    }

    /// <summary>
    /// The member pager announces a Previous step that lands on page one through a trimmed history.
    /// </summary>
    /// <remarks>
    /// <c>CursorHistory.Trim</c> re-appends the first-page sentinel beneath the newest entries, so one later
    /// Previous click walks the operator from mid-sequence straight to page one. The audit and
    /// global-administrator pagers announce that; this one discarded <c>Trim</c>'s return value entirely and
    /// rendered the jump as an ordinary step back.
    /// </remarks>
    [Fact]
    public async Task The_member_pager_announces_a_previous_step_that_jumps_to_the_first_page()
    {
        // Driven through TenantDetailPage's real pager, which is where the defect lived: the notice is
        // computed by the PARENT from `_memberPagingHistoryTruncated && _memberCursor is null &&
        // _memberCursorHistory.Count == 0`, and the remark names the defect as discarding `CursorHistory.Trim`'s
        // return value. Setting MemberAccessReview.PagingJumpedToFirstPage as a child parameter asserted only
        // that the child renders a notice when told to; the parent's computation had no test at all, so
        // hard-coding it to false, or deleting the Trim call, left the whole suite green. Both sibling pagers
        // drive the real control, and this now matches them.
        const int bound = CursorHistory.DefaultMaximum;
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadyWithSafeConfiguration(
                Detail("tenant.alpha"),
                ProjectionLifecycleState.Current,
                "projection-v1")));
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                TenantUsersRequest request = call.Arg<TenantUsersRequest>()!;
                int page = request.Cursor is null
                    ? 0
                    : int.Parse(request.Cursor["page-".Length..], CultureInfo.InvariantCulture);
                return Task.FromResult(TenantUsersSnapshot.Ready(
                    "tenant.alpha",
                    [new TenantMember($"user-page-{page}", TenantRole.TenantOwner)],
                    $"page-{page + 1}",
                    true,
                    $"members-{page}",
                    "projection-v1",
                    ReadModelFreshnessState.Current,
                    ProjectionLifecycleState.Current) with
                {
                    RequestCursor = request.Cursor,
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

        // One page past the bound, so the trim runs and drops the oldest non-sentinel entries.
        for (int page = 1; page <= bound + 1; page++)
        {
            cut.Find("[data-testid='tenants-member-next']").Click();
            cut.WaitForAssertion(() => cut.Markup.ShouldContain($"user-page-{page}"));
        }

        cut.FindAll("[data-testid='tenants-member-history-truncated']").ShouldBeEmpty(
            "Paging forward is not a jump; the notice belongs to the Previous click that lands on page one.");

        // Walk back. The retained history is the bound minus one entries plus the re-appended sentinel, so
        // the last of these pops the sentinel and lands on page one from the middle of the sequence.
        for (int step = 1; step < bound; step++)
        {
            cut.Find("[data-testid='tenants-member-previous']").Click();
            cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-member-previous']")
                .HasAttribute("disabled").ShouldBeFalse());
            cut.FindAll("[data-testid='tenants-member-history-truncated']").ShouldBeEmpty();
        }

        cut.Find("[data-testid='tenants-member-previous']").Click();
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='tenants-member-history-truncated']").ShouldNotBeEmpty());

        IElement notice = cut.Find("[data-testid='tenants-member-history-truncated']");
        notice.GetAttribute("role").ShouldBe("status");
        notice.GetAttribute("aria-live").ShouldBe("polite");

        // The sibling pagers assert the copy, not merely that some text is present: swapping the resource key
        // for an unrelated recovery string passed the previous `ShouldNotBeNullOrWhiteSpace` check.
        notice.TextContent.ShouldContain("first page", Case.Insensitive);
        cut.Find("[data-testid='tenants-member-previous']").HasAttribute("disabled").ShouldBeTrue();

        await cut.InvokeAsync(() => cut.FindComponent<MemberAccessReview>().Instance
            .OnProjectionRefreshRequested.InvokeAsync());
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='tenants-member-history-truncated']").ShouldBeEmpty(
            "the page-one jump notice is one-shot evidence and a later authoritative refresh clears it"));
    }
}
