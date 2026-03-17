using FluentValidation.TestHelper;

using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Server.Validators;

namespace Hexalith.Tenants.Server.Tests.Validators;

public class RemoveTenantConfigurationValidatorTests
{
    private readonly RemoveTenantConfigurationValidator _validator = new();

    [Fact]
    public void Should_have_error_when_TenantId_is_empty()
        => _validator.TestValidate(new RemoveTenantConfiguration("", "key"))
            .ShouldHaveValidationErrorFor(x => x.TenantId);

    [Fact]
    public void Should_have_error_when_Key_is_null()
        => _validator.TestValidate(new RemoveTenantConfiguration("acme", null!))
            .ShouldHaveValidationErrorFor(x => x.Key);

    [Fact]
    public void Should_have_error_when_Key_is_empty()
        => _validator.TestValidate(new RemoveTenantConfiguration("acme", ""))
            .ShouldHaveValidationErrorFor(x => x.Key);

    [Fact]
    public void Should_not_have_error_for_valid_input()
        => _validator.TestValidate(new RemoveTenantConfiguration("acme", "billing.plan"))
            .ShouldNotHaveAnyValidationErrors();
}
