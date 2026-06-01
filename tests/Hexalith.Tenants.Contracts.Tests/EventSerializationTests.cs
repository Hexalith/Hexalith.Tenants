using System.Reflection;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;

using Shouldly;

namespace Hexalith.Tenants.Contracts.Tests;

public class EventSerializationTests {
    public static IEnumerable<object[]> EventPayloadTypes() {
        Assembly contractsAssembly = typeof(Commands.CreateTenant).Assembly;
        IEnumerable<Type> eventTypes = contractsAssembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IEventPayload).IsAssignableFrom(t))
            .OrderBy(t => t.FullName);

        foreach (Type eventType in eventTypes) {
            IEventPayload instance = CreateTestInstance(eventType);
            yield return [eventType, instance];
        }
    }

    [Theory]
    [MemberData(nameof(EventPayloadTypes))]
    public void Event_serialization_roundtrip_preserves_equality(Type eventType, IEventPayload expected) {
        string json = JsonSerializer.Serialize(expected, eventType);
        object? deserialized = JsonSerializer.Deserialize(json, eventType);

        _ = deserialized.ShouldNotBeNull();
        deserialized.ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(EventPayloadTypes))]
    public void Event_payload_contract_exposes_top_level_TenantId(Type eventType, IEventPayload payload) {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(payload);

        PropertyInfo? tenantId = eventType.GetProperty(
            "TenantId",
            BindingFlags.Instance | BindingFlags.Public);

        _ = tenantId.ShouldNotBeNull($"{eventType.FullName} must expose top-level TenantId for consumers.");
        tenantId.PropertyType.ShouldBe(typeof(string));
    }

    [Fact]
    public void UserRemovedFromTenant_payload_contains_only_tenant_and_user_ids() {
        string[] propertyNames = typeof(UserRemovedFromTenant)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        propertyNames.ShouldBe(["TenantId", "UserId"]);
    }

    [Fact]
    public void ConfigurationLimitExceededRejection_payload_contains_only_structured_limit_fields() {
        string[] propertyNames = typeof(ConfigurationLimitExceededRejection)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        propertyNames.ShouldBe(["CurrentCount", "LimitType", "MaxAllowed", "TenantId"]);
    }

    private static IEventPayload CreateTestInstance(Type eventType) {
        ConstructorInfo? ctor = eventType.GetConstructors().FirstOrDefault();
        _ = ctor.ShouldNotBeNull($"Type {eventType.Name} has no public constructor");

        ParameterInfo[] parameters = ctor.GetParameters();
        object?[] args = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++) {
            args[i] = GetTestValue(parameters[i]);
        }

        return (IEventPayload)ctor.Invoke(args);
    }

    private static object? GetTestValue(ParameterInfo parameter) {
        Type paramType = parameter.ParameterType;
        string name = parameter.Name ?? string.Empty;

        if (paramType == typeof(string)) {
            return name switch {
                "TenantId" => "tenant-abc",
                "UserId" => "user-xyz",
                "ActorUserId" => "actor-user-123",
                "Name" => "Test Tenant Name",
                "Description" => "Test description",
                "Key" => "config-key-1",
                "Value" => "config-value-1",
                "LimitType" => "max-configs",
                _ => $"test-{name}",
            };
        }

        if (paramType == typeof(DateTimeOffset)) {
            return DateTimeOffset.Parse("2026-01-15T10:30:00+00:00");
        }

        if (paramType == typeof(TenantRole)) {
            return TenantRole.TenantContributor;
        }

        if (paramType == typeof(TenantStatus)) {
            return TenantStatus.Disabled;
        }

        if (paramType == typeof(int)) {
            return name switch {
                "CurrentCount" => 42,
                "MaxAllowed" => 100,
                _ => 1,
            };
        }

        if (Nullable.GetUnderlyingType(paramType) == typeof(TenantRole)) {
            return TenantRole.TenantReader;
        }

        if (Nullable.GetUnderlyingType(paramType) == typeof(string)) {
            return "nullable-test-value";
        }

        throw new NotSupportedException($"No test value configured for parameter '{name}' of type {paramType.Name}");
    }
}
