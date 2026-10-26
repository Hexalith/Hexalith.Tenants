using System.Globalization;

using AngleSharp.Dom;

using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Components.Shared;
using Hexalith.Tenants.UI.Components.Tenants;
using Hexalith.Tenants.UI.Components.Tenants.Audit;
using Hexalith.Tenants.UI.Components.Users;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.UserTenants;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

/// <summary>Qualifies status anatomy and exports actual localized badges for Chromium accessibility checks.</summary>
public sealed class StatusBadgeContractTests : FluentBunitContext
{
    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public void StatusBadgesExposeOneLocalizedNameAndExportBrowserFixtures(string cultureName)
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            Services.AddLocalization();
            List<string> markup = [];
            foreach (ReadModelFreshnessState freshness in Enum.GetValues<ReadModelFreshnessState>())
            {
                var cut = Render<TruthStateBadge>(parameters => parameters.Add(p => p.Freshness, freshness));
                CheckBadges(cut);
                BadgeContractFixture.Expect(cut.Find("fluent-badge"), cultureName, "truth", freshness.ToString(),
                    $"Tenants.List.Freshness.{freshness}");
                markup.Add(cut.Find("[data-status-badge]").OuterHtml);
            }
            var refreshing = Render<TruthStateBadge>(parameters => parameters.Add(p => p.IsRefreshing, true));
            CheckBadges(refreshing);
            BadgeContractFixture.Expect(refreshing.Find("fluent-badge"), cultureName, "truth", "Refreshing", "Tenants.List.Freshness.Refreshing");
            markup.Add(refreshing.Find("[data-status-badge]").OuterHtml);
            foreach (ProjectionLifecycleState lifecycle in Enum.GetValues<ProjectionLifecycleState>())
            {
                var cut = Render<ProjectionLifecycleBadge>(parameters => parameters.Add(p => p.Lifecycle, lifecycle));
                CheckBadges(cut);
                BadgeContractFixture.Expect(cut.Find("fluent-badge"), cultureName, "lifecycle", lifecycle.ToString(),
                    $"Tenants.ProjectionLifecycle.{lifecycle}");
                markup.Add(cut.Find("[data-status-badge]").OuterHtml);
            }
            foreach ((TenantCommandAuditState state, string labelState) in new[]
            {
                (TenantCommandAuditState.AuditPending, "Pending"),
                (TenantCommandAuditState.AuditDelayed, "Delayed"),
                (TenantCommandAuditState.AuditUnavailable, "Unavailable"),
                (TenantCommandAuditState.AuditAvailable, "Available"),
                (TenantCommandAuditState.MissingSupport, "MissingSupport"),
            })
            {
                var cut = Render<AuditAvailabilityState>(parameters => parameters.Add(p => p.AuditState, state));
                CheckBadges(cut);
                BadgeContractFixture.Expect(cut.Find("fluent-badge"), cultureName, "availability", state.ToString(),
                    $"Tenants.Audit.Availability.State.{labelState}");
                // Keep the scoped production root: its ::deep rules govern forced-color borders.
                markup.Add(cut.Find("section.tenants-audit-availability").OuterHtml);
            }
            Render<AuditAvailabilityState>(parameters => parameters.Add(p => p.AuditState, TenantCommandAuditState.NotStarted))
                .FindAll("fluent-badge").ShouldBeEmpty();
            foreach (TenantStatus status in Enum.GetValues<TenantStatus>())
            {
                var cut = Render<TenantDataGrid>(parameters => parameters.Add(p => p.Rows,
                    [new TenantListRow(status.ToString(), status.ToString(), status, TenantCountValue.Unknown,
                        TenantCountValue.Unknown, TenantPendingState.None, ReadModelFreshnessState.Current)]));
                CheckBadges(cut);
                markup.Add(BadgeContractFixture.Expect(cut.Find("[data-testid='tenants-list-status']"), cultureName,
                    "tenant-status", status.ToString(), $"Tenants.List.Status.{status}"));
            }
            foreach (TenantPendingState pending in Enum.GetValues<TenantPendingState>())
            {
                var cut = Render<TenantDataGrid>(parameters => parameters.Add(p => p.Rows,
                    [new TenantListRow("tenant", "Tenant", TenantStatus.Active, TenantCountValue.Unknown,
                        TenantCountValue.Unknown, pending, ReadModelFreshnessState.Current)]));
                CheckBadges(cut);
                markup.Add(BadgeContractFixture.Expect(cut.Find("[data-testid='tenants-list-pending']"), cultureName,
                    "tenant-pending", pending.ToString(), $"Tenants.List.Pending.{pending}"));
            }
            foreach (TenantStatus status in Enum.GetValues<TenantStatus>())
            {
                var cut = Render<MyTenantsDataGrid>(parameters => parameters.Add(p => p.Rows,
                    [new UserTenantMembershipRow("tenant", "Tenant", status, TenantRole.TenantReader, ReadModelFreshnessState.Current)]));
                CheckBadges(cut);
                markup.Add(BadgeContractFixture.Expect(cut.Find("[data-testid='tenants-my-status']"), cultureName,
                    "membership-status", status.ToString(), $"Tenants.MyTenants.Status.{status}", "Tenants.MyTenants.StatusAccessible"));
            }
            foreach (TenantRole role in Enum.GetValues<TenantRole>())
            {
                var cut = Render<MyTenantsDataGrid>(parameters => parameters.Add(p => p.Rows,
                    [new UserTenantMembershipRow("tenant", "Tenant", TenantStatus.Active, role, ReadModelFreshnessState.Current)]));
                CheckBadges(cut);
                markup.Add(BadgeContractFixture.Expect(cut.Find("[data-testid='tenants-my-role']"), cultureName,
                    "membership-role", role.ToString(), $"Tenants.MyTenants.Role.{role}", "Tenants.MyTenants.RoleAccessible"));
            }
            foreach (AuditEventCategory category in Enum.GetValues<AuditEventCategory>())
            {
                var cut = Render<AuditDataGrid>(parameters => parameters.Add(p => p.Rows,
                    [new TenantAuditRow("reference", "TenantCreated", category, "actor", DateTimeOffset.UtcNow,
                        "tenant", "tenant", "tenant", "TenantCreated", string.Empty, ReadModelFreshnessState.Current)]));
                CheckBadges(cut);
                markup.Add(BadgeContractFixture.Expect(cut.Find("fluent-badge"), cultureName,
                    "audit-category", category.ToString(), $"Tenants.Audit.Category.{category}"));
            }

            string? directory = Environment.GetEnvironmentVariable("TENANTS_BADGE_FIXTURE_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, $"status-badges-{cultureName}.html"), string.Join(Environment.NewLine, markup));
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    private static void CheckBadges<T>(IRenderedComponent<T> cut) where T : class, Microsoft.AspNetCore.Components.IComponent
    {
        foreach (var rendered in cut.FindComponents<FluentBadge>())
        {
            FluentBadge badge = rendered.Instance;
            badge.Shape.ShouldBe(BadgeShape.Rounded);
            badge.Appearance.ShouldBe(badge.Color is BadgeColor.Danger or BadgeColor.Severe ? BadgeAppearance.Filled : BadgeAppearance.Tint);
            badge.IconLabel.ShouldNotBeNullOrWhiteSpace();
            badge.IconLabel.ShouldNotStartWith("Tenants.");
            rendered.Find("fluent-badge").TextContent.Trim().ShouldBe(badge.IconLabel);
        }

        foreach (var element in cut.FindAll("fluent-badge"))
        {
            element.GetAttribute("aria-hidden").ShouldBe("true");
            element.Closest("[data-status-badge]").ShouldNotBeNull().GetAttribute("role").ShouldBe("img");
            element.Closest("[data-status-badge]").ShouldNotBeNull().GetAttribute("aria-label").ShouldBe(element.GetAttribute("aria-label"));
            element.GetAttribute("aria-label").ShouldNotBeNullOrWhiteSpace();
        }
    }
}
