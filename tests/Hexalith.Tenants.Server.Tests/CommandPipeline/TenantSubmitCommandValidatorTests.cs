using System.Text.Json;

using Hexalith.EventStore.Server.Pipeline.Commands;
using Hexalith.Tenants.Validation;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Server.Validators;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.CommandPipeline;

public class TenantSubmitCommandValidatorTests {
    private readonly TenantSubmitCommandValidator _validator = new(
        new AddUserToTenantValidator(),
        new ChangeUserRoleValidator(),
        new SetTenantConfigurationValidator(),
        new RemoveTenantConfigurationValidator(),
        new SetGlobalAdministratorValidator(),
        new RemoveGlobalAdministratorValidator());

    [Fact]
    public void AddUserToTenant_payload_with_empty_user_id_fails_validation() {
        SubmitCommand command = CreateCommand(new AddUserToTenant("acme", string.Empty, TenantRole.TenantReader));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Payload.UserId");
    }

    [Fact]
    public void AddUserToTenant_payload_with_missing_role_fails_validation() {
        SubmitCommand command = CreateCommand(
            nameof(AddUserToTenant),
            """{"TenantId":"acme","UserId":"user-1"}"""u8.ToArray());

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Payload.Role");
    }

    [Fact]
    public void AddUserToTenant_payload_with_invalid_enum_fails_validation() {
        SubmitCommand command = CreateCommand(new AddUserToTenant("acme", "user-1", (TenantRole)99));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Payload.Role");
    }

    [Fact]
    public void AddUserToTenant_payload_with_unrecognized_role_name_fails_validation() {
        SubmitCommand command = CreateCommand(
            nameof(AddUserToTenant),
            """{"TenantId":"acme","UserId":"user-1","Role":"GlobalAdministrator"}"""u8.ToArray());

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(SubmitCommand.Payload));
    }

    [Fact]
    public void ChangeUserRole_payload_with_invalid_enum_fails_validation() {
        SubmitCommand command = CreateCommand(new ChangeUserRole("acme", "user-1", (TenantRole)99));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Payload.NewRole");
    }

    [Fact]
    public void ChangeUserRole_payload_with_unknown_role_fails_validation() {
        SubmitCommand command = CreateCommand(new ChangeUserRole("acme", "user-1", TenantRole.Unknown));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Payload.NewRole");
    }

    [Fact]
    public void ChangeUserRole_payload_with_missing_role_fails_validation() {
        SubmitCommand command = CreateCommand(
            nameof(ChangeUserRole),
            """{"TenantId":"acme","UserId":"user-1"}"""u8.ToArray());

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Payload.NewRole");
    }

    [Fact]
    public void ChangeUserRole_payload_with_unrecognized_role_name_fails_validation() {
        SubmitCommand command = CreateCommand(
            nameof(ChangeUserRole),
            """{"TenantId":"acme","UserId":"user-1","NewRole":"GlobalAdministrator"}"""u8.ToArray());

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(SubmitCommand.Payload));
    }

    [Fact]
    public void SetTenantConfiguration_payload_with_null_key_fails_validation() {
        SubmitCommand command = CreateCommand(new SetTenantConfiguration("acme", null!, "value"));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Payload.Key");
    }

    [Fact]
    public void SetTenantConfiguration_payload_with_empty_key_fails_validation() {
        SubmitCommand command = CreateCommand(new SetTenantConfiguration("acme", string.Empty, "value"));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Payload.Key");
    }

    [Fact]
    public void SetTenantConfiguration_payload_with_whitespace_key_passes_validation() {
        SubmitCommand command = CreateCommand(new SetTenantConfiguration("acme", "   ", "value"));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SetTenantConfiguration_payload_with_key_at_max_length_passes_validation() {
        SubmitCommand command = CreateCommand(new SetTenantConfiguration("acme", new string('k', 256), "value"));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SetTenantConfiguration_payload_with_key_exceeding_max_length_fails_validation() {
        SubmitCommand command = CreateCommand(new SetTenantConfiguration("acme", new string('k', 257), "value"));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Payload.Key");
    }

    [Fact]
    public void SetTenantConfiguration_payload_with_null_value_fails_validation() {
        SubmitCommand command = CreateCommand(new SetTenantConfiguration("acme", "key", null!));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Payload.Value");
    }

    [Fact]
    public void SetTenantConfiguration_payload_with_empty_value_passes_validation() {
        SubmitCommand command = CreateCommand(new SetTenantConfiguration("acme", "key", string.Empty));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SetTenantConfiguration_payload_with_value_at_max_length_passes_validation() {
        SubmitCommand command = CreateCommand(new SetTenantConfiguration("acme", "key", new string('v', 1024)));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SetTenantConfiguration_payload_with_value_exceeding_max_length_fails_validation() {
        SubmitCommand command = CreateCommand(new SetTenantConfiguration("acme", "key", new string('v', 1025)));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Payload.Value");
    }

    [Fact]
    public void RemoveTenantConfiguration_payload_with_null_key_fails_validation() {
        SubmitCommand command = CreateCommand(new RemoveTenantConfiguration("acme", null!));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Payload.Key");
    }

    [Fact]
    public void RemoveTenantConfiguration_payload_with_empty_key_fails_validation() {
        SubmitCommand command = CreateCommand(new RemoveTenantConfiguration("acme", string.Empty));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Payload.Key");
    }

    [Fact]
    public void RemoveTenantConfiguration_payload_with_whitespace_key_passes_validation() {
        SubmitCommand command = CreateCommand(new RemoveTenantConfiguration("acme", "   "));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Unrelated_command_payload_is_ignored_by_tenant_submit_command_validator() {
        SubmitCommand command = CreateCommand(new CreateTenant("acme", "Acme Corp", "Test tenant"));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(nameof(SetGlobalAdministrator))]
    [InlineData(nameof(RemoveGlobalAdministrator))]
    public void Global_administrator_literal_identity_at_256_characters_passes(string commandType) {
        SubmitCommand command = CreateGlobalAdministratorCommand(commandType, new string('A', 256));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(nameof(SetGlobalAdministrator), "")]
    [InlineData(nameof(RemoveGlobalAdministrator), "   ")]
    [InlineData(nameof(SetGlobalAdministrator), "admin\nuser")]
    [InlineData(nameof(RemoveGlobalAdministrator), "admin\u0000user")]
    public void Global_administrator_invalid_literal_identity_fails(string commandType, string userId) {
        SubmitCommand command = CreateGlobalAdministratorCommand(commandType, userId);

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == "Payload.UserId");
    }

    [Theory]
    [InlineData(nameof(SetGlobalAdministrator))]
    [InlineData(nameof(RemoveGlobalAdministrator))]
    public void Global_administrator_literal_identity_at_257_characters_fails(string commandType) {
        SubmitCommand command = CreateGlobalAdministratorCommand(commandType, new string('A', 257));

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == "Payload.UserId");
    }

    [Theory]
    [InlineData(nameof(SetGlobalAdministrator), "System", "global-administrators", "global-administrators", nameof(SubmitCommand.Tenant))]
    [InlineData(nameof(RemoveGlobalAdministrator), "system", "Global-Administrators", "global-administrators", nameof(SubmitCommand.Domain))]
    [InlineData(nameof(SetGlobalAdministrator), "system", "global-administrators", "Global-Administrators", nameof(SubmitCommand.AggregateId))]
    [InlineData(nameof(RemoveGlobalAdministrator), "SYSTEM", "GLOBAL-ADMINISTRATORS", "GLOBAL-ADMINISTRATORS", nameof(SubmitCommand.Tenant))]
    public void Global_administrator_case_variant_envelope_fails_before_routing(
        string commandType,
        string tenant,
        string domain,
        string aggregateId,
        string expectedProperty) {
        SubmitCommand command = CreateGlobalAdministratorCommand(
            commandType,
            "  MixedCase.User  ",
            tenant,
            domain,
            aggregateId);

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == expectedProperty);
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

    private static SubmitCommand CreateCommand(string commandType, byte[] payload)
        => new(
            MessageId: Guid.NewGuid().ToString(),
            Tenant: "system",
            Domain: "tenants",
            AggregateId: "acme",
            CommandType: commandType,
            Payload: payload,
            CorrelationId: Guid.NewGuid().ToString(),
            UserId: "test-user",
            Extensions: null);

    private static SubmitCommand CreateGlobalAdministratorCommand(
        string commandType,
        string userId,
        string tenant = "system",
        string domain = "global-administrators",
        string aggregateId = "global-administrators")
    {
        object payload = commandType == nameof(SetGlobalAdministrator)
            ? new SetGlobalAdministrator(userId)
            : new RemoveGlobalAdministrator(userId);
        return new(
            MessageId: Guid.NewGuid().ToString(),
            Tenant: tenant,
            Domain: domain,
            AggregateId: aggregateId,
            CommandType: commandType,
            Payload: JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType()),
            CorrelationId: Guid.NewGuid().ToString(),
            UserId: "test-user",
            Extensions: null);
    }
}
