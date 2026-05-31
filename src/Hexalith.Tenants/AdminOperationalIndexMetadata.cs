using System.Reflection;

using Hexalith.EventStore.Client.Discovery;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Indexes;

namespace Hexalith.Tenants;

internal static class AdminOperationalIndexMetadata
{
    public static AdminOperationalIndexMetadataResponse Create(
        DiscoveryResult discovery,
        IReadOnlyList<string>? requestedDomains)
    {
        ArgumentNullException.ThrowIfNull(discovery);

        HashSet<string> requested = requestedDomains is { Count: > 0 }
            ? new HashSet<string>(requestedDomains, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(
                discovery.Aggregates.Select(static a => a.DomainName)
                    .Concat(discovery.Projections.Select(static p => p.DomainName)),
                StringComparer.OrdinalIgnoreCase);

        List<AdminOperationalIndexDomainMetadata> domains = [.. requested
            .Select(domain => CreateDomainMetadata(domain, discovery))
            .Where(static d => d is not null)
            .Select(static d => d!)
            .OrderBy(static d => d.Domain, StringComparer.OrdinalIgnoreCase)];

        return new AdminOperationalIndexMetadataResponse(domains);
    }

    private static AdminOperationalIndexDomainMetadata? CreateDomainMetadata(string domain, DiscoveryResult discovery)
    {
        List<DiscoveredDomain> aggregates = [.. discovery.Aggregates
            .Where(a => a.DomainName.Equals(domain, StringComparison.OrdinalIgnoreCase))];
        List<DiscoveredDomain> projections = [.. discovery.Projections
            .Where(p => p.DomainName.Equals(domain, StringComparison.OrdinalIgnoreCase))];

        if (aggregates.Count == 0 && projections.Count == 0)
        {
            return null;
        }

        List<string> commandTypes = [.. aggregates
            .SelectMany(static a => DiscoverCommandTypes(a.Type))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];
        List<string> eventTypes = [.. aggregates
            .SelectMany(static a => DiscoverEventTypes(a.StateType, typeof(IEventPayload)))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];
        List<string> rejectionEventTypes = [.. aggregates
            .SelectMany(static a => DiscoverEventTypes(a.StateType, typeof(IRejectionEvent)))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];
        List<string> aggregateTypes = [.. aggregates
            .Select(static a => a.Type.FullName ?? a.Type.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];
        List<string> projectionNames = [.. projections
            .Select(static p => p.DomainName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)];

        return new AdminOperationalIndexDomainMetadata(
            domain,
            eventTypes,
            rejectionEventTypes,
            commandTypes,
            aggregateTypes,
            projectionNames);
    }

    private static IEnumerable<string> DiscoverCommandTypes(Type aggregateType)
        => aggregateType
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(static m => m.Name == "Handle" && IsDomainResultReturn(m.ReturnType))
            .Select(static m => m.GetParameters().FirstOrDefault()?.ParameterType)
            .Where(static t => t is not null)
            .Select(static t => t!.FullName ?? t.Name);

    private static IEnumerable<string> DiscoverEventTypes(Type stateType, Type eventType)
        => stateType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(static m => m.Name == "Apply" && m.GetParameters().Length == 1)
            .Select(static m => m.GetParameters()[0].ParameterType)
            .Where(eventType.IsAssignableFrom)
            .Select(static t => t.FullName ?? t.Name);

    private static bool IsDomainResultReturn(Type returnType)
        => returnType == typeof(DomainResult)
            || returnType == typeof(Task<DomainResult>);
}
