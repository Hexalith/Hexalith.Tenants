#if HEXALITH_CHECKPOINTED_TENANT_PROJECTION
using Hexalith.EventStore.Client.Projections.Checkpointed;

namespace Hexalith.Tenants.Client.Projections;

/// <summary>
/// Binds the durable tenant projection to a read-model state-store component.
/// </summary>
public sealed class DurableTenantProjectionOptions {
    /// <summary>
    /// Initializes a new instance of the <see cref="DurableTenantProjectionOptions"/> class.
    /// </summary>
    /// <param name="stateStoreName">The read-model state-store component name.</param>
    public DurableTenantProjectionOptions(string stateStoreName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateStoreName);
        StateStoreName = stateStoreName;
    }

    /// <summary>Gets the read-model state-store component name.</summary>
    public string StateStoreName { get; }

    /// <summary>Gets the checkpointed write, lease, and history options.</summary>
    public CheckpointedProjectionOptions Checkpoint { get; } = new();
}
#endif
