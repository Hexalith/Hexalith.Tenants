using FluentValidation;

using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.Server.Validators;

public class ChangeUserRoleValidator : AbstractValidator<ChangeUserRole> {
    public ChangeUserRoleValidator() {
        _ = RuleFor(x => x.TenantId).NotEmpty();
        _ = RuleFor(x => x.UserId).NotEmpty();
        // IsInEnum alone now accepts the Unknown sentinel (ordinal 0); reject it explicitly (TEN-1).
        _ = RuleFor(x => x.NewRole).IsInEnum().NotEqual(TenantRole.Unknown);
    }
}
