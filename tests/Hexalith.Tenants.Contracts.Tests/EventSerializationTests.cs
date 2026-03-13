using System.Reflection;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Contracts.Enums;

using Shouldly;

namespace Hexalith.Tenants.Contracts.Tests;

public class EventSerializationTests
{
    public static IEnumerable<object[]> EventPayloadTypes()
    {
        Assembly contractsAssembly = typeof(Commands.CreateTenant).Assembly;
        IEnumerable<Type> eventTypes = contractsAssembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IEventPayload).IsAssignableFrom(t))
            .OrderBy(t => t.FullName);

        foreach (Type eventType in eventTypes)
        {
            IEventPayload instance = CreateTestInstance(eventType);
            yield return [eventType, instance];
        }
    }

    [Theory]
    [MemberData(nameof(EventPayloadTypes))]
    public void Event_serialization_roundtrip_preserves_equality(Type eventType, IEventPayload expected)
    {
        string json = JsonSerializer.Serialize(expected, eventType);
        object? deserialized = JsonSerializer.Deserialize(json, eventType);

        deserialized.ShouldNotBeNull();
        deserialized.ShouldBe(expected);
    }

    private static IEventPayload CreateTestInstance(Type eventType)
    {
        ConstructorInfo? ctor = eventType.GetConstructors().FirstOrDefault();
        ctor.ShouldNotBeNull($"Type {eventType.Name} has no public constructor");

        ParameterInfo[] parameters = ctor.GetParameters();
        object?[] args = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            args[i] = GetTestValue(parameters[i]);
        }

        return (IEventPayload)ctor.Invoke(args);
    }

    private static object? GetTestValue(ParameterInfo parameter)
    {
        Type paramType = parameter.ParameterType;
        string name = parameter.Name ?? string.Empty;

        if (paramType == typeof(string))
        {
            return name switch
            {
                "TenantId" => "tenant-abc",
                "UserId" => "user-xyz",
                "Name" => "Test Tenant Name",
                "Description" => "Test description",
                "Key" => "config-key-1",
                "Value" => "config-value-1",
                "LimitType" => "max-configs",
                _ => $"test-{name}",
            };
        }

        if (paramType == typeof(DateTimeOffset))
        {
            return DateTimeOffset.Parse("2026-01-15T10:30:00+00:00");
        }

        if (paramType == typeof(TenantRole))
        {
            return TenantRole.TenantContributor;
        }

        if (paramType == typeof(int))
        {
            return name switch
            {
                "CurrentCount" => 42,
                "MaxAllowed" => 100,
                _ => 1,
            };
        }

        if (Nullable.GetUnderlyingType(paramType) == typeof(string))
        {
            return "nullable-test-value";
        }

        throw new NotSupportedException($"No test value configured for parameter '{name}' of type {paramType.Name}");
    }
}
