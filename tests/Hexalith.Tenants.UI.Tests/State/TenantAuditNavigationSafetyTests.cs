using Hexalith.Tenants.UI.State.TenantAudit;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantAuditNavigationSafetyTests
{
    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("abc/def")]
    [InlineData("tenant\u200dalpha")]
    [InlineData("eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c")]
    public void UnsafeIdentifiersCannotBecomeTenantRoutes(string identifier)
        => TenantAuditNavigationSafety.IsSafeIdentifier(identifier).ShouldBeFalse();

    [Fact]
    public void UnpairedSurrogateCannotBecomeTenantRoute()
        => TenantAuditNavigationSafety.IsSafeIdentifier("tenant" + new string((char)0xD800, 1) + "alpha")
            .ShouldBeFalse();

    [Theory]
    [InlineData("/tenants//users")]
    [InlineData("/tenants/../admin")]
    [InlineData("/tenants/%2e%2e")]
    [InlineData("//evil.example/tenants")]
    [InlineData("/tenants?search=Bearer%2520abc")]
    [InlineData("/tenants?search=token%253Dsecret")]
    [InlineData("/tenants?search=etag%253Dopaque")]
    [InlineData("/tenants?search=access_token%3Dsecret")]
    [InlineData("/tenants?search=refresh_token%253Dsecret")]
    [InlineData("/tenants?returnUrl=%2Ftenants%3Fsearch%3Daccess_token%253Dsecret")]
    [InlineData("/tenants?returnUrl=%2Ftenants%3FreturnUrl%3D%252Ftenants")]
    public void UnsafeReturnsAreRejected(string returnUrl)
        => TenantAuditNavigationSafety.SafeReturnUrl(returnUrl, out _).ShouldBeNull();

    [Fact]
    public void OrdinarySearchAndEncodedPercentRemainSafe()
    {
        string? safe = TenantAuditNavigationSafety.SafeReturnUrl(
            "/tenants?search=100%25%20tokenization%2Falpha%40example", out bool partial);

        safe.ShouldBe("/tenants?search=100%25%20tokenization%2Falpha%40example");
        partial.ShouldBeFalse();
    }

    [Fact]
    public void CursorIsStrippedAndReportedWithoutLosingNestedContext()
    {
        string? safe = TenantAuditNavigationSafety.SafeReturnUrl(
            "/tenants/tenant.alpha?returnUrl=%2Ftenants%3Fcursor%3Dprotected%26search%3Dalpha", out bool partial);

        safe.ShouldNotBeNull();
        safe.ShouldContain("returnUrl=");
        safe.ShouldNotContain("cursor", Case.Insensitive);
        partial.ShouldBeTrue();
    }

    [Fact]
    public void EncodedPlusFocusIdentifierIsPreserved()
    {
        string? safe = TenantAuditNavigationSafety.SafeReturnUrl(
            "/tenants/tenant.alpha?auditFocus=tenants-member-user%2Balpha", out _);

        safe.ShouldBe("/tenants/tenant.alpha?auditFocus=tenants-member-user%2Balpha");
    }

    [Fact]
    public void NestedFocusForWrongReturnRouteIsRejected()
    {
        string? safe = TenantAuditNavigationSafety.SafeReturnUrl(
            "/tenants/tenant.alpha?returnUrl=%2Ftenants%3FauditFocus%3Dtenants-member-user%2Balpha%40example.com",
            out bool partial);

        safe.ShouldBeNull();
        partial.ShouldBeFalse();
        TenantAuditNavigationSafety.IsSafeFocus("tenants-member-user+alpha@example.com").ShouldBeTrue();
    }

    [Fact]
    public void NestedFocusMustBelongToItsOwnNestedReturnRoute()
    {
        string? valid = TenantAuditNavigationSafety.SafeReturnUrl(
            "/tenants/tenant.alpha?returnUrl=%2Ftenants%3Ftab%3Dusers%26auditFocus%3Dtenants-user-row-tenant.alpha",
            out _);
        valid.ShouldNotBeNull();

        string? invalid = TenantAuditNavigationSafety.SafeReturnUrl(
            "/tenants/tenant.alpha?returnUrl=%2Ftenants%3Ftab%3Dusers%26auditFocus%3Dtenant-row-tenant.alpha",
            out _);
        invalid.ShouldBeNull();
    }

    [Theory]
    [InlineData("/tenants/tenants", "tenant-row-tenant.alpha", "tenants-user-row-tenant.alpha")]
    [InlineData("/tenants/workspace-users", "tenants-user-row-tenant.alpha", "tenant-row-tenant.alpha")]
    public void CanonicalWorkspaceReturnAcceptsOnlyItsOwnFocus(string route, string validFocus, string invalidFocus)
    {
        TenantAuditNavigationSafety.SafeListReturnUrl(route, out _).ShouldBe(route);
        TenantAuditNavigationSafety.IsFocusForReturnUrl(route, validFocus).ShouldBeTrue();
        TenantAuditNavigationSafety.IsFocusForReturnUrl(route, invalidFocus).ShouldBeFalse();
    }

    [Fact]
    public void CanonicalTenantsRouteIgnoresLegacyUsersTabQueryForFocus()
        => TenantAuditNavigationSafety.IsFocusForReturnUrl(
            "/tenants/tenants?tab=users", "tenants-user-row-tenant.alpha").ShouldBeFalse();

    [Fact]
    public void SafeUserIdentityPunctuationRemainsValidInDerivedMemberFocus()
    {
        TenantAuditNavigationSafety.IsSafeFocus("tenants-member-user+'!~").ShouldBeTrue();
        TenantAuditNavigationSafety.IsSafeFocus("tenants-member-user%2Fsecret").ShouldBeFalse();
    }

    [Fact]
    public void FocusDerivedFromAMaximumLengthIdentityRemainsValid()
    {
        string userId = new('u', 256);

        TenantAuditNavigationSafety.IsSafeIdentifier(userId).ShouldBeTrue();
        TenantAuditNavigationSafety.IsSafeFocus($"tenants-member-{userId}").ShouldBeTrue();
        TenantAuditNavigationSafety.IsSafeFocus($"tenants-member-{userId}u").ShouldBeFalse();
        TenantAuditNavigationSafety.IsSafeFocus("tenants-member-").ShouldBeFalse();
    }

    [Theory]
    [InlineData("eyJhbGciOiJIUzI1NiJ9")]
    [InlineData("messageid-01ARZ3NDEKTSV4RRFFQ69G5FAV")]
    [InlineData("correlation-123")]
    public void CommandReferenceHintAppliesTheReceiptApprovedReferenceRule(string reference)
    {
        TenantAuditNavigationSafety.IsSafeIdentifier(reference).ShouldBeTrue();
        TenantAuditNavigationSafety.IsSafeHint(reference).ShouldBeFalse();
        TenantAuditNavigationSafety.IsSafeHint("command-safe-reference").ShouldBeTrue();
    }

    [Fact]
    public void NestedPlusAndAtFocusIdentifierIsPreserved()
    {
        string? safe = TenantAuditNavigationSafety.SafeReturnUrl(
            "/tenants/tenant.alpha?returnUrl=%2Ftenants%3Ftab%3Dusers%26auditFocus%3Dtenants-user-row-user%2Balpha%40example.com",
            out bool partial);

        safe.ShouldBe("/tenants/tenant.alpha?returnUrl=%2Ftenants%3Ftab%3Dusers%26auditFocus%3Dtenants-user-row-user%252Balpha%2540example.com");
        partial.ShouldBeFalse();
        TenantAuditNavigationSafety.IsSafeFocus("tenants-user-row-user+alpha@example.com").ShouldBeTrue();
    }

    [Fact]
    public void LiteralEscapedSlashInSearchRemainsLiteral()
    {
        string? safe = TenantAuditNavigationSafety.SafeReturnUrl("/tenants?search=alpha%252Fbeta", out bool partial);

        safe.ShouldBe("/tenants?search=alpha%252Fbeta");
        partial.ShouldBeFalse();
    }

    [Theory]
    [InlineData("/tenants?search=alpha+beta", "/tenants?search=alpha%20beta")]
    [InlineData("/tenants?search=alpha%2Bbeta", "/tenants?search=alpha%2Bbeta")]
    [InlineData("/tenants/tenant.alpha?returnUrl=%2Ftenants%3Fsearch%3Dalpha%2Bbeta", "/tenants/tenant.alpha?returnUrl=%2Ftenants%3Fsearch%3Dalpha%2520beta")]
    [InlineData("/tenants/tenant.alpha?returnUrl=%2Ftenants%3Fsearch%3Dalpha%252Bbeta", "/tenants/tenant.alpha?returnUrl=%2Ftenants%3Fsearch%3Dalpha%252Bbeta")]
    public void SearchPlusDistinguishesFormSpaceFromEscapedLiteral(string returnUrl, string expected)
    {
        string? safe = TenantAuditNavigationSafety.SafeReturnUrl(returnUrl, out bool partial);

        safe.ShouldBe(expected);
        partial.ShouldBeFalse();
    }

    [Fact]
    public void JwtShapedCursorIsDroppedBeforeCredentialInspection()
    {
        const string cursor = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        string? safe = TenantAuditNavigationSafety.SafeReturnUrl(
            "/tenants?search=alpha&cursor=" + cursor, out bool partial);

        safe.ShouldBe("/tenants?search=alpha");
        partial.ShouldBeTrue();
    }
}
