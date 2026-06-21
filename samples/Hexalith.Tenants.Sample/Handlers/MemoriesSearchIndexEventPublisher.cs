using Dapr.Client;

using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;

// Alias the one Memories contract used here: its V1 namespace also defines TenantStatus, which would
// collide with the tenant domain enum if imported wholesale.
using SearchIndexEntryChanged = Hexalith.Memories.Contracts.V1.SearchIndexEntryChanged;

namespace Hexalith.Tenants.Sample.Handlers;

/// <summary>
/// Maintains the Memories <c>tenants-index</c> search index by publishing one curated
/// <see cref="SearchIndexEntryChanged"/> per tenant lifecycle event (<see cref="TenantCreated"/>,
/// <see cref="TenantUpdated"/>, <see cref="TenantDisabled"/>, <see cref="TenantEnabled"/>).
/// </summary>
/// <remarks>
/// <para>
/// The publisher lives in a pub/sub consumer (not the broker-free Client package) co-located with the
/// local projection it reads. It is idempotent: the platform <c>EventStoreDomainEventProcessor</c> dedups
/// by <c>MessageId</c> before dispatch, and the emitted event uses upsert-by-(<c>TenantId</c>,
/// <c>AggregateId</c>) semantics so re-delivery of the same state is harmless.
/// </para>
/// <para>
/// The CloudEvent <c>id</c> is set to <c>tenant:{tenantId}</c> so it is echoed verbatim as
/// <c>ScoredResult.SourceUri</c>, letting the BFF recover the tenant id from a search hit. It never calls
/// the <c>[Experimental]</c> <c>MemoriesClient.IngestAsync</c> (build-breaking under TreatWarningsAsErrors).
/// </para>
/// </remarks>
public class MemoriesSearchIndexEventPublisher :
    IEventStoreDomainEventHandler<TenantCreated>,
    IEventStoreDomainEventHandler<TenantUpdated>,
    IEventStoreDomainEventHandler<TenantDisabled>,
    IEventStoreDomainEventHandler<TenantEnabled> {
    // Dapr pub/sub component + Memories ingestion topic. Both MUST stay aligned with the AppHost wiring
    // (pubsub scopes + MEMORIES_EVENTSTORE_TOPIC) and the Memories SourceToTenantMap, or ingestion no-ops.
    private const string PubSubName = "pubsub";
    private const string MemoriesIngestionTopic = "memories-events";

    // The dedicated, curated search index this producer feeds (one doc per tenant -> no over-matching).
    private const string SearchIndexName = "tenants-index";

    // cloudevent.source must match a Memories SourceToTenantMap prefix routing to the tenants-index tenant.
    private const string CloudEventSource = "hexalith-tenants";

    // cloudevent.id (== ScoredResult.SourceUri) the BFF parses back to a tenant id (AC3). Stable per tenant.
    private const string SourceIdPrefix = "tenant:";
    private const string StatusAttributeKey = "status";

    private readonly DaprClient _daprClient;
    private readonly ITenantProjectionStore _projectionStore;
    private readonly ILogger<MemoriesSearchIndexEventPublisher> _logger;

    public MemoriesSearchIndexEventPublisher(
        DaprClient daprClient,
        ITenantProjectionStore projectionStore,
        ILogger<MemoriesSearchIndexEventPublisher> logger) {
        ArgumentNullException.ThrowIfNull(daprClient);
        ArgumentNullException.ThrowIfNull(projectionStore);
        ArgumentNullException.ThrowIfNull(logger);
        _daprClient = daprClient;
        _projectionStore = projectionStore;
        _logger = logger;
    }

    public Task HandleAsync(TenantCreated @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(@event);
        return PublishAsync(@event.TenantId, TenantStatus.Active, @event.Name, context, cancellationToken);
    }

    public Task HandleAsync(TenantUpdated @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(@event);
        return PublishAsync(@event.TenantId, TenantStatus.Active, @event.Name, context, cancellationToken);
    }

    public Task HandleAsync(TenantDisabled @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(@event);
        return PublishAsync(@event.TenantId, TenantStatus.Disabled, fallbackName: null, context, cancellationToken);
    }

    public Task HandleAsync(TenantEnabled @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(@event);
        return PublishAsync(@event.TenantId, TenantStatus.Active, fallbackName: null, context, cancellationToken);
    }

    private async Task PublishAsync(
        string tenantId,
        TenantStatus fallbackStatus,
        string? fallbackName,
        EventStoreDomainEventContext context,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(context);

        // Prefer the authoritative current snapshot from the local projection (the projection handler is
        // registered first via AddHexalithTenants, so it has applied this event already). Fall back to
        // event-derived values on the rare race where the projection has not caught up; the upsert self-heals.
        TenantLocalState? state = await _projectionStore.GetAsync(tenantId, cancellationToken).ConfigureAwait(false);
        string name = !string.IsNullOrWhiteSpace(state?.Name) ? state!.Name : fallbackName ?? tenantId;
        TenantStatus status = state is not null && state.Status != TenantStatus.Unknown ? state.Status : fallbackStatus;

        SearchIndexEntryChanged entry = new() {
            TenantId = SearchIndexName,
            AggregateId = tenantId,
            Text = $"{name} {tenantId}",
            Attributes = new Dictionary<string, string>(StringComparer.Ordinal) {
                [StatusAttributeKey] = status.ToString(),
            },
            CorrelationId = context.CorrelationId,
            CausationId = context.MessageId,
        };

        Dictionary<string, string> metadata = new(StringComparer.Ordinal) {
            ["cloudevent.id"] = SourceIdPrefix + tenantId,
            ["cloudevent.type"] = nameof(SearchIndexEntryChanged),
            ["cloudevent.source"] = CloudEventSource,
        };

        await _daprClient
            .PublishEventAsync(PubSubName, MemoriesIngestionTopic, entry, metadata, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "[Tenants->Memories] Published SearchIndexEntryChanged for tenant {TenantId} (status {Status}); message {MessageId}; correlation {CorrelationId}",
            tenantId,
            status,
            context.MessageId,
            context.CorrelationId);
    }
}
