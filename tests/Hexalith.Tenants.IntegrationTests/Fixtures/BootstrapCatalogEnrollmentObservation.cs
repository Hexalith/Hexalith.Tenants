namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Support-safe facts from an actual resource snapshot and its private catalog coordination files.</summary>
/// <param name="SnapshotObserved">Whether Aspire supplied a snapshot for this logical resource or an allocated instance.</param>
/// <param name="EnvironmentObserved">Whether that snapshot contains rendered environment entries.</param>
/// <param name="DevelopmentEnvironment">Whether the resource selects the Development environment.</param>
/// <param name="DirectoryConfigured">Whether the bootstrap directory setting is present.</param>
/// <param name="CatalogConfigured">Whether the catalog path setting is present.</param>
/// <param name="ComponentConfigured">Whether the authority component path setting is present.</param>
/// <param name="AccessControlConfigured">Whether the receiver ACL path setting is present.</param>
/// <param name="TechnicalBindingV2">Whether the resource selects binding version two.</param>
/// <param name="HostIdentityMatches">Whether the configured physical host id matches its known resource name.</param>
/// <param name="PrivateDirectoryObserved">Whether the directory is a real private catalog child of this profile's temporary root.</param>
/// <param name="CatalogFilePresent">Whether that directory contains the configured regular catalog file.</param>
/// <param name="ComponentFilePresent">Whether the configured component is a regular file in that directory.</param>
/// <param name="AccessControlFilePresent">Whether the configured ACL is a regular file in that directory.</param>
/// <param name="OwnObservationPresent">Whether the host has written its independent observation.</param>
/// <param name="ObservedHostCount">How many of the four known required hosts have written observations.</param>
/// <param name="ReadyHostCount">How many of those hosts have written ready attestations.</param>
/// <param name="CommittedHostCount">How many of those hosts have written committed readbacks.</param>
/// <param name="ActivationPresent">Whether the coordinator has written an activation proposal.</param>
/// <param name="BootstrapReceiptPresent">Whether the owned business bootstrap has written its receipt.</param>
internal sealed record BootstrapCatalogEnrollmentObservation(
    bool SnapshotObserved,
    bool EnvironmentObserved,
    bool DevelopmentEnvironment,
    bool DirectoryConfigured,
    bool CatalogConfigured,
    bool ComponentConfigured,
    bool AccessControlConfigured,
    bool TechnicalBindingV2,
    bool HostIdentityMatches,
    bool PrivateDirectoryObserved,
    bool CatalogFilePresent,
    bool ComponentFilePresent,
    bool AccessControlFilePresent,
    bool OwnObservationPresent,
    int ObservedHostCount,
    int ReadyHostCount,
    int CommittedHostCount,
    bool ActivationPresent,
    bool BootstrapReceiptPresent);
