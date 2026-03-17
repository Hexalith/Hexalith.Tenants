using System.Text.Json;

using Hexalith.EventStore.Server.Pipeline.Commands;
using Hexalith.Tenants.CommandApi.Validation;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Server.Validators;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.CommandPipeline;

public class TenantSubmitCommandValidatorTests
{
    private readonly TenantSubmitCommandValidator _validator = new(
        new AddUserToTenantValidator(),
        new ChangeUserRoleValidator(),
        new SetTenantConfigurationValidator(),
        new RemoveTenantConfigurationValidator());

    [Fact]
    public void AddUserToTenant_payload_with_empty_user_id_fails_validation()
    {
        SubmitCommand command = CreateCommand(new AddUserToTenant("acme", string.Empty, TenantRole.TenantReader));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Payload.UserId");
    }

    [Fact]
    public void ChangeUserRole_payload_with_invalid_enum_fails_validation()
    {
        SubmitCommand command = CreateCommand(new ChangeUserRole("acme", "user-1", (TenantRole)99));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Payload.NewRole");
    }

    [Fact]
    public void SetTenantConfiguration_payload_with_empty_key_fails_validation()
    {
        SubmitCommand command = CreateCommand(new SetTenantConfiguration("acme", string.Empty, "value"));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Payload.Key");
    }

    [Fact]
    public void SetTenantConfiguration_payload_with_whitespace_key_passes_validation()
    {
        SubmitCommand command = CreateCommand(new SetTenantConfiguration("acme", "   ", "value"));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SetTenantConfiguration_payload_with_null_value_fails_validation()
    {
        SubmitCommand command = CreateCommand(new SetTenantConfiguration("acme", "key", null!));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Payload.Value");
    }

    [Fact]
    public void RemoveTenantConfiguration_payload_with_null_key_fails_validation()
    {
        SubmitCommand command = CreateCommand(new RemoveTenantConfiguration("acme", null!));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Payload.Key");
    }

    [Fact]
    public void RemoveTenantConfiguration_payload_with_empty_key_fails_validation()
    {
        SubmitCommand command = CreateCommand(new RemoveTenantConfiguration("acme", string.Empty));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Payload.Key");
    }

    [Fact]
    public void Unrelated_command_payload_is_ignored_by_tenant_submit_command_validator()
    {
        SubmitCommand command = CreateCommand(new CreateTenant("acme", "Acme Corp", "Test tenant"));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    private static SubmitCommand CreateCommand<T>(T payload)
        where T : notnull
        => new(
            MessageId: Guid.NewGuid().ToString(),
            Tenant: "system",
            Domain: "tenants",
            AggregateId: payload is CreateTenant createTenant ? createTenant.TenantId : ((dynamic)payload).TenantId,
            CommandType: typeof(T).Name,
            Payload: JsonSerializer.SerializeToUtf8Bytes(payload),
            CorrelationId: Guid.NewGuid().ToString(),
            UserId: "test-user",
            Extensions: null);
}
