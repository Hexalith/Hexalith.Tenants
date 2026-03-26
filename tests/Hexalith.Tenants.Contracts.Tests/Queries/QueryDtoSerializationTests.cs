using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;

using Shouldly;

namespace Hexalith.Tenants.Contracts.Tests.Queries;

public class QueryDtoSerializationTests {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
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
    public void TenantStatus_and_TenantRole_serialize_as_strings() {
        TenantSummary summary = new("tenant-1", "Test", TenantStatus.Active);
        string json = JsonSerializer.Serialize(summary, JsonOptions);

        json.ShouldContain("\"Active\"");
        json.ShouldNotContain("\":0");

        TenantMember member = new("user-1", TenantRole.TenantOwner);
        string memberJson = JsonSerializer.Serialize(member, JsonOptions);

        memberJson.ShouldContain("\"TenantOwner\"");
    }
}
