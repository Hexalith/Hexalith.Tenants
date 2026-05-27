using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Testing.Projections;

using Shouldly;

namespace Hexalith.Tenants.Testing.Tests.Projections;

/// <summary>
/// Drift guard (TEN-4): every non-rejection event payload in Contracts.Events must be explicitly handled
/// by <see cref="InMemoryTenantProjection.Apply"/>. Adding a success event without wiring it (it would hit
/// the silent <c>default:</c> arm) fails <see cref="AllSuccessEvents_AreWired_IntoProjection"/>.
/// </summary>
[Trait("Category", "Conformance")]
public sealed class InMemoryTenantProjectionConformanceTests {
    private static readonly DateTimeOffset When = DateTimeOffset.Parse("2026-01-15T10:30:00+00:00");

    // Success (non-rejection) events the in-memory projection is expected to handle.
    private static readonly HashSet<string> ExpectedHandledEvents = new(StringComparer.Ordinal) {
        nameof(TenantCreated), nameof(TenantUpdated), nameof(TenantDisabled), nameof(TenantEnabled),
        nameof(UserAddedToTenant), nameof(UserRemovedFromTenant), nameof(UserRoleChanged),
        nameof(TenantConfigurationSet), nameof(TenantConfigurationRemoved),
        nameof(GlobalAdministratorSet), nameof(GlobalAdministratorRemoved),
    };

    private static IEnumerable<string> DiscoverSuccessEventNames() =>
        typeof(TenantCreated).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract
                && typeof(IEventPayload).IsAssignableFrom(t)
                && !typeof(IRejectionEvent).IsAssignableFrom(t))
            .Select(t => t.Name);

    [Fact]
    public void AllSuccessEvents_AreWired_IntoProjection() {
        HashSet<string> discovered = DiscoverSuccessEventNames().ToHashSet(StringComparer.Ordinal);

        List<string> unwired = discovered.Except(ExpectedHandledEvents).OrderBy(n => n, StringComparer.Ordinal).ToList();
        unwired.ShouldBeEmpty(
            $"Success event(s) in Contracts.Events are not wired into InMemoryTenantProjection.Apply " +
            $"(they would hit the silent default: arm): {string.Join(", ", unwired)}. " +
            "Add a case for each and update ExpectedHandledEvents.");

        List<string> stale = ExpectedHandledEvents.Except(discovered).OrderBy(n => n, StringComparer.Ordinal).ToList();
        stale.ShouldBeEmpty($"ExpectedHandledEvents lists removed event(s): {string.Join(", ", stale)}.");
    }

    // Behavioral guard: tenant-scoped success events route through GetOrThrow, so applying them to an
    // empty projection throws. A dropped (default-arm) event would silently no-op instead.
    [Fact]
    public void TenantScopedEvents_on_empty_projection_are_routed_not_dropped() {
        IEventPayload[] events = [
            new TenantUpdated("ghost", "Name", "Desc"),
            new TenantDisabled("ghost", When),
            new TenantEnabled("ghost", When),
            new UserAddedToTenant("ghost", "user", TenantRole.TenantReader),
            new UserRemovedFromTenant("ghost", "user"),
            new UserRoleChanged("ghost", "user", TenantRole.TenantReader, TenantRole.TenantOwner),
            new TenantConfigurationSet("ghost", "key", "value"),
            new TenantConfigurationRemoved("ghost", "key"),
        ];

        foreach (IEventPayload evt in events) {
            var projection = new InMemoryTenantProjection();
            _ = Should.Throw<InvalidOperationException>(
                () => projection.Apply(evt),
                $"{evt.GetType().Name} must route through GetOrThrow on a missing tenant, not be silently dropped.");
        }
    }

    [Fact]
    public void TenantCreated_is_applied() {
        var projection = new InMemoryTenantProjection();

        projection.Apply(new TenantCreated("ghost", "Ghost", null, When));

        _ = projection.GetTenant("ghost").ShouldNotBeNull();
    }

    [Fact]
    public void GlobalAdministrator_events_are_applied() {
        var projection = new InMemoryTenantProjection();

        projection.Apply(new GlobalAdministratorSet("system", "admin-1"));
        projection.GetGlobalAdministrators().Administrators.ShouldContain("admin-1");

        projection.Apply(new GlobalAdministratorRemoved("system", "admin-1"));
        projection.GetGlobalAdministrators().Administrators.ShouldNotContain("admin-1");
    }
}
