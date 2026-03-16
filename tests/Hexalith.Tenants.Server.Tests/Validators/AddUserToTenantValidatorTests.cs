using FluentValidation.TestHelper;

using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Server.Validators;

namespace Hexalith.Tenants.Server.Tests.Validators;

public class AddUserToTenantValidatorTests
{
    private readonly AddUserToTenantValidator _validator = new();

    [Fact]
    public void Should_have_error_when_TenantId_is_empty()
        => _validator.TestValidate(new AddUserToTenant(string.Empty, "user-1", TenantRole.TenantReader))
            .ShouldHaveValidationErrorFor(x => x.TenantId);

    [Fact]
    public void Should_have_error_when_UserId_is_empty()
        => _validator.TestValidate(new AddUserToTenant("acme", string.Empty, TenantRole.TenantReader))
            .ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void Should_have_error_when_Role_is_invalid_enum()
        => _validator.TestValidate(new AddUserToTenant("acme", "user-1", (TenantRole)99))
            .ShouldHaveValidationErrorFor(x => x.Role);
}
