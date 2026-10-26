using Hexalith.Tenants.UI.Services;
using Hexalith.Tenants.UI.Services.Gateways;
using Microsoft.AspNetCore.Components.Authorization;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantAudit;

namespace Hexalith.Tenants.UI.Components.Tenants.Audit;

public partial class GlobalAdministratorCorrectionPanel
{
    private int _recoveryInFlight;
    private readonly object _recoveryCallerSync = new();
    private CancellationTokenSource? _recoveryCallerCancellation;

    private void CancelRecoveryCallerReads()
    {
        lock (_recoveryCallerSync)
        {
            _recoveryCallerCancellation?.Cancel();
            _recoveryCallerCancellation?.Dispose();
            _recoveryCallerCancellation = null;
        }
    }


    private bool CanRecoverCommand
        => !_disposed && Volatile.Read(ref _recoveryInFlight) == 0
            && _authorizationReflection is TenantLifecycleAuthorizationReflectionState.Authorized
            && _admissionLease is { IsDispatchMarked: true, IsReleased: false, IsReconciliationDispatchInFlight: false }
            && _bffComposition?.IsGlobalAdministratorStatusConnected == true && CommandGateway?.SupportsCommandStatusLookup == true;

    private async Task<string?> ResolveRecoveryCallerSafelyAsync(
        CancellationToken cancellationToken = default, bool retainedCompletion = false)
    {
        CancellationTokenSource? linked = null;
        try
        {
            if (!retainedCompletion)
            {
                lock (_recoveryCallerSync)
                {
                    if (_disposed) return null;
                    _recoveryCallerCancellation ??= new CancellationTokenSource();
                    linked = CancellationTokenSource.CreateLinkedTokenSource(_recoveryCallerCancellation.Token, cancellationToken);
                    cancellationToken = linked.Token;
                }
            }
            return await TenantUiOperationPolicy.FromServices(Services).RunAsync(
                token => _bffComposition!.ResolveGlobalAdministratorRecoveryCallerAsync(token).AsTask(), cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
        finally
        {
            linked?.Dispose();
        }
    }

    private async Task RecoverStatusAsync()
    {
        var snapshot = _snapshot;
        TenantAggregateCommandLease? lease = _admissionLease;
        GlobalAdministratorReconciliationState? basis = snapshot?.ToReconciliation();
        if (!CanRecoverCommand || basis is not { IsSubmissionAmbiguous: true, CorrelationId: null }
            || Interlocked.CompareExchange(ref _recoveryInFlight, 1, 0) != 0)
        {
            return;
        }

        long generation = Volatile.Read(ref _operationGeneration);
        long authorizationVersion = Volatile.Read(ref _authorizationGeneration);
        using var callerLifetime = new CancellationTokenSource();
        AuthenticationStateProvider? callerProvider = _authenticationStateProvider;
        void CallerChanged(Task<AuthenticationState> state)
        {
            try { _ = callerLifetime.CancelAsync(); }
            catch (ObjectDisposedException) { /* A queued event can outlive unsubscription. */ }
        }
        if (callerProvider is not null) callerProvider.AuthenticationStateChanged += CallerChanged;
        long token = 0;
        try
        {
            if (!await ReauthorizeAsync(generation).ConfigureAwait(false)
                || lease?.RecoveryCallerSubject is not { Length: > 0 } caller
                || !string.Equals(caller, await ResolveRecoveryCallerSafelyAsync().ConfigureAwait(false), StringComparison.Ordinal)
                || authorizationVersion != Volatile.Read(ref _authorizationGeneration)
                || !ReferenceEquals(snapshot, _snapshot) || !ReferenceEquals(lease, _admissionLease)
                || !lease.TryBeginReconciliationDispatch(_admissionOwner, basis, out token))
            {
                await SurfaceRecoveryRefusalAsync(snapshot, lease).ConfigureAwait(false);
                return;
            }

            TenantCommandStatusResult result;
            try
            {
                result = await CommandGateway!.RecoverGlobalAdministratorStatusAsync(basis.MessageId).ConfigureAwait(false);
            }
            catch
            {
                result = TenantCommandStatusResult.UnknownWithKey("Tenants.GlobalAdministrators.Recovery.Unverified");
            }

            if (!string.Equals(caller, await ResolveRecoveryCallerSafelyAsync(callerLifetime.Token, retainedCompletion: true).ConfigureAwait(false), StringComparison.Ordinal))
            {
                await SurfaceRecoveryRefusalAsync(snapshot, lease).ConfigureAwait(false);
                return;
            }

            GlobalAdministratorReconciliationState completion = result.RecoveredCorrelationId is { Length: > 0 } correlation
                && result.HasVerifiedCommandIdentity
                ? basis with
                {
                    CorrelationId = correlation,
                    LifecycleState = TenantCommandLifecycleState.Accepted,
                    IsSubmissionAmbiguous = false,
                    SafeMessageKey = null,
                    SafeRecoveryKey = null
                }
                : basis with
                {
                    SafeMessageKey = "Tenants.GlobalAdministrators.Recovery.Unverified",
                    SafeRecoveryKey = "Tenants.GlobalAdministrators.Recovery.Retained"
                };
            bool dispatched = await InvokeRendererSafelyAsync(() =>
            {
                if (authorizationVersion != Volatile.Read(ref _authorizationGeneration)) return;
                _ = lease.TryCompleteReconciliationDispatch(token, completion);
                SynchronizeOwnedReconciliation();
                StateHasChanged();
            }).ConfigureAwait(false);
            if (!dispatched && _disposed
                && string.Equals(caller, await ResolveRecoveryCallerSafelyAsync(callerLifetime.Token, retainedCompletion: true).ConfigureAwait(false), StringComparison.Ordinal))
            {
                _ = lease.TryCompleteReconciliationDispatch(token, completion);
            }
            await DrainAdoptedReconciliationAsync().ConfigureAwait(false);
        }
        finally
        {
            if (callerProvider is not null) callerProvider.AuthenticationStateChanged -= CallerChanged;
            if (token != 0)
            {
                _ = lease!.TryAbortReconciliationDispatch(token);
            }
            Volatile.Write(ref _recoveryInFlight, 0);
            _ = await InvokeRendererSafelyAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    private async Task SurfaceRecoveryRefusalAsync(GlobalAdministratorCorrectionSnapshot? snapshot,
        TenantAggregateCommandLease? lease)
        => _ = await InvokeRendererSafelyAsync(() =>
        {
            if (snapshot is not null && ReferenceEquals(snapshot, _snapshot) && ReferenceEquals(lease, _admissionLease))
            {
                SetSnapshot(snapshot with
                {
                    SafeMessageKey = "Tenants.GlobalAdministrators.Recovery.Unverified",
                    SafeRecoveryKey = "Tenants.GlobalAdministrators.Recovery.Retained"
                }, focusTerminalState: true);
                StateHasChanged();
            }
        }).ConfigureAwait(false);
}
