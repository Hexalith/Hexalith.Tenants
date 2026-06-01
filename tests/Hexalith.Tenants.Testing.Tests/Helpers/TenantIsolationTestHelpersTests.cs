using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Testing.Fakes;
using Hexalith.Tenants.Testing.Helpers;

using Shouldly;

namespace Hexalith.Tenants.Testing.Tests.Helpers;

public sealed class TenantIsolationTestHelpersTests {
    [Fact]
    public void CreateServiceWithTenants_creates_independent_tenants_and_roles() {
        InMemoryTenantService service = TenantIsolationTestHelpers.CreateServiceWithTenants(
            new Dictionary<string, IReadOnlyDictionary<string, TenantRole>> {
                ["tenant-a"] = new Dictionary<string, TenantRole> {
                    ["shared-user"] = TenantRole.TenantOwner,
                    ["alice"] = TenantRole.TenantContributor,
                },
                ["tenant-b"] = new Dictionary<string, TenantRole> {
                    ["shared-user"] = TenantRole.TenantReader,
                    ["bob"] = TenantRole.TenantOwner,
                },
            },
            disabledTenantIds: new HashSet<string>(StringComparer.Ordinal) { "tenant-b" });

        DomainResult enableTenantB = TenantIsolationTestHelpers.EnableTenant(service, "tenant-b");
        IReadOnlyDictionary<string, TenantRole> sharedUserRoles = TenantIsolationTestHelpers.GetTenantRolesForUser(service, "shared-user");

        service.GetTenantState("tenant-a")!.Status.ShouldBe(TenantStatus.Active);
        enableTenantB.IsSuccess.ShouldBeTrue();
        service.GetTenantState("tenant-b")!.Status.ShouldBe(TenantStatus.Active);
        service.GetTenantState("tenant-a")!.Users["shared-user"].ShouldBe(TenantRole.TenantOwner);
        service.GetTenantState("tenant-b")!.Users["shared-user"].ShouldBe(TenantRole.TenantReader);
        sharedUserRoles["tenant-a"].ShouldBe(TenantRole.TenantOwner);
        sharedUserRoles["tenant-b"].ShouldBe(TenantRole.TenantReader);

        DomainResult disableTenantA = TenantIsolationTestHelpers.DisableTenant(service, "tenant-a");

        disableTenantA.IsSuccess.ShouldBeTrue();
        TenantIsolationTestHelpers.IsAuthorizedForTenant(
            service,
            "tenant-a",
            "shared-user",
            TenantRole.TenantReader).ShouldBeFalse();

        DomainResult enableTenantA = TenantIsolationTestHelpers.EnableTenant(service, "tenant-a");

        enableTenantA.IsSuccess.ShouldBeTrue();
        TenantIsolationTestHelpers.IsAuthorizedForTenant(
            service,
            "tenant-a",
            "shared-user",
            TenantRole.TenantContributor).ShouldBeTrue();
        TenantIsolationTestHelpers.IsAuthorizedForTenant(
            service,
            "tenant-b",
            "shared-user",
            TenantRole.TenantContributor).ShouldBeFalse();
        TenantIsolationTestHelpers.IsAuthorizedForTenant(
            service,
            "tenant-b",
            "shared-user",
            TenantRole.TenantReader).ShouldBeTrue();
    }

    [Fact]
    public void SeedTenants_returns_DomainResults_and_does_not_leak_between_services() {
        var first = new InMemoryTenantService();
        IReadOnlyList<DomainResult> firstResults = TenantIsolationTestHelpers.SeedTenants(
            first,
            new Dictionary<string, IReadOnlyDictionary<string, TenantRole>> {
                ["tenant-a"] = new Dictionary<string, TenantRole> {
                    ["alice"] = TenantRole.TenantOwner,
                },
            });

        var second = new InMemoryTenantService();
        IReadOnlyList<DomainResult> secondResults = TenantIsolationTestHelpers.SeedTenants(
            second,
            new Dictionary<string, IReadOnlyDictionary<string, TenantRole>> {
                ["tenant-a"] = new Dictionary<string, TenantRole> {
                    ["bob"] = TenantRole.TenantReader,
                },
            });

        firstResults.All(r => r.IsSuccess).ShouldBeTrue();
        secondResults.All(r => r.IsSuccess).ShouldBeTrue();
        first.GetTenantState("tenant-a")!.Users.ShouldContainKey("alice");
        first.GetTenantState("tenant-a")!.Users.ShouldNotContainKey("bob");
        second.GetTenantState("tenant-a")!.Users.ShouldContainKey("bob");
        second.GetTenantState("tenant-a")!.Users.ShouldNotContainKey("alice");
    }

    [Fact]
    public void GetTenantEvents_returns_only_payloads_for_requested_tenant() {
        InMemoryTenantService service = TenantIsolationTestHelpers.CreateServiceWithTenants(
            new Dictionary<string, IReadOnlyDictionary<string, TenantRole>> {
                ["tenant-a"] = new Dictionary<string, TenantRole> {
                    ["alice"] = TenantRole.TenantOwner,
                },
                ["tenant-b"] = new Dictionary<string, TenantRole> {
                    ["bob"] = TenantRole.TenantOwner,
                },
            },
            tenantConfiguration: new Dictionary<string, IReadOnlyDictionary<string, string>> {
                ["tenant-a"] = new Dictionary<string, string> {
                    ["sample.feature"] = "enabled",
                },
                ["tenant-b"] = new Dictionary<string, string> {
                    ["sample.feature"] = "disabled",
                },
            });

        IReadOnlyList<IEventPayload> tenantAEvents = TenantIsolationTestHelpers.GetTenantEvents(service, "tenant-a");
        var projection = new ConsumerTenantProjection();

        projection.ApplyEvents(tenantAEvents);

        tenantAEvents.Count.ShouldBe(3);
        tenantAEvents.All(e => TenantIsolationTestHelpers.GetTenantId(e) == "tenant-a").ShouldBeTrue();
        projection.GetMembers("tenant-a").ShouldContainKey("alice");
        projection.GetConfiguration("tenant-a")["sample.feature"].ShouldBe("enabled");
        projection.GetMembers("tenant-b").ShouldBeEmpty();
        projection.GetConfiguration("tenant-b").ShouldBeEmpty();
    }

    [Fact]
    public void DuplicateDelivery_repeats_selected_success_events_for_idempotency_checks() {
        InMemoryTenantService service = TenantIsolationTestHelpers.CreateServiceWithTenants(
            new Dictionary<string, IReadOnlyDictionary<string, TenantRole>> {
                ["tenant-a"] = new Dictionary<string, TenantRole> {
                    ["alice"] = TenantRole.TenantOwner,
                },
            });
        IReadOnlyList<IEventPayload> tenantAEvents = TenantIsolationTestHelpers.GetTenantEvents(service, "tenant-a");

        IReadOnlyList<IEventPayload> duplicated = TenantIsolationTestHelpers.DuplicateDelivery(tenantAEvents);
        var projection = new ConsumerTenantProjection();
        projection.ApplyEvents(duplicated);

        duplicated.Count.ShouldBe(tenantAEvents.Count * 2);
        projection.GetMembers("tenant-a").Count.ShouldBe(1);
        projection.IsAuthorized("tenant-a", "alice", TenantRole.TenantOwner).ShouldBeTrue();
    }

    [Fact]
    public void RemoveUser_emits_revocation_sequence_consumers_can_replay_without_infrastructure() {
        InMemoryTenantService service = TenantIsolationTestHelpers.CreateServiceWithTenants(
            new Dictionary<string, IReadOnlyDictionary<string, TenantRole>> {
                ["tenant-a"] = new Dictionary<string, TenantRole> {
                    ["alice"] = TenantRole.TenantReader,
                    ["owner"] = TenantRole.TenantOwner,
                },
            });

        DomainResult removeResult = TenantIsolationTestHelpers.RemoveUser(service, "tenant-a", "alice");
        IReadOnlyList<IEventPayload> tenantAEvents = TenantIsolationTestHelpers.GetTenantEvents(service, "tenant-a");
        var projection = new ConsumerTenantProjection();

        projection.ApplyEvents(tenantAEvents);

        removeResult.IsSuccess.ShouldBeTrue();
        tenantAEvents.OfType<UserAddedToTenant>().Any(e => e.UserId == "alice").ShouldBeTrue();
        tenantAEvents.OfType<UserRemovedFromTenant>().Any(e => e.UserId == "alice").ShouldBeTrue();
        projection.IsAuthorized("tenant-a", "alice", TenantRole.TenantReader).ShouldBeFalse();
        projection.IsAuthorized("tenant-a", "owner", TenantRole.TenantOwner).ShouldBeTrue();
    }

    private sealed class ConsumerTenantProjection {
        private readonly Dictionary<string, ConsumerTenantState> _tenants = [];

        public void ApplyEvents(IEnumerable<IEventPayload> events) {
            ArgumentNullException.ThrowIfNull(events);

            foreach (IEventPayload eventPayload in events) {
                Apply(eventPayload);
            }
        }

        public IReadOnlyDictionary<string, TenantRole> GetMembers(string tenantId)
            => _tenants.TryGetValue(tenantId, out ConsumerTenantState? state)
                ? state.Members
                : new Dictionary<string, TenantRole>();

        public IReadOnlyDictionary<string, string> GetConfiguration(string tenantId)
            => _tenants.TryGetValue(tenantId, out ConsumerTenantState? state)
                ? state.Configuration
                : new Dictionary<string, string>();

        public bool IsAuthorized(string tenantId, string userId, TenantRole minimumRole)
            => _tenants.TryGetValue(tenantId, out ConsumerTenantState? state)
               && state.Status == TenantStatus.Active
               && state.Members.TryGetValue(userId, out TenantRole role)
               && MeetsMinimumRole(role, minimumRole);

        private static bool MeetsMinimumRole(TenantRole role, TenantRole minimumRole)
            => minimumRole switch {
                TenantRole.TenantReader => role is TenantRole.TenantReader or TenantRole.TenantContributor or TenantRole.TenantOwner,
                TenantRole.TenantContributor => role is TenantRole.TenantContributor or TenantRole.TenantOwner,
                TenantRole.TenantOwner => role is TenantRole.TenantOwner,
                _ => false,
            };

        private void Apply(IEventPayload eventPayload) {
            switch (eventPayload) {
                case TenantCreated e:
                    _tenants.TryAdd(e.TenantId, new ConsumerTenantState(e.Name, TenantStatus.Active));
                    break;
                case TenantUpdated e:
                    GetOrCreate(e.TenantId).Name = e.Name;
                    break;
                case TenantDisabled e:
                    GetOrCreate(e.TenantId).Status = TenantStatus.Disabled;
                    break;
                case TenantEnabled e:
                    GetOrCreate(e.TenantId).Status = TenantStatus.Active;
                    break;
                case UserAddedToTenant e:
                    GetOrCreate(e.TenantId).Members[e.UserId] = e.Role;
                    break;
                case UserRemovedFromTenant e:
                    _ = GetOrCreate(e.TenantId).Members.Remove(e.UserId);
                    break;
                case UserRoleChanged e:
                    GetOrCreate(e.TenantId).Members[e.UserId] = e.NewRole;
                    break;
                case TenantConfigurationSet e:
                    GetOrCreate(e.TenantId).Configuration[e.Key] = e.Value;
                    break;
                case TenantConfigurationRemoved e:
                    _ = GetOrCreate(e.TenantId).Configuration.Remove(e.Key);
                    break;
                default:
                    break;
            }
        }

        private ConsumerTenantState GetOrCreate(string tenantId) {
            if (!_tenants.TryGetValue(tenantId, out ConsumerTenantState? state)) {
                state = new ConsumerTenantState(tenantId, TenantStatus.Active);
                _tenants[tenantId] = state;
            }

            return state;
        }
    }

    private sealed class ConsumerTenantState(string name, TenantStatus status) {
        public string Name { get; set; } = name;

        public TenantStatus Status { get; set; } = status;

        public Dictionary<string, TenantRole> Members { get; } = [];

        public Dictionary<string, string> Configuration { get; } = [];
    }
}
