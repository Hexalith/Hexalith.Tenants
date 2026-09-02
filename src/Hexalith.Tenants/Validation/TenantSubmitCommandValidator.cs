using System.Text.Json;

using FluentValidation;
using FluentValidation.Results;

using Hexalith.EventStore.Server.Pipeline.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Identity;

namespace Hexalith.Tenants.Validation;

public class TenantSubmitCommandValidator : AbstractValidator<SubmitCommand> {
    public TenantSubmitCommandValidator(
        IValidator<AddUserToTenant> addUserToTenantValidator,
        IValidator<ChangeUserRole> changeUserRoleValidator,
        IValidator<SetTenantConfiguration> setTenantConfigurationValidator,
        IValidator<RemoveTenantConfiguration> removeTenantConfigurationValidator,
        IValidator<SetGlobalAdministrator> setGlobalAdministratorValidator,
        IValidator<RemoveGlobalAdministrator> removeGlobalAdministratorValidator) => RuleFor(x => x).Custom((command, context) => {
            switch (command.CommandType) {
                case nameof(AddUserToTenant):
                    ValidatePayload(command, context, addUserToTenantValidator);
                    break;
                case nameof(ChangeUserRole):
                    ValidatePayload(command, context, changeUserRoleValidator);
                    break;
                case nameof(SetTenantConfiguration):
                    ValidatePayload(command, context, setTenantConfigurationValidator);
                    break;
                case nameof(RemoveTenantConfiguration):
                    ValidatePayload(command, context, removeTenantConfigurationValidator);
                    break;
                case nameof(SetGlobalAdministrator):
                    ValidateGlobalAdministratorEnvelope(command, context);
                    ValidatePayload(command, context, setGlobalAdministratorValidator);
                    break;
                case nameof(RemoveGlobalAdministrator):
                    ValidateGlobalAdministratorEnvelope(command, context);
                    ValidatePayload(command, context, removeGlobalAdministratorValidator);
                    break;
            }
        });

    private static void ValidateGlobalAdministratorEnvelope(
        SubmitCommand command,
        ValidationContext<SubmitCommand> context)
    {
        if (!string.Equals(command.Tenant, TenantIdentity.DefaultTenantId, StringComparison.Ordinal))
        {
            context.AddFailure(nameof(SubmitCommand.Tenant), "Global-administrator commands require the fixed system tenant.");
        }

        if (!string.Equals(command.Domain, TenantIdentity.GlobalAdministratorsDomain, StringComparison.Ordinal))
        {
            context.AddFailure(nameof(SubmitCommand.Domain), "Global-administrator commands require the fixed global-administrators domain.");
        }

        if (!string.Equals(command.AggregateId, TenantIdentity.GlobalAdministratorsAggregateId, StringComparison.Ordinal))
        {
            context.AddFailure(nameof(SubmitCommand.AggregateId), "Global-administrator commands require the fixed global-administrators aggregate.");
        }
    }

    private static void ValidatePayload<TCommand>(
        SubmitCommand command,
        ValidationContext<SubmitCommand> context,
        IValidator<TCommand> validator)
        where TCommand : class {
        TCommand? payload;
        try {
            payload = JsonSerializer.Deserialize<TCommand>(command.Payload);
        }
        catch (JsonException ex) {
            context.AddFailure(nameof(SubmitCommand.Payload), $"Payload is not valid JSON for {typeof(TCommand).Name}: {ex.Message}");
            return;
        }

        if (payload is null) {
            context.AddFailure(nameof(SubmitCommand.Payload), $"Payload could not be deserialized to {typeof(TCommand).Name}.");
            return;
        }

        ValidationResult result = validator.Validate(payload);
        foreach (ValidationFailure failure in result.Errors) {
            context.AddFailure(new ValidationFailure($"Payload.{failure.PropertyName}", failure.ErrorMessage));
        }
    }
}
