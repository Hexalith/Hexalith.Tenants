using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Queries;

using Microsoft.AspNetCore.DataProtection;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Queries;

public class TenantQueryCursorCodecTests {
    [Fact]
    public void Cursor_scopes_preserve_existing_endpoint_strings() {
        DateTimeOffset from = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset to = from.AddHours(1);

        TenantQueryCursorScopes.ListTenants("user-1").ShouldBe("user:user-1");
        TenantQueryCursorScopes.GetTenantUsers("tenant-1").ShouldBe("tenant:tenant-1");
        TenantQueryCursorScopes.GetUserTenants("user-1", "user-2").ShouldBe("requester:user-1|target-user:user-2");
        TenantQueryCursorScopes
            .GetTenantAudit("tenant-1", from, to, AuditEventCategory.Administrative)
            .ShouldBe("tenant:tenant-1|from:2026-05-14T10:00:00.0000000Z|to:2026-05-14T11:00:00.0000000Z|category:Administrative");
    }

    [Fact]
    public void Cursor_scopes_escape_user_controlled_segments_once_to_prevent_collisions() {
        string escapedListScope = TenantQueryCursorScopes.ListTenants(@"user\1|target-user:admin");
        string unescapedDifferentScope = TenantQueryCursorScopes.GetUserTenants(@"user\1", "admin");

        escapedListScope.ShouldBe(@"user:user\\1\ptarget-user\cadmin");
        unescapedDifferentScope.ShouldBe(@"requester:user\\1|target-user:admin");
        escapedListScope.ShouldNotBe(unescapedDifferentScope);
    }

    [Fact]
    public void TryDecode_accepts_existing_list_tenants_query_scope_and_logical_position_shape() {
        ITenantQueryCursorCodec codec = CreateCodec();
        string scope = TenantQueryCursorScopes.ListTenants("user-1");
        string cursor = codec.Encode(ListTenantsQuery.QueryType, scope, "tenant-001");

        bool decoded = codec.TryDecode(cursor, ListTenantsQuery.QueryType, scope, out string? position, out string? failureReason);

        decoded.ShouldBeTrue();
        position.ShouldBe("tenant-001");
        failureReason.ShouldBeNull();
    }

    [Fact]
    public void Encode_does_not_expose_raw_position() {
        ITenantQueryCursorCodec codec = CreateCodec();

        string cursor = codec.Encode("list-tenants", TenantQueryCursorScopes.ListTenants("user-1"), "tenant-001");

        cursor.ShouldNotContain("tenant-001");
        cursor.ShouldNotContain("list-tenants");
        cursor.ShouldNotContain("user-1");
    }

    [Fact]
    public void Encode_does_not_expose_raw_scope_segments_or_audit_position() {
        ITenantQueryCursorCodec codec = CreateCodec();
        string scope = TenantQueryCursorScopes.GetTenantAudit(
            "tenant-secret",
            new DateTimeOffset(2026, 5, 14, 10, 0, 0, TimeSpan.Zero),
            null,
            AuditEventCategory.Access);

        string cursor = codec.Encode(GetTenantAuditQuery.QueryType, scope, "0635788912000000000:evt-secret");

        cursor.ShouldNotContain(GetTenantAuditQuery.QueryType);
        cursor.ShouldNotContain("tenant-secret");
        cursor.ShouldNotContain("evt-secret");
        cursor.ShouldNotContain("Access");
    }

    [Fact]
    public void TryDecode_returns_position_for_matching_query_and_scope() {
        ITenantQueryCursorCodec codec = CreateCodec();
        string scope = TenantQueryCursorScopes.GetTenantUsers("tenant-1");
        string cursor = codec.Encode("get-tenant-users", scope, "user-7");

        bool decoded = codec.TryDecode(cursor, "get-tenant-users", scope, out string? position, out string? failureReason);

        decoded.ShouldBeTrue();
        position.ShouldBe("user-7");
        failureReason.ShouldBeNull();
    }

    [Fact]
    public void TryDecode_rejects_wrong_query_type() {
        ITenantQueryCursorCodec codec = CreateCodec();
        string cursor = codec.Encode("list-tenants", TenantQueryCursorScopes.ListTenants("user-1"), "tenant-001");

        bool decoded = codec.TryDecode(cursor, "get-tenant-users", TenantQueryCursorScopes.GetTenantUsers("tenant-1"), out string? position, out string? failureReason);

        decoded.ShouldBeFalse();
        position.ShouldBeNull();
        failureReason.ShouldBe("wrong-query-type");
    }

    [Fact]
    public void TryDecode_rejects_wrong_scope() {
        ITenantQueryCursorCodec codec = CreateCodec();
        string cursor = codec.Encode("list-tenants", TenantQueryCursorScopes.ListTenants("user-1"), "tenant-001");

        bool decoded = codec.TryDecode(cursor, "list-tenants", TenantQueryCursorScopes.ListTenants("user-2"), out string? position, out string? failureReason);

        decoded.ShouldBeFalse();
        position.ShouldBeNull();
        failureReason.ShouldBe("wrong-scope");
    }

    [Fact]
    public void TryDecode_rejects_malformed_cursor() {
        ITenantQueryCursorCodec codec = CreateCodec();

        bool decoded = codec.TryDecode("not-a-protected-cursor", "list-tenants", TenantQueryCursorScopes.ListTenants("user-1"), out string? position, out string? failureReason);

        decoded.ShouldBeFalse();
        position.ShouldBeNull();
        failureReason.ShouldBe("tamper-or-key-rotation");
    }

    [Fact]
    public void TryDecode_rejects_cursor_above_length_cap() {
        ITenantQueryCursorCodec codec = CreateCodec();
        string oversized = new('A', 4097);

        bool decoded = codec.TryDecode(oversized, "list-tenants", TenantQueryCursorScopes.ListTenants("user-1"), out string? position, out string? failureReason);

        decoded.ShouldBeFalse();
        position.ShouldBeNull();
        failureReason.ShouldBe("too-large");
    }

    [Fact]
    public void TryDecode_rejects_tampered_cursor() {
        ITenantQueryCursorCodec codec = CreateCodec();
        string scope = TenantQueryCursorScopes.GetTenantAudit("tenant-1", null, null, null);
        string cursor = codec.Encode("get-tenant-audit", scope, "00000000000000000001:evt-1");

        // Mutate a byte mid-payload so the change lands in ciphertext rather than base64 padding —
        // a last-character flip can become a no-op for some encodings.
        char[] tamperedChars = cursor.ToCharArray();
        int midIndex = tamperedChars.Length / 2;
        tamperedChars[midIndex] = tamperedChars[midIndex] == 'A' ? 'B' : 'A';
        string tampered = new(tamperedChars);

        bool decoded = codec.TryDecode(tampered, "get-tenant-audit", scope, out string? position, out string? failureReason);

        decoded.ShouldBeFalse();
        position.ShouldBeNull();
        // Either the protector rejects the MAC (tamper-or-key-rotation) or the decoded JSON is unparseable (malformed).
        failureReason.ShouldBeOneOf("tamper-or-key-rotation", "malformed");
    }

    [Fact]
    public void TryDecode_rejects_cursor_after_data_protection_key_rotation_equivalent() {
        ITenantQueryCursorCodec originalCodec = CreateCodec();
        ITenantQueryCursorCodec rotatedKeyCodec = CreateCodec();
        string scope = TenantQueryCursorScopes.ListTenants("user-1");
        string cursor = originalCodec.Encode(ListTenantsQuery.QueryType, scope, "tenant-001");

        bool decoded = rotatedKeyCodec.TryDecode(cursor, ListTenantsQuery.QueryType, scope, out string? position, out string? failureReason);

        decoded.ShouldBeFalse();
        position.ShouldBeNull();
        failureReason.ShouldBe("tamper-or-key-rotation");
    }

    [Fact]
    public void TryDecode_returns_true_with_null_failure_reason_for_empty_cursor() {
        ITenantQueryCursorCodec codec = CreateCodec();

        bool decoded = codec.TryDecode(null, "list-tenants", TenantQueryCursorScopes.ListTenants("user-1"), out string? position, out string? failureReason);

        decoded.ShouldBeTrue();
        position.ShouldBeNull();
        failureReason.ShouldBeNull();
    }

    private static ITenantQueryCursorCodec CreateCodec()
        => new TenantQueryCursorCodec(new EphemeralDataProtectionProvider());
}
