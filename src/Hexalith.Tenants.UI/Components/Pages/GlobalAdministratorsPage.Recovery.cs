using Hexalith.Tenants.UI.Services;
using Hexalith.Tenants.UI.Services.Gateways;
using Microsoft.AspNetCore.Components.Authorization;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantCommands;

namespace Hexalith.Tenants.UI.Components.Pages;

public partial class GlobalAdministratorsPage
{
    private int _recoveryInFlight;

    private bool CanRecoverCommand(TenantAggregateCommandLease? lease)
        => !_disposed && IsAuthorized && Volatile.Read(ref _recoveryInFlight) == 0
            && lease is { IsDispatchMarked: true, IsReleased: false, IsReconciliationDispatchInFlight: false }
            && BffComposition.IsGlobalAdministratorStatusConnected && CommandGateway.SupportsCommandStatusLookup;

    private async Task<string?> ResolveRecoveryCallerSafelyAsync(
        CancellationToken cancellationToken = default, bool retainedCompletion = false)
    {
        CancellationTokenSource? linked = null;
        try
        {
            if (!retainedCompletion)
            {
                lock (_cancellationLock)
                {
                    if (_disposed) return null;
                    _authorizationCancellation ??= new CancellationTokenSource();
                    linked = CancellationTokenSource.CreateLinkedTokenSource(_authorizationCancellation.Token, cancellationToken);
                    cancellationToken = linked.Token;
                }
            }
            return await TenantUiOperationPolicy.FromServices(Services).RunAsync(
                token => BffComposition.ResolveGlobalAdministratorRecoveryCallerAsync(token).AsTask(), cancellationToken).ConfigureAwait(false);
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

    private Task RecoverGrantStatusAsync() => RecoverCommandStatusAsync(isGrant: true);

    private Task RecoverRemoveStatusAsync() => RecoverCommandStatusAsync(isGrant: false);

    private async Task RecoverCommandStatusAsync(bool isGrant)
    {
        TenantAggregateCommandLease? lease = isGrant ? _grantAdmissionLease : _removeAdmissionLease;
        GlobalAdministratorReconciliationState? basis = isGrant
            ? CreateReconciliation(_grantSnapshot) : CreateReconciliation(_removeSnapshot);
        if (!CanRecoverCommand(lease) || basis is not { IsSubmissionAmbiguous: true, CorrelationId: null }
            || Interlocked.CompareExchange(ref _recoveryInFlight, 1, 0) != 0)
        {
            return;
        }

        long authorizationVersion = Volatile.Read(ref _authorizationTransitionVersion);
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
            if (!await ReauthorizeAsync().ConfigureAwait(false)
                || lease?.RecoveryCallerSubject is not { Length: > 0 } caller
                || !string.Equals(caller, await ResolveRecoveryCallerSafelyAsync().ConfigureAwait(false), StringComparison.Ordinal)
                || authorizationVersion != Volatile.Read(ref _authorizationTransitionVersion)
                || !(isGrant ? ReferenceEquals(lease, _grantAdmissionLease) : ReferenceEquals(lease, _removeAdmissionLease))
                || !lease.TryBeginReconciliationDispatch(_fixedAggregateOwner, basis, out token))
            {
                await SurfaceRecoveryRefusalAsync(isGrant, lease).ConfigureAwait(false);
                return;
            }

            await RenderIfAliveAsync().ConfigureAwait(false);
            TenantCommandStatusResult result;
            try
            {
                result = await CommandGateway.RecoverGlobalAdministratorStatusAsync(basis.MessageId).ConfigureAwait(false);
            }
            catch
            {
                result = TenantCommandStatusResult.UnknownWithKey("Tenants.GlobalAdministrators.Recovery.Unverified");
            }

            // Re-resolve after I/O: another authorized caller must never inherit the original attempt.
            if (!string.Equals(caller, await ResolveRecoveryCallerSafelyAsync(callerLifetime.Token, retainedCompletion: true).ConfigureAwait(false), StringComparison.Ordinal))
            {
                await SurfaceRecoveryRefusalAsync(isGrant, lease).ConfigureAwait(false);
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
            // An authentication event can queue after the last principal read but before this dispatch.
            bool dispatched = await InvokeRendererSafelyAsync(() =>
            {
                if (authorizationVersion != Volatile.Read(ref _authorizationTransitionVersion)) return;
                _ = lease.TryCompleteReconciliationDispatch(token, completion);
                _ = SynchronizeOwnedGrantReconciliation();
                _ = SynchronizeOwnedRemoveReconciliation();
                StateHasChanged();
            }).ConfigureAwait(false);
            if (!dispatched && _disposed
                && string.Equals(caller, await ResolveRecoveryCallerSafelyAsync(callerLifetime.Token, retainedCompletion: true).ConfigureAwait(false), StringComparison.Ordinal))
            {
                // A replacement renderer observes the exact result after the original caller is rechecked.
                _ = lease.TryCompleteReconciliationDispatch(token, completion);
            }
        }
        finally
        {
            if (callerProvider is not null) callerProvider.AuthenticationStateChanged -= CallerChanged;
            if (token != 0)
            {
                _ = lease!.TryAbortReconciliationDispatch(token);
            }
            Volatile.Write(ref _recoveryInFlight, 0);
            await RenderIfAliveAsync().ConfigureAwait(false);
        }
    }

    private async Task SurfaceRecoveryRefusalAsync(bool isGrant, TenantAggregateCommandLease? lease)
        => _ = await InvokeRendererSafelyAsync(() =>
        {
            if (isGrant && ReferenceEquals(lease, _grantAdmissionLease) && _grantSnapshot.IsSubmissionAmbiguous)
            {
                SetGrantSnapshot(_grantSnapshot with
                {
                    SafeMessageKey = "Tenants.GlobalAdministrators.Recovery.Unverified",
                    SafeRecoveryKey = "Tenants.GlobalAdministrators.Recovery.Retained"
                });
            }
            else if (!isGrant && ReferenceEquals(lease, _removeAdmissionLease) && _removeSnapshot.IsSubmissionAmbiguous)
            {
                SetRemoveSnapshot(_removeSnapshot with
                {
                    SafeMessageKey = "Tenants.GlobalAdministrators.Recovery.Unverified",
                    SafeRecoveryKey = "Tenants.GlobalAdministrators.Recovery.Retained"
                });
            }
            StateHasChanged();
        }).ConfigureAwait(false);
}
