using Hexalith.EventStore.DomainService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hexalith.Tenants.Authorization;

/// <summary>Rebuilds command hints from native evidence and strips query hints before independent authorization.</summary>
/// <param name="authority">The request-scoped native authority for tenant commands.</param>
/// <param name="httpContextAccessor">The server-matched operational endpoint, when available.</param>
public sealed class TenantsGlobalAdministratorVerifier(ITenantsGlobalAdministratorAuthority authority, IHttpContextAccessor? httpContextAccessor = null) : IDomainServiceAdministratorVerifier
{
    /// <inheritdoc/>
    public Task<bool> IsCurrentGlobalAdministratorAsync(DomainServiceAdministratorClaim claim, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(claim);
        cancellationToken.ThrowIfCancellationRequested();
        // Global management commands already authorize from their locked replayed aggregate state.
        // Never honor their wire hint: reading that same actor during /process would self-deadlock.
        // Global queries still run their independent native-source gate in the query handler.
        // Query handlers establish their own permissions. Discard their unused hint before a global
        // source outage can preempt an independently authorized owner. Only matched server metadata
        // identifies this operation; a caller-supplied path or claim never selects the shortcut.
        return IsCanonicalQueryEndpoint() || string.Equals(claim.Domain, "global-administrators", StringComparison.Ordinal)
            ? Task.FromResult(false)
            : authority.IsCurrentGlobalAdministratorAsync(claim.UserId, cancellationToken);
    }

    private bool IsCanonicalQueryEndpoint()
    {
        HttpContext? context = httpContextAccessor?.HttpContext;
        return context is not null && HttpMethods.IsPost(context.Request.Method)
            && context.GetEndpoint() is RouteEndpoint endpoint && endpoint.RoutePattern.RawText == "/query"
            && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods is { Count: 1 } methods
            && methods[0] == HttpMethods.Post
            && endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Any(data => data.Policy == EventStoreDomainServicePolicies.Query);
    }
}
