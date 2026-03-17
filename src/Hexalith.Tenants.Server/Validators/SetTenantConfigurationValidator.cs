using FluentValidation;

using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Server.Aggregates;

namespace Hexalith.Tenants.Server.Validators;

public class SetTenantConfigurationValidator : AbstractValidator<SetTenantConfiguration>
{
    public SetTenantConfigurationValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Key).NotEmpty().MaximumLength(TenantAggregate.MaxKeyLength);
        RuleFor(x => x.Value).MaximumLength(TenantAggregate.MaxValueLength);
    }
}
