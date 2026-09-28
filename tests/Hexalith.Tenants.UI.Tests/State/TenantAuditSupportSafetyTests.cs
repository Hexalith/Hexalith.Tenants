using Hexalith.Tenants.UI.Services.SupportSafety;
using Hexalith.Tenants.UI.State.TenantAudit;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantAuditSupportSafetyTests
{
    [Theory]
    [InlineData("event\u200Bsafe")]
    [InlineData("event\u202Esafe")]
    [InlineData("event\u2060safe")]
    [InlineData("event\U000E0001safe")]
    [InlineData("event%E2%80%8Bsafe")]
    public void Invisible_unicode_format_characters_are_not_support_safe(string value)
    {
        TenantAuditSupportSafety.SafeApprovedReference(value).ShouldBeNull();
        TenantAuditSupportSafety.SafeIdentifier(value, SupportSafeCopyValueKind.UserId).ShouldBeEmpty();
    }

    [Fact]
    public void Ordinary_visible_reference_remains_support_safe()
        => TenantAuditSupportSafety.SafeApprovedReference("event-visible-1").ShouldBe("event-visible-1");

    [Theory]
    [InlineData("person@example.test")]
    [InlineData("person＠example.test")]
    [InlineData("+1-202-555-0100")]
    [InlineData("202\u00A0555\u00A00100")]
    [InlineData("٢٠٢٥٥٥٠١٠٠")]
    [InlineData("user|Actor: false")]
    [InlineData("user\uFF5Ctarget")]
    [InlineData("user\u2223target")]
    [InlineData("user\u2225target")]
    [InlineData("user\u2016target")]
    [InlineData("user\u2502target")]
    [InlineData("user\u2758target")]
    [InlineData("user\u2759target")]
    [InlineData("user;target")]
    [InlineData("user\uFF1Btarget")]
    [InlineData("user\u00A6target")]
    [InlineData("user\u2551target")]
    [InlineData("user\u2028target")]
    public void Pii_and_visual_field_boundaries_are_rejected_before_typed_mapping(string value)
    {
        TenantAuditSupportSafety.SafeIdentifier(value, SupportSafeCopyValueKind.UserId).ShouldBeEmpty();
        TenantAuditSupportSafety.SafeApprovedReference(value).ShouldBeNull();
    }

    [Fact]
    public void Summary_field_boundary_check_rejects_ascii_semicolon_and_visual_dividers()
    {
        TenantAuditSupportSafety.ContainsFieldBoundary("actor; target").ShouldBeTrue();
        TenantAuditSupportSafety.ContainsFieldBoundary("actor\u00A6target").ShouldBeTrue();
        TenantAuditSupportSafety.ContainsFieldBoundary("actor\u2551target").ShouldBeTrue();
        TenantAuditSupportSafety.SafeApprovedReference("event\uFF1Bactor").ShouldBeNull();
    }

    [Fact]
    public void Visible_non_pii_unicode_identifier_remains_usable()
        => TenantAuditSupportSafety.SafeIdentifier("équipe-α", SupportSafeCopyValueKind.UserId).ShouldBe("équipe-α");

    [Fact]
    public void Malformed_utf16_identifier_fails_closed_without_throwing()
    {
        string value = "user" + new string('\uD800', 1) + "target";
        TenantAuditSupportSafety.SafeIdentifier(value, SupportSafeCopyValueKind.UserId).ShouldBeEmpty();
        TenantAuditSupportSafety.SafeApprovedReference(value).ShouldBeNull();
    }
}
