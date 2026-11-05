namespace Hexalith.Tenants.Authorization;

/// <summary>Explicit test-only enrollment; the normal host retains its production owner-source behavior.</summary>
internal static class TenantOwnerSourceReadDevelopmentEnrollment
{
    /// <summary>Enrolls only a source Debug helper selected by its owning Development host.</summary>
    internal static void Add(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
#if PRIVATE_OWNER_SOURCE_READ_PROOF
        Hexalith.EventStore.Testing.OwnerSourceRead.OwnerSourceReadServiceCollectionExtensions
            .AddPrivateOwnerSourceRead(services, configuration, environment);
#else
        if (configuration.GetValue<bool>("EventStore:Testing:OwnerSourceRead:Enabled"))
            throw new InvalidOperationException("Private owner source proof requires the explicitly selected Debug/source host build.");
#endif
    }
}
