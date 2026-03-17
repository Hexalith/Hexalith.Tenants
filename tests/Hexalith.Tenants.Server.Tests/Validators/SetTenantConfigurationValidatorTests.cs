using FluentValidation.TestHelper;

using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Server.Validators;

namespace Hexalith.Tenants.Server.Tests.Validators;

public class SetTenantConfigurationValidatorTests
{
    private readonly SetTenantConfigurationValidator _validator = new();

    [Fact]
    public void Should_have_error_when_TenantId_is_empty()
        => _validator.TestValidate(new SetTenantConfiguration("", "key", "value"))
            .ShouldHaveValidationErrorFor(x => x.TenantId);

    [Fact]
    public void Should_have_error_when_Key_is_empty()
        => _validator.TestValidate(new SetTenantConfiguration("acme", "", "value"))
            .ShouldHaveValidationErrorFor(x => x.Key);

    [Fact]
    public void Should_have_error_when_Key_exceeds_max_length()
        => _validator.TestValidate(new SetTenantConfiguration("acme", new string('k', 257), "value"))
            .ShouldHaveValidationErrorFor(x => x.Key);

    [Fact]
    public void Should_have_error_when_Value_exceeds_max_length()
        => _validator.TestValidate(new SetTenantConfiguration("acme", "key", new string('v', 1025)))
            .ShouldHaveValidationErrorFor(x => x.Value);

    [Fact]
    public void Should_not_have_error_when_Value_is_empty()
        => _validator.TestValidate(new SetTenantConfiguration("acme", "key", ""))
            .ShouldNotHaveValidationErrorFor(x => x.Value);
}
