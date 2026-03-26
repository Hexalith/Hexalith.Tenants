using FluentValidation;

using Hexalith.Tenants.Contracts.Commands;

namespace Hexalith.Tenants.Server.Validators;

public class AddUserToTenantValidator : AbstractValidator<AddUserToTenant> {
    public AddUserToTenantValidator() {
        _ = RuleFor(x => x.TenantId).NotEmpty();
        _ = RuleFor(x => x.UserId).NotEmpty();
        _ = RuleFor(x => x.Role).IsInEnum();
    }
}
