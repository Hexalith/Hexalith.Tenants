using FluentValidation;

using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.Server.Validators;

public class AddUserToTenantValidator : AbstractValidator<AddUserToTenant> {
    public AddUserToTenantValidator() {
        _ = RuleFor(x => x.TenantId).NotEmpty();
        _ = RuleFor(x => x.UserId).NotEmpty();
        // IsInEnum alone now accepts the Unknown sentinel (ordinal 0); reject it explicitly (TEN-1).
        _ = RuleFor(x => x.Role).IsInEnum().NotEqual(TenantRole.Unknown);
    }
}
