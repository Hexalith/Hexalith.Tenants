using Dapr.Client;

using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Sample.Handlers;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.Core;

using Shouldly;

// Memories.Contracts.V1 also defines TenantStatus, so alias the one contract this publisher emits.
using SearchIndexEntryChanged = Hexalith.Memories.Contracts.V1.SearchIndexEntryChanged;

namespace Hexalith.Tenants.Sample.Tests.Handlers;

public sealed class MemoriesSearchIndexEventPublisherTests {
    private const string PubSub = "pubsub";
    private const string Topic = "memories-events";

    private static EventStoreDomainEventContext CreateContext(string tenantId = "acme")
        => new("system", tenantId, "msg-1", 1, DateTimeOffset.UtcNow, "corr-1");

    [Fact]
    public async Task TenantCreated_publishes_one_curated_entry_with_stable_id_and_active_status() {
        (DaprClient dapr, ITenantProjectionStore store) = Mocks();
        StoreReturns(store, "acme", "Acme Corporation", TenantStatus.Active);
        MemoriesSearchIndexEventPublisher publisher = Create(dapr, store);

        await publisher.HandleAsync(
            new TenantCreated("acme", "Acme Corporation", null, DateTimeOffset.UtcNow),
            CreateContext(),
            CancellationToken.None);

        (SearchIndexEntryChanged entry, Dictionary<string, string> metadata) = SinglePublishedEntry(dapr);
        entry.TenantId.ShouldBe("tenants-index"); // the curated search index, not the tenant domain id
        entry.AggregateId.ShouldBe("acme");
        entry.Text.ShouldContain("Acme Corporation");
        entry.Text.ShouldContain("acme");
        entry.Attributes["status"].ShouldBe(nameof(TenantStatus.Active));
        metadata["cloudevent.id"].ShouldBe("tenant:acme"); // == ScoredResult.SourceUri the BFF parses
        metadata["cloudevent.type"].ShouldBe(nameof(SearchIndexEntryChanged));
        metadata["cloudevent.source"].ShouldBe("hexalith-tenants");
    }

    [Fact]
    public async Task TenantDisabled_publishes_disabled_status_with_name_from_projection() {
        (DaprClient dapr, ITenantProjectionStore store) = Mocks();
        StoreReturns(store, "acme", "Acme Corporation", TenantStatus.Disabled);
        MemoriesSearchIndexEventPublisher publisher = Create(dapr, store);

        await publisher.HandleAsync(new TenantDisabled("acme", DateTimeOffset.UtcNow), CreateContext(), CancellationToken.None);

        (SearchIndexEntryChanged entry, _) = SinglePublishedEntry(dapr);
        entry.Attributes["status"].ShouldBe(nameof(TenantStatus.Disabled));
        entry.Text.ShouldContain("Acme Corporation"); // TenantDisabled carries no Name -> resolved from projection
    }

    [Fact]
    public async Task TenantEnabled_publishes_active_status() {
        (DaprClient dapr, ITenantProjectionStore store) = Mocks();
        StoreReturns(store, "acme", "Acme Corporation", TenantStatus.Active);
        MemoriesSearchIndexEventPublisher publisher = Create(dapr, store);

        await publisher.HandleAsync(new TenantEnabled("acme", DateTimeOffset.UtcNow), CreateContext(), CancellationToken.None);

        (SearchIndexEntryChanged entry, _) = SinglePublishedEntry(dapr);
        entry.Attributes["status"].ShouldBe(nameof(TenantStatus.Active));
    }

    [Fact]
    public async Task TenantUpdated_rename_is_reflected_in_searchable_text() {
        (DaprClient dapr, ITenantProjectionStore store) = Mocks();
        StoreReturns(store, "acme", "Renamed Corporation", TenantStatus.Active);
        MemoriesSearchIndexEventPublisher publisher = Create(dapr, store);

        await publisher.HandleAsync(
            new TenantUpdated("acme", "Renamed Corporation", null, DateTimeOffset.UtcNow),
            CreateContext(),
            CancellationToken.None);

        (SearchIndexEntryChanged entry, _) = SinglePublishedEntry(dapr);
        entry.Text.ShouldContain("Renamed Corporation");
    }

    [Fact]
    public async Task Falls_back_to_event_data_when_projection_has_not_caught_up() {
        (DaprClient dapr, ITenantProjectionStore store) = Mocks();
        store.GetAsync("acme", Arg.Any<CancellationToken>()).Returns(Task.FromResult<TenantLocalState?>(null));
        MemoriesSearchIndexEventPublisher publisher = Create(dapr, store);

        await publisher.HandleAsync(
            new TenantCreated("acme", "Event Name", null, DateTimeOffset.UtcNow),
            CreateContext(),
            CancellationToken.None);

        (SearchIndexEntryChanged entry, _) = SinglePublishedEntry(dapr);
        entry.Text.ShouldContain("Event Name"); // event Name fallback
        entry.Attributes["status"].ShouldBe(nameof(TenantStatus.Active)); // created -> active fallback
    }

    [Fact]
    public async Task Repeated_delivery_publishes_each_time_relying_on_platform_dedup_and_upsert() {
        (DaprClient dapr, ITenantProjectionStore store) = Mocks();
        StoreReturns(store, "acme", "Acme Corporation", TenantStatus.Active);
        MemoriesSearchIndexEventPublisher publisher = Create(dapr, store);
        TenantCreated created = new("acme", "Acme Corporation", null, DateTimeOffset.UtcNow);

        await publisher.HandleAsync(created, CreateContext(), CancellationToken.None);
        await publisher.HandleAsync(created, CreateContext(), CancellationToken.None);

        // The publisher does not deduplicate (the platform processor dedups by MessageId); republishing the
        // same curated state is harmless because the entry uses upsert-by-(TenantId, AggregateId) semantics.
        await dapr.Received(2).PublishEventAsync(
            PubSub,
            Topic,
            Arg.Any<SearchIndexEntryChanged>(),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Constructor_null_arguments_throw() {
        DaprClient dapr = Substitute.For<DaprClient>();
        ITenantProjectionStore store = Substitute.For<ITenantProjectionStore>();
        ILogger<MemoriesSearchIndexEventPublisher> logger = Substitute.For<ILogger<MemoriesSearchIndexEventPublisher>>();

        Should.Throw<ArgumentNullException>(() => new MemoriesSearchIndexEventPublisher(null!, store, logger));
        Should.Throw<ArgumentNullException>(() => new MemoriesSearchIndexEventPublisher(dapr, null!, logger));
        Should.Throw<ArgumentNullException>(() => new MemoriesSearchIndexEventPublisher(dapr, store, null!));
    }

    private static (DaprClient Dapr, ITenantProjectionStore Store) Mocks()
        => (Substitute.For<DaprClient>(), Substitute.For<ITenantProjectionStore>());

    private static void StoreReturns(ITenantProjectionStore store, string tenantId, string name, TenantStatus status)
        => store.GetAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantLocalState?>(new TenantLocalState { TenantId = tenantId, Name = name, Status = status }));

    private static MemoriesSearchIndexEventPublisher Create(DaprClient dapr, ITenantProjectionStore store)
        => new(dapr, store, Substitute.For<ILogger<MemoriesSearchIndexEventPublisher>>());

    private static (SearchIndexEntryChanged Entry, Dictionary<string, string> Metadata) SinglePublishedEntry(DaprClient dapr) {
        ICall call = dapr.ReceivedCalls()
            .Single(c => string.Equals(c.GetMethodInfo().Name, nameof(DaprClient.PublishEventAsync), StringComparison.Ordinal));
        object?[] arguments = call.GetArguments();
        return ((SearchIndexEntryChanged)arguments[2]!, (Dictionary<string, string>)arguments[3]!);
    }
}
