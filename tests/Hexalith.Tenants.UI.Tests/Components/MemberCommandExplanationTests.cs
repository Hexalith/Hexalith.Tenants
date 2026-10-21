using System.Globalization;

using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants.Members;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantUsers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

/// <summary>Qualifies member command explanations without changing the existing admission gates.</summary>
public sealed class MemberCommandExplanationTests : FluentBunitContext
{
    /// <summary>Missing support explanations remain honest for null, empty and whitespace reasons.</summary>
    /// <param name="culture">The rendered resource culture.</param>
    /// <param name="reason">The absent support reason.</param>
    [Theory]
    [InlineData("en", null)]
    [InlineData("en", "")]
    [InlineData("en", " \t")]
    [InlineData("fr", null)]
    [InlineData("fr", "")]
    [InlineData("fr", " \t")]
    public Task UnknownSupportCauseDoesNotClaimAnotherCommand(string culture, string? reason)
        => InCultureAsync(culture, () =>
        {
            IRenderedComponent<MemberAccessReview> cut = RenderMembers();
            cut.FindAll("[data-testid='tenants-change-role-open']").Count.ShouldBe(2);
            cut.Render(parameters => parameters
                .Add(p => p.IsCommandSurfaceAvailable, false)
                .Add(p => p.CommandSurfaceUnavailableReason, reason));

            string expected = culture == "fr"
                ? "Les commandes de gestion des membres sont indisponibles pour ce locataire."
                : "Member commands are unavailable for this tenant.";
            AssertBlockedRows(cut, expected);
            Gateway.ReceivedCalls().ShouldBeEmpty();

            cut.Render(parameters => parameters.Add(p => p.IsCommandSurfaceAvailable, true));
            cut.FindAll("[data-testid='tenants-change-role-open']").Count.ShouldBe(2);
            cut.FindAll("[data-testid='tenants-remove-member-open']").Count.ShouldBe(2);
            return Task.CompletedTask;
        });

    /// <summary>A known external explanation is preserved while row actions remain blocked.</summary>
    /// <param name="culture">The rendered resource culture.</param>
    /// <param name="availableFlag">Whether the parent availability flag alone would allow commands.</param>
    [Theory]
    [InlineData("en", false)]
    [InlineData("en", true)]
    [InlineData("fr", false)]
    [InlineData("fr", true)]
    public Task ExplicitExternalReasonIsPreserved(string culture, bool availableFlag)
        => InCultureAsync(culture, () =>
        {
            IRenderedComponent<MemberAccessReview> cut = RenderMembers();
            string reason = Localizer["Tenants.Members.UnavailableReason.AggregateLocked"].Value;
            cut.Render(parameters => parameters
                .Add(p => p.IsCommandSurfaceAvailable, availableFlag)
                .Add(p => p.CommandSurfaceUnavailableReason, reason));

            AssertBlockedRows(cut, reason);
            cut.Find("[data-testid='tenants-member-reason-aggregate-locked']").TextContent
                .ShouldContain(Localizer["Tenants.Members.UnavailableReason.AggregateLocked.Catalog"].Value);
            Gateway.ReceivedCalls().ShouldBeEmpty();
            return Task.CompletedTask;
        });

    /// <summary>Gets every member lease owner in both cultures with both parent conditions.</summary>
    /// <returns>Culture, flow and parent-condition combinations.</returns>
    public static IEnumerable<object[]> ChildLeaseCases()
    {
        foreach (string culture in new[] { "en", "fr" })
        {
            foreach (string flow in new[] { "add", "change", "remove" })
            {
                yield return [culture, flow, false];
                yield return [culture, flow, true];
            }
        }
    }

    /// <summary>Rows describe the owned command without falsely assigning it to another command.</summary>
    /// <param name="culture">The rendered resource culture.</param>
    /// <param name="flow">The lease-owning member flow.</param>
    /// <param name="parentEchoesLock">Whether the parent echoes its aggregate lock to the child.</param>
    [Theory]
    [MemberData(nameof(ChildLeaseCases))]
    public Task OwnedReservationDoesNotCallItselfAnotherCommand(string culture, string flow, bool parentEchoesLock)
        => InCultureAsync(culture, async () =>
        {
            IRenderedComponent<MemberAccessReview> cut = RenderMembers();
            if (flow != "add")
            {
                cut.FindAll($"[data-testid='tenants-{(flow == "change" ? "change-role" : "remove-member")}-open']")[0].Click();
            }

            Func<bool, Task<bool>> lease = flow switch
            {
                "add" => cut.FindComponent<AddTenantMemberFlow>().Instance.CommandActivityLease!,
                "change" => cut.FindComponent<ChangeTenantMemberRoleFlow>().Instance.CommandActivityLease!,
                _ => cut.FindComponent<RemoveTenantMemberFlow>().Instance.CommandActivityLease!,
            };
            // Invoke the real child-to-owner admission boundary. No private fields or fake snapshots.
            (await cut.InvokeAsync(() => lease(true))).ShouldBeTrue();
            cut.Render(parameters => parameters
                .Add(p => p.IsCommandSurfaceAvailable, !parentEchoesLock)
                .Add(p => p.CommandSurfaceUnavailableReason, parentEchoesLock
                    ? Localizer["Tenants.Members.UnavailableReason.AggregateLocked"].Value : null));

            string expected = culture == "fr"
                ? "Une commande de gestion des membres est active pour ce locataire. Examinez cette commande avant d’en démarrer une autre."
                : "A member command is active for this tenant. Review that command before starting another.";
            AssertBlockedRows(cut, expected);
            Gateway.ReceivedCalls().ShouldBeEmpty();

            (await cut.InvokeAsync(() => lease(false))).ShouldBeTrue();
            cut.Render(parameters => parameters
                .Add(p => p.IsCommandSurfaceAvailable, true)
                .Add(p => p.CommandSurfaceUnavailableReason, (string?)null));
            cut.FindAll("[data-testid='tenants-change-role-open']").Count.ShouldBe(2);
            cut.FindAll("[data-testid='tenants-remove-member-open']").Count.ShouldBe(2);
        });

    /// <summary>Permission, freshness and lifecycle failures retain precedence over command contention.</summary>
    /// <param name="culture">The rendered resource culture.</param>
    /// <param name="scenario">The earlier evidence failure.</param>
    /// <param name="resource">The expected reason resource suffix.</param>
    [Theory]
    [InlineData("en", "unauthorized", "MissingPermission")]
    [InlineData("en", "stale", "StaleData")]
    [InlineData("en", "disabled", "MissingLifecycleSupport")]
    [InlineData("fr", "unauthorized", "MissingPermission")]
    [InlineData("fr", "stale", "StaleData")]
    [InlineData("fr", "disabled", "MissingLifecycleSupport")]
    public Task EarlierEvidenceFailuresKeepTheirOwnExplanation(string culture, string scenario, string resource)
        => InCultureAsync(culture, () =>
        {
            IRenderedComponent<MemberAccessReview> cut = RenderMembers();
            cut.Render(parameters => parameters
                .Add(p => p.IsCommandSurfaceAvailable, false)
                .Add(p => p.CommandSurfaceUnavailableReason, Localizer["Tenants.Members.UnavailableReason.AggregateLocked"].Value)
                .Add(p => p.SurfaceKind, scenario == "unauthorized" ? TenantDetailSurfaceKind.Unauthorized : TenantDetailSurfaceKind.Ready)
                .Add(p => p.Freshness, scenario == "stale" ? ReadModelFreshnessState.Stale : ReadModelFreshnessState.Current)
                .Add(p => p.Detail, Detail() with { Status = scenario == "disabled" ? TenantStatus.Disabled : TenantStatus.Active }));

            AssertBlockedRows(cut, Localizer[$"Tenants.Members.UnavailableReason.{resource}"].Value);
            Gateway.ReceivedCalls().ShouldBeEmpty();
            return Task.CompletedTask;
        });

    /// <summary>An admission refusal or fault removes the provisional owned-command explanation.</summary>
    /// <param name="culture">The rendered resource culture.</param>
    /// <param name="flow">The member flow requesting the lease.</param>
    /// <param name="throws">Whether the parent faults rather than refusing admission.</param>
    [Theory]
    [MemberData(nameof(ChildLeaseCases))]
    public Task FailedAdmissionClearsTheProvisionalCommandExplanation(string culture, string flow, bool throws)
        => InCultureAsync(culture, async () =>
        {
            var parent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            IRenderedComponent<MemberAccessReview> cut = RenderMembers(active => active ? parent.Task : Task.FromResult(true));
            if (flow != "add")
            {
                cut.FindAll($"[data-testid='tenants-{(flow == "change" ? "change-role" : "remove-member")}-open']")[0].Click();
            }
            Func<bool, Task<bool>> lease = flow switch
            {
                "add" => cut.FindComponent<AddTenantMemberFlow>().Instance.CommandActivityLease!,
                "change" => cut.FindComponent<ChangeTenantMemberRoleFlow>().Instance.CommandActivityLease!,
                _ => cut.FindComponent<RemoveTenantMemberFlow>().Instance.CommandActivityLease!,
            };
            Task<bool> admission = cut.InvokeAsync(() => lease(true));
            cut.Render(parameters => parameters.Add(p => p.IsCommandSurfaceAvailable, false));
            AssertBlockedRows(cut, Localizer["Tenants.Members.UnavailableReason.ActiveMemberCommand"].Value);
            if (throws)
            {
                parent.SetException(new InvalidOperationException("Admission failed."));
                await Should.ThrowAsync<InvalidOperationException>(() => admission);
            }
            else
            {
                parent.SetResult(false);
                (await admission).ShouldBeFalse();
            }
            cut.Render();
            AssertBlockedRows(cut, Localizer["Tenants.Members.UnavailableReason.CommandSurfaceUnavailable"].Value);
            cut.Render(parameters => parameters.Add(p => p.CommandSurfaceUnavailableReason,
                Localizer["Tenants.Members.UnavailableReason.AggregateLocked"].Value));
            AssertBlockedRows(cut, Localizer["Tenants.Members.UnavailableReason.AggregateLocked"].Value);
            Gateway.ReceivedCalls().ShouldBeEmpty();
        });

    private ITenantCommandGateway Gateway { get; set; } = null!;

    private IStringLocalizer<TenantsResources> Localizer
        => Services.GetRequiredService<IStringLocalizer<TenantsResources>>();

    private IRenderedComponent<MemberAccessReview> RenderMembers(Func<bool, Task<bool>>? lease = null)
    {
        Services.AddLocalization();
        Gateway = Substitute.For<ITenantCommandGateway>();
        Services.AddSingleton(Gateway);
        TenantDetail detail = Detail();
        return Render<MemberAccessReview>(parameters => parameters
            .Add(p => p.Detail, detail)
            .Add(p => p.Members, TenantUsersSnapshot.Ready(detail.TenantId, detail.Members, null, false,
                "members-etag", "tenant-sequence:1", ReadModelFreshnessState.Current, ProjectionLifecycleState.Current))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.ProjectionVersion, "tenant-sequence:1")
            .Add(p => p.IsAddMemberAuthorized, true)
            .Add(p => p.IsChangeRoleAuthorized, true)
            .Add(p => p.IsRemoveMemberAuthorized, true)
            .Add(p => p.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(p => p.AuthorizationReflectionProvider, _ => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized))
            .Add(p => p.AuditProofCapabilityAvailable, true)
            .Add(p => p.CommandActivityLease, lease));
    }

    private static TenantDetail Detail()
        => new("tenant.alpha", "Alpha", "Description", TenantStatus.Active,
            [new("owner-user", TenantRole.TenantOwner), new("reader-user", TenantRole.TenantReader)],
            new Dictionary<string, string>(), DateTimeOffset.UnixEpoch);

    private static void AssertBlockedRows(IRenderedComponent<MemberAccessReview> cut, string expected)
    {
        cut.FindAll("[data-testid='tenants-change-role-open']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-remove-member-open']").ShouldBeEmpty();
        var reasons = cut.FindAll("[data-testid='tenants-member-unavailable-reason']");
        reasons.Count.ShouldBe(2);
        foreach (var reason in reasons)
        {
            reason.QuerySelector("span:last-child").ShouldNotBeNull().TextContent.ShouldBe(expected);
        }
        cut.FindAll("[data-testid='tenants-member-action-slot']").Count.ShouldBe(6);
        cut.FindAll("[data-testid='tenants-member-action-slot']")
            .ShouldAllBe(slot => slot.GetAttribute("aria-label")!.Contains(expected, StringComparison.Ordinal));
    }

    private static async Task InCultureAsync(string culture, Func<Task> action)
    {
        CultureInfo priorCulture = CultureInfo.CurrentCulture;
        CultureInfo priorUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
            await action();
        }
        finally
        {
            CultureInfo.CurrentCulture = priorCulture;
            CultureInfo.CurrentUICulture = priorUiCulture;
        }
    }
}
