using FluentValidation;

using Hexalith.Tenants.Contracts.Commands;

namespace Hexalith.Tenants.Server.Validators;

/// <summary>Validates the supported tenant-access recovery command.</summary>
public sealed class RefreshTenantAccessValidator : AbstractValidator<RefreshTenantAccess>
{
    /// <summary>Initializes command validation.</summary>
    public RefreshTenantAccessValidator()
    {
        _ = RuleFor(command => command.TenantId).NotEmpty();
    }
}
