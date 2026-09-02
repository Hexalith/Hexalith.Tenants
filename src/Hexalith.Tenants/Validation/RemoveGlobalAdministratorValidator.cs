using FluentValidation;

using Hexalith.Tenants.Contracts.Commands;

namespace Hexalith.Tenants.Validation;

/// <summary>Validates the literal removal payload before command routing.</summary>
public sealed class RemoveGlobalAdministratorValidator : AbstractValidator<RemoveGlobalAdministrator>
{
    /// <summary>Initializes a new instance of the <see cref="RemoveGlobalAdministratorValidator"/> class.</summary>
    public RemoveGlobalAdministratorValidator()
        => RuleFor(static command => command.UserId)
            .Must(GlobalAdministratorIdentity.IsSupported)
            .WithMessage("UserId must be a non-blank literal of 256 characters or fewer without control characters.");
}
