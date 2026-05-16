using System.Text.Json;

using Hexalith.EventStore.Contracts.Projections;

namespace Hexalith.Tenants.Server.Projections;

/// <summary>
/// Per-tenant audit projection applied by the tenant projection handler.
/// </summary>
public static class TenantAuditProjection {
    public static TenantAuditReadModel ProjectAuditEvents(IEnumerable<ProjectionEventDto> events) {
        ArgumentNullException.ThrowIfNull(events);

        var model = new TenantAuditReadModel();
        foreach (ProjectionEventDto? evt in events) {
            if (evt is null) {
                continue;
            }

            // Malformed payloads must not poison the whole rebuild. Invariant violations
            // (missing MessageId/UserId, unsupported payload type) still propagate.
            try {
                model.Apply(evt);
            }
            catch (JsonException) {
                continue;
            }
        }

        model.SortEntries();
        return model;
    }
}
