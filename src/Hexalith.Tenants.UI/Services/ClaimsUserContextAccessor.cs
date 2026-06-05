using System.Security.Claims;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.Tenants.UI.Services;

internal sealed class ClaimsUserContextAccessor(IHttpContextAccessor httpContextAccessor) : IUserContextAccessor
{
    public string? TenantId => FindFirstNonEmpty("tenant_id", "tenantId", "tid", "tenant");

    public string? UserId => FindFirstNonEmpty(ClaimTypes.NameIdentifier, "sub", "user_id", "userId");

    private string? FindFirstNonEmpty(params string[] claimTypes)
    {
        ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) {
            return null;
        }

        foreach (string claimType in claimTypes) {
            string? value = user.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value)) {
                return value;
            }
        }

        return null;
    }
}
