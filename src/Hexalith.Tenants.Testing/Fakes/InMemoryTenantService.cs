using System.Text.Json;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Server.Aggregates;

namespace Hexalith.Tenants.Testing.Fakes;

/// <summary>
/// In-memory command processor that delegates to the same TenantAggregate and GlobalAdministratorsAggregate
/// Handle/Apply methods used in production. Does not reimplement domain logic — wraps existing pure-function
/// methods in a simple in-memory container for fast, infrastructure-free testing.
/// </summary>
public sealed class InMemoryTenantService
{
    private const string DefaultDomain = "tenants";
    private const string GlobalAdminAggregateId = "system";
    private const string GlobalAdminExtensionKey = "actor:globalAdmin";
    private const string SystemTenantId = "system";

    private readonly List<IEventPayload> _eventHistory = [];
    private readonly Dictionary<string, TenantState> _tenantStates = new();
    private GlobalAdministratorsState? _globalAdminState;

    /// <summary>
    /// Gets the accumulated list of all successful events across all commands.
    /// Enables sequence assertions and is reused by Story 6.2 conformance tests.
    /// </summary>
    public IReadOnlyList<IEventPayload> EventHistory => _eventHistory;

    /// <summary>
    /// Gets the current global admin state, or null if not bootstrapped.
    /// </summary>
    public GlobalAdministratorsState? GetGlobalAdminState() => _globalAdminState;

    /// <summary>
    /// Gets the current state for a tenant, or null if no commands have been processed for that tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The current <see cref="TenantState"/> or null.</returns>
    public TenantState? GetTenantState(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return _tenantStates.TryGetValue(tenantId, out TenantState? state) ? state : null;
    }

    // ─── Tenant Commands (no envelope needed) ───

    /// <summary>Processes a CreateTenant command.</summary>
    public DomainResult ProcessCommand(CreateTenant command)
    {
        ArgumentNullException.ThrowIfNull(command);
        TenantState? state = GetOrDefaultTenantState(command.TenantId);
        DomainResult result = TenantAggregate.Handle(command, state);
        ApplyTenantEvents(command.TenantId, result);
        return result;
    }

    /// <summary>Processes a DisableTenant command.</summary>
    public DomainResult ProcessCommand(DisableTenant command)
    {
        ArgumentNullException.ThrowIfNull(command);
        TenantState? state = GetOrDefaultTenantState(command.TenantId);
        DomainResult result = TenantAggregate.Handle(command, state);
        ApplyTenantEvents(command.TenantId, result);
        return result;
    }

    /// <summary>Processes an EnableTenant command.</summary>
    public DomainResult ProcessCommand(EnableTenant command)
    {
        ArgumentNullException.ThrowIfNull(command);
        TenantState? state = GetOrDefaultTenantState(command.TenantId);
        DomainResult result = TenantAggregate.Handle(command, state);
        ApplyTenantEvents(command.TenantId, result);
        return result;
    }

    // ─── Tenant Commands (envelope needed for RBAC) ───

    /// <summary>Processes an UpdateTenant command.</summary>
    public DomainResult ProcessCommand(UpdateTenant command, string userId, bool isGlobalAdmin = false)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        CommandEnvelope envelope = CreateEnvelope(command, command.TenantId, userId, isGlobalAdmin);
        TenantState? state = GetOrDefaultTenantState(command.TenantId);
        DomainResult result = TenantAggregate.Handle(command, state, envelope);
        ApplyTenantEvents(command.TenantId, result);
        return result;
    }

    /// <summary>Processes an AddUserToTenant command.</summary>
    public DomainResult ProcessCommand(AddUserToTenant command, string userId, bool isGlobalAdmin = false)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        CommandEnvelope envelope = CreateEnvelope(command, command.TenantId, userId, isGlobalAdmin);
        TenantState? state = GetOrDefaultTenantState(command.TenantId);
        DomainResult result = TenantAggregate.Handle(command, state, envelope);
        ApplyTenantEvents(command.TenantId, result);
        return result;
    }

    /// <summary>Processes a RemoveUserFromTenant command.</summary>
    public DomainResult ProcessCommand(RemoveUserFromTenant command, string userId, bool isGlobalAdmin = false)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        CommandEnvelope envelope = CreateEnvelope(command, command.TenantId, userId, isGlobalAdmin);
        TenantState? state = GetOrDefaultTenantState(command.TenantId);
        DomainResult result = TenantAggregate.Handle(command, state, envelope);
        ApplyTenantEvents(command.TenantId, result);
        return result;
    }

    /// <summary>Processes a ChangeUserRole command.</summary>
    public DomainResult ProcessCommand(ChangeUserRole command, string userId, bool isGlobalAdmin = false)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        CommandEnvelope envelope = CreateEnvelope(command, command.TenantId, userId, isGlobalAdmin);
        TenantState? state = GetOrDefaultTenantState(command.TenantId);
        DomainResult result = TenantAggregate.Handle(command, state, envelope);
        ApplyTenantEvents(command.TenantId, result);
        return result;
    }

    /// <summary>Processes a SetTenantConfiguration command.</summary>
    public DomainResult ProcessCommand(SetTenantConfiguration command, string userId, bool isGlobalAdmin = false)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        CommandEnvelope envelope = CreateEnvelope(command, command.TenantId, userId, isGlobalAdmin);
        TenantState? state = GetOrDefaultTenantState(command.TenantId);
        DomainResult result = TenantAggregate.Handle(command, state, envelope);
        ApplyTenantEvents(command.TenantId, result);
        return result;
    }

    /// <summary>Processes a RemoveTenantConfiguration command.</summary>
    public DomainResult ProcessCommand(RemoveTenantConfiguration command, string userId, bool isGlobalAdmin = false)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        CommandEnvelope envelope = CreateEnvelope(command, command.TenantId, userId, isGlobalAdmin);
        TenantState? state = GetOrDefaultTenantState(command.TenantId);
        DomainResult result = TenantAggregate.Handle(command, state, envelope);
        ApplyTenantEvents(command.TenantId, result);
        return result;
    }

    // ─── Global Admin Commands (no envelope needed) ───

    /// <summary>Processes a BootstrapGlobalAdmin command.</summary>
    public DomainResult ProcessCommand(BootstrapGlobalAdmin command)
    {
        ArgumentNullException.ThrowIfNull(command);
        DomainResult result = GlobalAdministratorsAggregate.Handle(command, _globalAdminState);
        ApplyGlobalAdminEvents(result);
        return result;
    }

    /// <summary>Processes a SetGlobalAdministrator command.</summary>
    public DomainResult ProcessCommand(SetGlobalAdministrator command)
    {
        ArgumentNullException.ThrowIfNull(command);
        DomainResult result = GlobalAdministratorsAggregate.Handle(command, _globalAdminState);
        ApplyGlobalAdminEvents(result);
        return result;
    }

    /// <summary>Processes a RemoveGlobalAdministrator command.</summary>
    public DomainResult ProcessCommand(RemoveGlobalAdministrator command)
    {
        ArgumentNullException.ThrowIfNull(command);
        DomainResult result = GlobalAdministratorsAggregate.Handle(command, _globalAdminState);
        ApplyGlobalAdminEvents(result);
        return result;
    }

    // ─── Low-level envelope-based method (for Story 6.2 conformance tests) ───

    /// <summary>
    /// Processes a tenant command with an explicit envelope. Used by conformance tests that need
    /// to compare behavior with identical envelopes between InMemoryTenantService and TenantAggregate.
    /// </summary>
    /// <typeparam name="T">The command type.</typeparam>
    /// <param name="command">The command to process.</param>
    /// <param name="envelope">The command envelope.</param>
    /// <returns>The domain result.</returns>
    public DomainResult ProcessTenantCommand<T>(T command, CommandEnvelope envelope)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string aggregateId = envelope.AggregateId;
        TenantState? state = GetOrDefaultTenantState(aggregateId);

        DomainResult result = command switch
        {
            CreateTenant c => TenantAggregate.Handle(c, state),
            DisableTenant c => TenantAggregate.Handle(c, state),
            EnableTenant c => TenantAggregate.Handle(c, state),
            UpdateTenant c => TenantAggregate.Handle(c, state, envelope),
            AddUserToTenant c => TenantAggregate.Handle(c, state, envelope),
            RemoveUserFromTenant c => TenantAggregate.Handle(c, state, envelope),
            ChangeUserRole c => TenantAggregate.Handle(c, state, envelope),
            SetTenantConfiguration c => TenantAggregate.Handle(c, state, envelope),
            RemoveTenantConfiguration c => TenantAggregate.Handle(c, state, envelope),
            _ => throw new InvalidOperationException(
                $"Unknown tenant command type: {command.GetType().Name}. Update ProcessTenantCommand when adding new command types."),
        };

        ApplyTenantEvents(aggregateId, result);
        return result;
    }

    // ─── Internal helpers ───

    private static CommandEnvelope CreateEnvelope<T>(T command, string aggregateId, string userId, bool isGlobalAdmin)
        where T : notnull
    {
        return new CommandEnvelope(
            Guid.NewGuid().ToString(),
            SystemTenantId,
            DefaultDomain,
            aggregateId,
            typeof(T).Name,
            JsonSerializer.SerializeToUtf8Bytes(command),
            Guid.NewGuid().ToString(),
            null,
            userId,
            isGlobalAdmin
                ? new Dictionary<string, string> { [GlobalAdminExtensionKey] = "true" }
                : null);
    }

    private void ApplyGlobalAdminEvents(DomainResult result)
    {
        if (!result.IsSuccess)
        {
            return;
        }

        _globalAdminState ??= new GlobalAdministratorsState();

        foreach (IEventPayload evt in result.Events)
        {
            switch (evt)
            {
                case GlobalAdministratorSet e:
                    _globalAdminState.Apply(e);
                    break;
                case GlobalAdministratorRemoved e:
                    _globalAdminState.Apply(e);
                    break;
                default:
                    // Ignore unknown events safely to allow forward compatibility
                    break;
            }

            lock (_eventHistory)
            {
                _eventHistory.Add(evt);
            }
        }
    }

    private void ApplyTenantEvents(string tenantId, DomainResult result)
    {
        if (!result.IsSuccess)
        {
            return;
        }

        if (!_tenantStates.TryGetValue(tenantId, out TenantState? state))
        {
            state = new TenantState();
            _tenantStates[tenantId] = state;
        }

        foreach (IEventPayload evt in result.Events)
        {
            switch (evt)
            {
                case TenantCreated e:
                    state.Apply(e);
                    break;
                case TenantUpdated e:
                    state.Apply(e);
                    break;
                case TenantDisabled e:
                    state.Apply(e);
                    break;
                case TenantEnabled e:
                    state.Apply(e);
                    break;
                case UserAddedToTenant e:
                    state.Apply(e);
                    break;
                case UserRemovedFromTenant e:
                    state.Apply(e);
                    break;
                case UserRoleChanged e:
                    state.Apply(e);
                    break;
                case TenantConfigurationSet e:
                    state.Apply(e);
                    break;
                case TenantConfigurationRemoved e:
                    state.Apply(e);
                    break;
                default:
                    // Ignore unknown events safely to allow forward compatibility
                    break;
            }

            lock (_eventHistory)
            {
                _eventHistory.Add(evt);
            }
        }
    }

    private TenantState? GetOrDefaultTenantState(string tenantId)
        => _tenantStates.TryGetValue(tenantId, out TenantState? state) ? state : null;
}
