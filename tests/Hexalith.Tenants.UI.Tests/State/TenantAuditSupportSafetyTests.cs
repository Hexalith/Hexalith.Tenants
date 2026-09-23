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
}
