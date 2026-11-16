using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>
/// Rejects stale or missing generator cache keys while retaining equal independent copies.
/// </summary>
public sealed class GeneratorModelCacheSafetyTests
{
    /// <summary>
    /// A different declaration identity, null, or an unrelated object cannot reuse an IR entry.
    /// </summary>
    /// <param name="kind">The generator model whose cache identity is checked.</param>
    [Theory]
    [InlineData(nameof(DomainModel))]
    [InlineData(nameof(CommandModel))]
    [InlineData(nameof(PropertyModel))]
    [InlineData(nameof(BadgeMappingEntry))]
    [InlineData(nameof(ColumnModel))]
    [InlineData(nameof(FormFieldModel))]
    [InlineData(nameof(CommandFormModel))]
    [InlineData(nameof(CommandRendererModel))]
    [InlineData(nameof(FluxorModel))]
    [InlineData(nameof(CommandFluxorModel))]
    [InlineData(nameof(RazorModel))]
    [InlineData(nameof(RegistrationModel))]
    [Trait("Category", "MutationErrorRegression")]
    public void ChangedOrMissingIdentityCannotReuseCachedMetadata(string kind)
    {
        object cached = CreateModel(kind, "Original");
        object independentCopy = CreateModel(kind, "Original");
        object changedIdentity = CreateModel(kind, "Replacement");

        // Independent parses are valid cache hits. These controls also prevent an
        // implementation that "fixes" stale reuse by rejecting every comparison.
        cached.Equals(cached).ShouldBeTrue();
        ReferenceEquals(cached, independentCopy).ShouldBeFalse();
        cached.Equals(independentCopy).ShouldBeTrue();
        independentCopy.Equals(cached).ShouldBeTrue();
        cached.GetHashCode().ShouldBe(independentCopy.GetHashCode());

        // Keep all non-identity metadata equal: agreeing labels or presentation
        // options must never hide that this entry belongs to another declaration.
        cached.Equals(changedIdentity).ShouldBeFalse();
        changedIdentity.Equals(cached).ShouldBeFalse();
        cached.Equals(null).ShouldBeFalse();
        cached.Equals(new object()).ShouldBeFalse();
    }

    private static object CreateModel(string kind, string identity) => kind switch
    {
        nameof(DomainModel) => new DomainModel(
            identity, "CacheFixtures", "Sales", "Sales label", "ActionQueue", default,
            projectionRoleWhenState: "Pending", displayName: "Work item", displayGroupName: "Operations",
            sourceFilePath: "/repo/domain.cs", sourceLine: 4, sourceColumn: 2,
            emptyStateCtaCommandTypeName: "CreateCommand"),
        nameof(CommandModel) => new CommandModel(
            identity, "CacheFixtures", "Sales", "Sales label", "Submit", default, default, default,
            iconName: "Regular.Size16.Play", isDestructive: true, destructiveConfirmTitle: "Confirm",
            destructiveConfirmBody: "Confirm item", authorizationPolicyName: "CanSubmit"),
        nameof(PropertyModel) => new PropertyModel(
            identity, "String", false, false, "Payload", default, fieldGroup: "Details",
            description: "Field help", sourceTypeName: "global::System.String", fieldGroupDescription: "Group help"),
        nameof(BadgeMappingEntry) => new BadgeMappingEntry(identity, "Success"),
        nameof(ColumnModel) => new ColumnModel(
            identity, "Header", TypeCategory.Text, null, false, default,
            priority: 3, fieldGroup: "Details", description: "Column help"),
        nameof(FormFieldModel) => new FormFieldModel(
            identity, "String", FormFieldTypeCategory.TextInput, "Label", false, true, null,
            hasExplicitDisplayName: true, fieldGroup: "Details", description: "Field help", fieldGroupDescription: "Group help"),
        nameof(CommandFormModel) => new CommandFormModel(
            identity, "CacheFixtures", "Sales", "CacheFixtures.SubmitCommand", "Submit", default, "CanSubmit"),
        nameof(CommandRendererModel) => new CommandRendererModel(
            identity, "CacheFixtures", "Sales", CommandDensity.FullPage, "Regular.Size16.Play", "Submit",
            "/commands/Sales/SubmitCommand", "CacheFixtures.SubmitCommand", default, default,
            "SubmitCommandForm", "SubmitCommandActions", "SubmitCommandState", "SubmitCommandSubscriber",
            isDestructive: true, destructiveConfirmTitle: "Confirm", destructiveConfirmBody: "Confirm item",
            authorizationPolicyName: "CanSubmit"),
        nameof(FluxorModel) => new FluxorModel(identity, "CacheFixtures", "WorkItemState", "WorkItems"),
        nameof(CommandFluxorModel) => new CommandFluxorModel(
            identity, "CacheFixtures", "SubmitCommandState", "Submit", "SubmitCommandActions",
            "SubmitCommandReducers", "CacheFixtures.SubmitCommand", "CacheFixtures.SubmitCommandLifecycleState"),
        nameof(RazorModel) => new RazorModel(
            identity, "CacheFixtures", "Sales", default, entityLabel: "Work item", entityPluralLabel: "Work items",
            emptyStateCtaCommandName: "CreateCommand"),
        nameof(RegistrationModel) => new RegistrationModel(
            identity, "WorkItem", "CacheFixtures", "Work item", isCommand: true,
            authorizationPolicyName: "CanSubmit", hasFullPageRoute: true),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown cache model"),
    };
}
