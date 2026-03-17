using System.Text.Json;

using FluentValidation;
using FluentValidation.Results;

using Hexalith.EventStore.Server.Pipeline.Commands;
using Hexalith.Tenants.Contracts.Commands;

namespace Hexalith.Tenants.CommandApi.Validation;

public class TenantSubmitCommandValidator : AbstractValidator<SubmitCommand>
{
    public TenantSubmitCommandValidator(
        IValidator<AddUserToTenant> addUserToTenantValidator,
        IValidator<ChangeUserRole> changeUserRoleValidator,
        IValidator<SetTenantConfiguration> setTenantConfigurationValidator)
    {
        RuleFor(x => x).Custom((command, context) =>
        {
            switch (command.CommandType)
            {
                case nameof(AddUserToTenant):
                    ValidatePayload(command, context, addUserToTenantValidator);
                    break;
                case nameof(ChangeUserRole):
                    ValidatePayload(command, context, changeUserRoleValidator);
                    break;
                case nameof(SetTenantConfiguration):
                    ValidatePayload(command, context, setTenantConfigurationValidator);
                    break;
            }
        });
    }

    private static void ValidatePayload<TCommand>(
        SubmitCommand command,
        ValidationContext<SubmitCommand> context,
        IValidator<TCommand> validator)
        where TCommand : class
    {
        TCommand? payload;
        try
        {
            payload = JsonSerializer.Deserialize<TCommand>(command.Payload);
        }
        catch (JsonException ex)
        {
            context.AddFailure(nameof(SubmitCommand.Payload), $"Payload is not valid JSON for {typeof(TCommand).Name}: {ex.Message}");
            return;
        }

        if (payload is null)
        {
            context.AddFailure(nameof(SubmitCommand.Payload), $"Payload could not be deserialized to {typeof(TCommand).Name}.");
            return;
        }

        ValidationResult result = validator.Validate(payload);
        foreach (ValidationFailure failure in result.Errors)
        {
            context.AddFailure(new ValidationFailure($"Payload.{failure.PropertyName}", failure.ErrorMessage));
        }
    }
}
