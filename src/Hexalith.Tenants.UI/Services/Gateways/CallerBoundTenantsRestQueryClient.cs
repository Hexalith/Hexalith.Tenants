using System.Collections.Concurrent;
using Hexalith.Tenants.Contracts.Queries;

using Microsoft.AspNetCore.Components.Authorization;

namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>Guards every asynchronous read and recovery against an immutable authenticated caller.</summary>
internal sealed class CallerBoundTenantsRestQueryClient : ITenantsRestQueryClient, IDisposable
{
    private readonly ITenantsRestQueryClient _client;
    private readonly Func<bool> _isCurrentCaller;
    private readonly AuthenticationStateProvider? _provider;
    private int _rejected;
    private readonly ConcurrentBag<Task> _reads = [];

    /// <summary>Subscribes the immutable read guard to its authoritative authentication source.</summary>
    /// <param name="client">The existing REST transport.</param>
    /// <param name="isCurrentCaller">The captured authenticated subject and authorization revision check.</param>
    /// <param name="provider">The authoritative circuit provider, when available.</param>
    internal CallerBoundTenantsRestQueryClient(
        ITenantsRestQueryClient client, Func<bool> isCurrentCaller, AuthenticationStateProvider? provider = null)
    {
        _client = client;
        _isCurrentCaller = isCurrentCaller;
        _provider = provider;
        if (_provider is not null)
        {
            _provider.AuthenticationStateChanged += OnAuthenticationStateChanged;
        }
    }

    /// <summary>Gets whether every observed identity has remained authenticated and unchanged.</summary>
    internal bool IsCallerVerified => Volatile.Read(ref _rejected) == 0;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_provider is not null)
        {
            _provider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        }
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> state)
        => Interlocked.Exchange(ref _rejected, 1);

    /// <inheritdoc />
    public Task<TenantsRestQueryResponse<PaginatedResult<TenantSummary>>> ListTenantsAsync(
        ListTenantsQuery query, string? eTag, CancellationToken cancellationToken = default)
        => ReadAsync(() => _client.ListTenantsAsync(query, eTag, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<TenantsRestQueryResponse<TenantDetail>> GetTenantAsync(
        GetTenantQuery query, string? eTag, CancellationToken cancellationToken = default)
        => ReadAsync(() => _client.GetTenantAsync(query, eTag, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<TenantsRestQueryResponse<PaginatedResult<TenantMember>>> GetTenantUsersAsync(
        GetTenantUsersQuery query, string? eTag, CancellationToken cancellationToken = default)
        => ReadAsync(() => _client.GetTenantUsersAsync(query, eTag, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<TenantsRestQueryResponse<PaginatedResult<UserTenantMembership>>> GetUserTenantsAsync(
        GetUserTenantsQuery query, string? eTag, CancellationToken cancellationToken = default)
        => ReadAsync(() => _client.GetUserTenantsAsync(query, eTag, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<TenantsRestQueryResponse<PaginatedResult<TenantAuditEntry>>> GetTenantAuditAsync(
        GetTenantAuditQuery query, string? eTag, CancellationToken cancellationToken = default)
        => ReadAsync(() => _client.GetTenantAuditAsync(query, eTag, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<TenantsRestQueryResponse<PaginatedResult<GlobalAdministratorSummary>>> GetGlobalAdministratorsAsync(
        GetGlobalAdministratorsQuery query, string? eTag, CancellationToken cancellationToken = default)
        => ReadAsync(() => _client.GetGlobalAdministratorsAsync(query, eTag, cancellationToken), cancellationToken);

    /// <summary>Guards an asynchronous dependency, including its exceptional completion.</summary>
    /// <typeparam name="T">The dependency result.</typeparam>
    /// <param name="read">The original dependency operation.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>The verified result.</returns>
    internal async Task<T> ReadAsync<T>(Func<Task<T>> read, CancellationToken cancellationToken)
    {
        VerifyCaller(cancellationToken);
        T result;
        try
        {
            Task<T> dependency = read();
            _reads.Add(dependency);
            result = await dependency.ConfigureAwait(false);
        }
        catch (Exception exception) when (!TenantWorkspaceReadDiagnostics.IsFatal(exception))
        {
            VerifyCaller(cancellationToken);
            throw;
        }
        VerifyCaller(cancellationToken);
        return result;
    }

    /// <summary>Preserves fatal dependencies already completed when a caller or deadline cancellation wins the await.</summary>
    internal void ThrowIfFatal()
    {
        foreach (Task dependency in _reads) TenantWorkspaceReadDiagnostics.ThrowIfFatal(dependency);
    }

    private void VerifyCaller(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsCallerVerified || !_isCurrentCaller())
        {
            Volatile.Write(ref _rejected, 1);
            throw new UnauthorizedAccessException("Authenticated caller could not be verified.");
        }
    }
}
