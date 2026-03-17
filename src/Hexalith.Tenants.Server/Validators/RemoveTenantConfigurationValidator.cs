using FluentValidation;

using Hexalith.Tenants.Contracts.Commands;

namespace Hexalith.Tenants.Server.Validators;

public class RemoveTenantConfigurationValidator : AbstractValidator<RemoveTenantConfiguration>
{
    public RemoveTenantConfigurationValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Key)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .MinimumLength(1);
    }
}
