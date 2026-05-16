using Hexalith.Tenants.Queries;

using Microsoft.AspNetCore.DataProtection;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Queries;

public class TenantQueryCursorCodecTests {
    [Fact]
    public void Encode_does_not_expose_raw_position() {
        ITenantQueryCursorCodec codec = CreateCodec();

        string cursor = codec.Encode("list-tenants", TenantQueryCursorScopes.ListTenants("user-1"), "tenant-001");

        cursor.ShouldNotContain("tenant-001");
        cursor.ShouldNotContain("list-tenants");
        cursor.ShouldNotContain("user-1");
    }

    [Fact]
    public void TryDecode_returns_position_for_matching_query_and_scope() {
        ITenantQueryCursorCodec codec = CreateCodec();
        string scope = TenantQueryCursorScopes.GetTenantUsers("tenant-1");
        string cursor = codec.Encode("get-tenant-users", scope, "user-7");

        bool decoded = codec.TryDecode(cursor, "get-tenant-users", scope, out string? position);

        decoded.ShouldBeTrue();
        position.ShouldBe("user-7");
    }

    [Fact]
    public void TryDecode_rejects_wrong_query_type() {
        ITenantQueryCursorCodec codec = CreateCodec();
        string cursor = codec.Encode("list-tenants", TenantQueryCursorScopes.ListTenants("user-1"), "tenant-001");

        bool decoded = codec.TryDecode(cursor, "get-tenant-users", TenantQueryCursorScopes.GetTenantUsers("tenant-1"), out string? position);

        decoded.ShouldBeFalse();
        position.ShouldBeNull();
    }

    [Fact]
    public void TryDecode_rejects_wrong_scope() {
        ITenantQueryCursorCodec codec = CreateCodec();
        string cursor = codec.Encode("list-tenants", TenantQueryCursorScopes.ListTenants("user-1"), "tenant-001");

        bool decoded = codec.TryDecode(cursor, "list-tenants", TenantQueryCursorScopes.ListTenants("user-2"), out string? position);

        decoded.ShouldBeFalse();
        position.ShouldBeNull();
    }

    [Fact]
    public void TryDecode_rejects_malformed_cursor() {
        ITenantQueryCursorCodec codec = CreateCodec();

        bool decoded = codec.TryDecode("not-a-protected-cursor", "list-tenants", TenantQueryCursorScopes.ListTenants("user-1"), out string? position);

        decoded.ShouldBeFalse();
        position.ShouldBeNull();
    }

    [Fact]
    public void TryDecode_rejects_tampered_cursor() {
        ITenantQueryCursorCodec codec = CreateCodec();
        string cursor = codec.Encode("get-tenant-audit", TenantQueryCursorScopes.GetTenantAudit("tenant-1", null, null, null), "00000000000000000001:evt-1");
        string tampered = cursor[..^1] + (cursor[^1] == 'A' ? "B" : "A");

        bool decoded = codec.TryDecode(tampered, "get-tenant-audit", TenantQueryCursorScopes.GetTenantAudit("tenant-1", null, null, null), out string? position);

        decoded.ShouldBeFalse();
        position.ShouldBeNull();
    }

    private static ITenantQueryCursorCodec CreateCodec()
        => new TenantQueryCursorCodec(new EphemeralDataProtectionProvider());
}
