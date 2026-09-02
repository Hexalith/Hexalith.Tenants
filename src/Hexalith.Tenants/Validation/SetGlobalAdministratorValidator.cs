using FluentValidation;

using Hexalith.Tenants.Contracts.Commands;

namespace Hexalith.Tenants.Validation;

/// <summary>Validates the literal grant payload before command routing.</summary>
public sealed class SetGlobalAdministratorValidator : AbstractValidator<SetGlobalAdministrator>
{
    /// <summary>Initializes a new instance of the <see cref="SetGlobalAdministratorValidator"/> class.</summary>
    public SetGlobalAdministratorValidator()
        => RuleFor(static command => command.UserId)
            .Must(GlobalAdministratorIdentity.IsSupported)
            .WithMessage("UserId must be a non-blank literal of 256 characters or fewer without control characters.");
}
