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
