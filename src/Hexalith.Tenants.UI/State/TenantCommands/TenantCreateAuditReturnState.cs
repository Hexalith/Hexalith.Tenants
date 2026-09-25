using Microsoft.AspNetCore.Components.Authorization;

namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>
/// Holds a create command's visible state for one audit round trip in the current circuit.
/// </summary>
public sealed class TenantCreateAuditReturnState : IDisposable
{
    private readonly AuthenticationStateProvider? _authenticationStateProvider;
    private readonly Lock _sync = new();
    private (TenantCreateCommandSnapshot Snapshot, string? TenantId, string? Name, string? Description)? _pending;

    /// <summary>
    /// Creates circuit-local return state and clears it as soon as the caller changes.
    /// </summary>
    /// <param name="services">The circuit service provider.</param>
    public TenantCreateAuditReturnState(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _authenticationStateProvider = services.GetService(typeof(AuthenticationStateProvider)) as AuthenticationStateProvider;
        if (_authenticationStateProvider is not null)
        {
            _authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
        }
    }

    /// <summary>
    /// Retains one command state immediately before its audit link is followed.
    /// </summary>
    public void Remember(TenantCreateCommandSnapshot snapshot, string? tenantId, string? name, string? description)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (_sync)
        {
            _pending = (snapshot, tenantId, name, description);
        }
    }

    /// <summary>
    /// Consumes the pending state after the matching audit return.
    /// </summary>
    public (TenantCreateCommandSnapshot Snapshot, string? TenantId, string? Name, string? Description)? Take()
    {
        lock (_sync)
        {
            var pending = _pending;
            _pending = null;
            return pending;
        }
    }

    /// <summary>
    /// Discards the pending state when the caller changes or the circuit ends.
    /// </summary>
    public void Clear()
    {
        lock (_sync)
        {
            _pending = null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_authenticationStateProvider is not null)
        {
            _authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        }

        Clear();
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> _)
        => Clear();
}
