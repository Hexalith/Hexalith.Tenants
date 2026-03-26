using FluentValidation;

using Hexalith.Tenants.Contracts.Commands;

namespace Hexalith.Tenants.Server.Validators;

public class ChangeUserRoleValidator : AbstractValidator<ChangeUserRole> {
    public ChangeUserRoleValidator() {
        _ = RuleFor(x => x.TenantId).NotEmpty();
        _ = RuleFor(x => x.UserId).NotEmpty();
        _ = RuleFor(x => x.NewRole).IsInEnum();
    }
}
