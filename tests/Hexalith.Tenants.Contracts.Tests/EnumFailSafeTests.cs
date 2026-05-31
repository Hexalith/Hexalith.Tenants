using System.Text.Json;

using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Queries;

using Shouldly;

namespace Hexalith.Tenants.Contracts.Tests;

/// <summary>
/// Fail-safe serialization tests for the role/status enums (TEN-1 / TEN-2): missing fields
/// deserialize to the non-privileged <c>Unknown</c> sentinel, unrecognized names fail closed,
/// and values serialize by name (not as integers).
/// </summary>
public sealed class EnumFailSafeTests {
    [Fact]
    public void Enum_zero_values_are_non_privileged_sentinels() {
        default(TenantRole).ShouldBe(TenantRole.Unknown);
        default(TenantStatus).ShouldBe(TenantStatus.Unknown);
    }

    [Fact]
    public void UserAddedToTenant_with_missing_role_deserializes_to_Unknown() {
        const string json = """{"TenantId":"acme","UserId":"alice"}""";

        UserAddedToTenant? evt = JsonSerializer.Deserialize<UserAddedToTenant>(json);

        _ = evt.ShouldNotBeNull();
        evt.Role.ShouldBe(TenantRole.Unknown);
    }

    [Fact]
    public void UserAddedToTenant_with_unrecognized_role_name_fails_closed() {
        const string json = """{"TenantId":"acme","UserId":"alice","Role":"Superuser"}""";

        _ = Should.Throw<JsonException>(() => JsonSerializer.Deserialize<UserAddedToTenant>(json));
    }

    [Fact]
    public void AddUserToTenant_with_missing_role_deserializes_to_Unknown() {
        const string json = """{"TenantId":"acme","UserId":"alice"}""";

        AddUserToTenant? command = JsonSerializer.Deserialize<AddUserToTenant>(json);

        _ = command.ShouldNotBeNull();
        command.Role.ShouldBe(TenantRole.Unknown);
    }

    [Fact]
    public void AddUserToTenant_with_unrecognized_role_name_fails_closed() {
        const string json = """{"TenantId":"acme","UserId":"alice","Role":"GlobalAdministrator"}""";

        _ = Should.Throw<JsonException>(() => JsonSerializer.Deserialize<AddUserToTenant>(json));
    }

    [Fact]
    public void AddUserToTenant_role_round_trips_by_name() {
        var expected = new AddUserToTenant("acme", "alice", TenantRole.TenantContributor);

        string json = JsonSerializer.Serialize(expected);
        AddUserToTenant? actual = JsonSerializer.Deserialize<AddUserToTenant>(json);

        json.ShouldContain("\"TenantContributor\"");
        _ = actual.ShouldNotBeNull();
        actual.ShouldBe(expected);
    }

    [Fact]
    public void TenantRole_serializes_by_name_not_integer() {
        string json = JsonSerializer.Serialize(new UserAddedToTenant("acme", "alice", TenantRole.TenantOwner));

        json.ShouldContain("\"TenantOwner\"");
        json.ShouldNotContain("\"Role\":1");
        json.ShouldNotContain("\"Role\":0");
    }

    [Fact]
    public void TenantSummary_with_missing_status_deserializes_to_Unknown() {
        const string json = """{"TenantId":"acme","Name":"Acme"}""";

        TenantSummary? summary = JsonSerializer.Deserialize<TenantSummary>(json);

        _ = summary.ShouldNotBeNull();
        summary.Status.ShouldBe(TenantStatus.Unknown);
    }

    [Fact]
    public void TenantSummary_with_unrecognized_status_name_deserializes_to_Unknown() {
        const string json = """{"TenantId":"acme","Name":"Acme","Status":"Suspended"}""";

        TenantSummary? summary = JsonSerializer.Deserialize<TenantSummary>(json);

        _ = summary.ShouldNotBeNull();
        summary.Status.ShouldBe(TenantStatus.Unknown);
    }

    [Fact]
    public void TenantStatus_serializes_by_name_not_integer() {
        string json = JsonSerializer.Serialize(new TenantSummary("acme", "Acme", TenantStatus.Active));

        json.ShouldContain("\"Active\"");
        json.ShouldNotContain("\"Status\":0");
        json.ShouldNotContain("\"Status\":1");
    }
}
