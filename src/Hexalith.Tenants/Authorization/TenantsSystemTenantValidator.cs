using System.Security.Claims;

using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.Contracts.Authorization;
using Hexalith.Tenants.Contracts.Identity;

namespace Hexalith.Tenants.Authorization;

internal sealed class TenantsSystemTenantValidator(ClaimsTenantValidator inner) : ITenantValidator {
    private const string TenantClaimType = "eventstore:tenant";

    public Task<TenantValidationResult> ValidateAsync(
        ClaimsPrincipal user,
        string tenantId,
        CancellationToken cancellationToken,
        string? aggregateId = null) {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrWhiteSpace(tenantId)) {
            return Task.FromResult(TenantValidationResult.Denied(
                "Tenant is required.",
                AuthorizationFailureReason.TenantMissing));
        }

        if (!string.Equals(tenantId, TenantIdentity.DefaultTenantId, StringComparison.Ordinal)) {
            return Task.FromResult(TenantValidationResult.Denied(
                $"Not authorized for tenant '{tenantId}'. Tenants protected endpoints require the platform tenant.",
                AuthorizationFailureReason.TenantMismatch));
        }

        var tenantClaims = user.FindAll(TenantClaimType)
            .Select(static c => c.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        if (tenantClaims.Count == 0) {
            return Task.FromResult(TenantValidationResult.Denied(
                $"No {TenantClaimType} authorization claims found. Access denied.",
                AuthorizationFailureReason.PrincipalNotMember));
        }

        if (!tenantClaims.Any(static value => string.Equals(value, TenantIdentity.DefaultTenantId, StringComparison.Ordinal))) {
            return Task.FromResult(TenantValidationResult.Denied(
                $"Not authorized for tenant '{TenantIdentity.DefaultTenantId}'.",
                AuthorizationFailureReason.TenantMismatch));
        }

        return inner.ValidateAsync(user, tenantId, cancellationToken, aggregateId);
    }
}
