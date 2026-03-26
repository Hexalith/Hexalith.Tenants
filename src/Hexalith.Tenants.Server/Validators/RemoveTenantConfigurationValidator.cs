using FluentValidation;

using Hexalith.Tenants.Contracts.Commands;

namespace Hexalith.Tenants.Server.Validators;

public class RemoveTenantConfigurationValidator : AbstractValidator<RemoveTenantConfiguration> {
    public RemoveTenantConfigurationValidator() {
        _ = RuleFor(x => x.TenantId).NotEmpty();
        _ = RuleFor(x => x.Key)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .MinimumLength(1);
    }
}
