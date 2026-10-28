namespace Hexalith.Tenants.Authorization;

/// <summary>Verifies global membership from the fixed authenticated native source, never a projection.</summary>
public interface ITenantsGlobalAdministratorAuthority
{
    /// <summary>Returns membership at the final verified source observation; unavailable evidence denies access.</summary>
    /// <param name="userId">The authenticated principal.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>Whether the principal currently has source membership.</returns>
    Task<bool> IsCurrentGlobalAdministratorAsync(string userId, CancellationToken cancellationToken);
}
