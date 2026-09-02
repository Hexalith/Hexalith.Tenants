namespace Hexalith.Tenants.Validation;

/// <summary>Defines the literal global-administrator identity boundary shared by command validators.</summary>
internal static class GlobalAdministratorIdentity
{
    /// <summary>Gets the maximum supported literal user identifier length.</summary>
    internal const int MaximumLength = 256;

    /// <summary>Returns whether a literal user identifier is supported without normalization.</summary>
    /// <param name="userId">Literal user identifier.</param>
    /// <returns><see langword="true"/> when the identifier satisfies the fixed boundary.</returns>
    internal static bool IsSupported(string? userId)
        => !string.IsNullOrWhiteSpace(userId)
            && userId.Length <= MaximumLength
            && !userId.Any(char.IsControl);
}
