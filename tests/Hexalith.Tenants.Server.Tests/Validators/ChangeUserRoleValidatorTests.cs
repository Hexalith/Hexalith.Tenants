using FluentValidation.TestHelper;

using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Server.Validators;

namespace Hexalith.Tenants.Server.Tests.Validators;

public class ChangeUserRoleValidatorTests
{
    private readonly ChangeUserRoleValidator _validator = new();

    [Fact]
    public void Should_have_error_when_TenantId_is_empty()
        => _validator.TestValidate(new ChangeUserRole(string.Empty, "user-1", TenantRole.TenantContributor))
            .ShouldHaveValidationErrorFor(x => x.TenantId);

    [Fact]
    public void Should_have_error_when_UserId_is_empty()
        => _validator.TestValidate(new ChangeUserRole("acme", string.Empty, TenantRole.TenantContributor))
            .ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void Should_have_error_when_NewRole_is_invalid_enum()
        => _validator.TestValidate(new ChangeUserRole("acme", "user-1", (TenantRole)99))
            .ShouldHaveValidationErrorFor(x => x.NewRole);
}
