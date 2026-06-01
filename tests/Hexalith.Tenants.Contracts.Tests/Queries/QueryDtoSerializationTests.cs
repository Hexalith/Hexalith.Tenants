using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Contracts.Serialization;

using Shouldly;

namespace Hexalith.Tenants.Contracts.Tests.Queries;

public class QueryDtoSerializationTests {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new TenantStatusJsonConverter(), new JsonStringEnumConverter() },
    };

    [Fact]
    public void TenantDetail_round_trip_preserves_all_properties() {
        TenantDetail original = new(
            TenantId: "tenant-1",
            Name: "Test Tenant",
            Description: "A test tenant",
            Status: TenantStatus.Active,
            Members:
            [
                new("user-1", TenantRole.TenantOwner),
                new("user-2", TenantRole.TenantReader),
            ],
            Configuration: new Dictionary<string, string> {
                ["key1"] = "value1",
                ["key2"] = "value2",
            },
            CreatedAt: new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero));

        string json = JsonSerializer.Serialize(original, JsonOptions);
        TenantDetail? deserialized = JsonSerializer.Deserialize<TenantDetail>(json, JsonOptions);

        _ = deserialized.ShouldNotBeNull();
        deserialized.TenantId.ShouldBe(original.TenantId);
        deserialized.Name.ShouldBe(original.Name);
        deserialized.Description.ShouldBe(original.Description);
        deserialized.Status.ShouldBe(original.Status);
        deserialized.Members.Count.ShouldBe(2);
        deserialized.Members[0].UserId.ShouldBe("user-1");
        deserialized.Members[0].Role.ShouldBe(TenantRole.TenantOwner);
        deserialized.Configuration.Count.ShouldBe(2);
        deserialized.Configuration["key1"].ShouldBe("value1");
        deserialized.CreatedAt.ShouldBe(original.CreatedAt);
    }

    [Fact]
    public void PaginatedResult_round_trip_preserves_structure() {
        PaginatedResult<TenantSummary> original = new(
            Items:
            [
                new("tenant-1", "First", TenantStatus.Active),
                new("tenant-2", "Second", TenantStatus.Disabled),
            ],
            Cursor: "tenant-2",
            HasMore: true);

        string json = JsonSerializer.Serialize(original, JsonOptions);
        PaginatedResult<TenantSummary>? deserialized = JsonSerializer.Deserialize<PaginatedResult<TenantSummary>>(json, JsonOptions);

        _ = deserialized.ShouldNotBeNull();
        deserialized.Items.Count.ShouldBe(2);
        deserialized.Cursor.ShouldBe("tenant-2");
        deserialized.HasMore.ShouldBeTrue();

        // Verify JSON structure matches expected format
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("items", out _).ShouldBeTrue("JSON should have 'items' property (camelCase)");
        doc.RootElement.TryGetProperty("cursor", out _).ShouldBeTrue("JSON should have 'cursor' property");
        doc.RootElement.TryGetProperty("hasMore", out _).ShouldBeTrue("JSON should have 'hasMore' property");
    }

    [Fact]
    public void UserTenantMembership_round_trip_uses_camelCase_shape_and_string_enums() {
        UserTenantMembership original = new(
            TenantId: "tenant-1",
            Name: "Tenant One",
            Status: TenantStatus.Disabled,
            Role: TenantRole.TenantContributor);

        string json = JsonSerializer.Serialize(original, JsonOptions);
        UserTenantMembership? deserialized = JsonSerializer.Deserialize<UserTenantMembership>(json, JsonOptions);

        _ = deserialized.ShouldNotBeNull();
        deserialized.TenantId.ShouldBe(original.TenantId);
        deserialized.Name.ShouldBe(original.Name);
        deserialized.Status.ShouldBe(TenantStatus.Disabled);
        deserialized.Role.ShouldBe(TenantRole.TenantContributor);

        using var document = JsonDocument.Parse(json);
        document.RootElement.TryGetProperty("tenantId", out JsonElement tenantId).ShouldBeTrue();
        tenantId.GetString().ShouldBe("tenant-1");
        document.RootElement.GetProperty("name").GetString().ShouldBe("Tenant One");
        document.RootElement.GetProperty("status").GetString().ShouldBe("Disabled");
        document.RootElement.GetProperty("role").GetString().ShouldBe("TenantContributor");
        document.RootElement.TryGetProperty("TenantId", out _).ShouldBeFalse();
    }

    [Fact]
    public void PaginatedResult_of_UserTenantMembership_round_trip_preserves_public_shape() {
        PaginatedResult<UserTenantMembership> original = new(
            Items:
            [
                new("tenant-1", "First", TenantStatus.Active, TenantRole.TenantOwner),
                new("tenant-2", "Second", TenantStatus.Disabled, TenantRole.TenantReader),
            ],
            Cursor: "opaque-cursor",
            HasMore: true);

        string json = JsonSerializer.Serialize(original, JsonOptions);
        PaginatedResult<UserTenantMembership>? deserialized = JsonSerializer.Deserialize<PaginatedResult<UserTenantMembership>>(json, JsonOptions);

        _ = deserialized.ShouldNotBeNull();
        deserialized.Items.Count.ShouldBe(2);
        deserialized.Items[0].TenantId.ShouldBe("tenant-1");
        deserialized.Items[0].Role.ShouldBe(TenantRole.TenantOwner);
        deserialized.Cursor.ShouldBe("opaque-cursor");
        deserialized.HasMore.ShouldBeTrue();

        using var document = JsonDocument.Parse(json);
        document.RootElement.TryGetProperty("items", out JsonElement items).ShouldBeTrue();
        items[0].GetProperty("tenantId").GetString().ShouldBe("tenant-1");
        items[0].GetProperty("name").GetString().ShouldBe("First");
        items[0].GetProperty("status").GetString().ShouldBe("Active");
        items[0].GetProperty("role").GetString().ShouldBe("TenantOwner");
        document.RootElement.GetProperty("cursor").GetString().ShouldBe("opaque-cursor");
        document.RootElement.GetProperty("hasMore").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public void TenantStatus_and_TenantRole_serialize_as_strings() {
        TenantSummary summary = new("tenant-1", "Test", TenantStatus.Active);
        string json = JsonSerializer.Serialize(summary, JsonOptions);

        json.ShouldContain("\"Active\"");
        json.ShouldNotContain("\":0");

        TenantMember member = new("user-1", TenantRole.TenantOwner);
        string memberJson = JsonSerializer.Serialize(member, JsonOptions);

        memberJson.ShouldContain("\"TenantOwner\"");
    }

    [Fact]
    public void TenantSummary_with_unrecognized_status_deserializes_to_Unknown() {
        const string json = """{"tenantId":"tenant-1","name":"Test","status":"Suspended"}""";

        TenantSummary? summary = JsonSerializer.Deserialize<TenantSummary>(json, JsonOptions);

        _ = summary.ShouldNotBeNull();
        summary.Status.ShouldBe(TenantStatus.Unknown);
    }

    [Fact]
    public void TenantAuditEntry_round_trip_preserves_metadata_and_payload() {
        TenantAuditEntry original = new(
            EventId: "evt-001",
            EventType: "TenantCreated",
            Category: AuditEventCategory.Administrative,
            ActorId: "admin-1",
            Timestamp: new DateTimeOffset(2026, 5, 14, 10, 30, 0, TimeSpan.Zero),
            TenantId: "tenant-1",
            NarrativePayload: new Dictionary<string, string> {
                ["name"] = "Acme",
                ["createdAt"] = "2026-05-14T10:30:00.0000000+00:00",
            });

        string json = JsonSerializer.Serialize(original, JsonOptions);
        TenantAuditEntry? deserialized = JsonSerializer.Deserialize<TenantAuditEntry>(json, JsonOptions);

        _ = deserialized.ShouldNotBeNull();
        deserialized.EventId.ShouldBe(original.EventId);
        deserialized.EventType.ShouldBe(original.EventType);
        deserialized.Category.ShouldBe(AuditEventCategory.Administrative);
        deserialized.ActorId.ShouldBe(original.ActorId);
        deserialized.Timestamp.ShouldBe(original.Timestamp);
        deserialized.TenantId.ShouldBe(original.TenantId);
        deserialized.NarrativePayload["name"].ShouldBe("Acme");
        json.ShouldContain("\"Administrative\"");
        json.ShouldNotContain("\":1");
    }
}
