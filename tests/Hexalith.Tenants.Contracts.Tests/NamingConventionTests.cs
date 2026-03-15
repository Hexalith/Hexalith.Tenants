using System.Reflection;

using Hexalith.EventStore.Contracts.Events;

using Shouldly;

namespace Hexalith.Tenants.Contracts.Tests;

public class NamingConventionTests
{
    private static readonly Assembly ContractsAssembly = typeof(Commands.CreateTenant).Assembly;

    private static readonly string[] CommandVerbPrefixes =
    [
        "Create",
        "Update",
        "Disable",
        "Enable",
        "Add",
        "Remove",
        "Change",
        "Set",
        "Bootstrap",
    ];

    private static readonly string[] EventSuffixes =
    [
        "Created",
        "Updated",
        "Disabled",
        "Enabled",
        "Added",
        "AddedToTenant",
        "Removed",
        "RemovedFromTenant",
        "Changed",
        "Set",
    ];

    [Fact]
    public void All_commands_follow_verb_target_naming()
    {
        IEnumerable<Type> commandTypes = ContractsAssembly
            .GetTypes()
            .Where(t =>
                t.IsClass
                && !t.IsAbstract
                && t.Namespace == "Hexalith.Tenants.Contracts.Commands");

        commandTypes.ShouldNotBeEmpty("No command types found in Contracts.Commands namespace");

        foreach (Type commandType in commandTypes)
        {
            bool startsWithVerb = CommandVerbPrefixes.Any(prefix => commandType.Name.StartsWith(prefix, StringComparison.Ordinal));
            startsWithVerb.ShouldBeTrue($"Command '{commandType.Name}' does not start with an allowed verb prefix: {string.Join(", ", CommandVerbPrefixes)}");
        }
    }

    [Fact]
    public void All_success_events_follow_target_past_verb_naming()
    {
        IEnumerable<Type> successEventTypes = ContractsAssembly
            .GetTypes()
            .Where(t =>
                t.IsClass
                && !t.IsAbstract
                && typeof(IEventPayload).IsAssignableFrom(t)
                && !typeof(IRejectionEvent).IsAssignableFrom(t)
                && t.Namespace == "Hexalith.Tenants.Contracts.Events");

        successEventTypes.ShouldNotBeEmpty("No success event types found in Contracts.Events namespace");

        foreach (Type eventType in successEventTypes)
        {
            bool endsWithVerb = EventSuffixes.Any(suffix => eventType.Name.EndsWith(suffix, StringComparison.Ordinal));
            endsWithVerb.ShouldBeTrue($"Event '{eventType.Name}' does not end with an allowed past-tense verb suffix: {string.Join(", ", EventSuffixes)}");
        }
    }

    [Fact]
    public void All_rejection_events_end_with_rejection_suffix()
    {
        IEnumerable<Type> rejectionTypes = ContractsAssembly
            .GetTypes()
            .Where(t =>
                t.IsClass
                && !t.IsAbstract
                && typeof(IRejectionEvent).IsAssignableFrom(t));

        rejectionTypes.ShouldNotBeEmpty("No rejection event types found");

        foreach (Type rejectionType in rejectionTypes)
        {
            rejectionType.Name.EndsWith("Rejection", StringComparison.Ordinal)
                .ShouldBeTrue($"Rejection event '{rejectionType.Name}' does not end with 'Rejection'");
        }
    }

    [Fact]
    public void All_event_types_have_tenant_id_property()
    {
        IEnumerable<Type> allEventTypes = ContractsAssembly
            .GetTypes()
            .Where(t =>
                t.IsClass
                && !t.IsAbstract
                && typeof(IEventPayload).IsAssignableFrom(t));

        allEventTypes.ShouldNotBeEmpty("No event types found");

        foreach (Type eventType in allEventTypes)
        {
            PropertyInfo? tenantIdProperty = eventType.GetProperty("TenantId");
            tenantIdProperty.ShouldNotBeNull($"Event type '{eventType.Name}' is missing 'TenantId' property");
            tenantIdProperty.PropertyType.ShouldBe(typeof(string), $"Event type '{eventType.Name}' has 'TenantId' but it is not of type string");
        }
    }
}
