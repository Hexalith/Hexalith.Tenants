using FluentValidation;

using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Server.Aggregates;

namespace Hexalith.Tenants.Server.Validators;

public class SetTenantConfigurationValidator : AbstractValidator<SetTenantConfiguration> {
    public SetTenantConfigurationValidator() {
        _ = RuleFor(x => x.TenantId).NotEmpty();
        _ = RuleFor(x => x.Key)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .MinimumLength(1)
            .MaximumLength(TenantAggregate.MaxKeyLength);
        _ = RuleFor(x => x.Value)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .MaximumLength(TenantAggregate.MaxValueLength);
    }
}
