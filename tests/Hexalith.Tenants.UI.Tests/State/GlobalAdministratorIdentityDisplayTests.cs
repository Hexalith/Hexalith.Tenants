using Hexalith.Tenants.Contracts.Identity;
using Hexalith.Tenants.UI.State.GlobalAdministrators;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class GlobalAdministratorIdentityDisplayTests
{
    [Fact]
    public void PublicApiRetainsTheSingleArgumentVisualEncoderOnly()
    {
        typeof(GlobalAdministratorIdentityDisplay)
            .GetMethods()
            .Where(method => method.Name == nameof(GlobalAdministratorIdentityDisplay.Encode))
            .ShouldHaveSingleItem()
            .GetParameters()
            .Length.ShouldBe(1);
        typeof(GlobalAdministratorIdentityDisplay).GetMethod("EncodeAccessible").ShouldBeNull();
    }

    [Theory]
    [InlineData(" user  id ", " user  id ")]
    [InlineData(@"user\{U+200D}", @"user\\{U+200D}")]
    [InlineData("user\u200D", @"user\{U+200D}")]
    [InlineData("user\u034F", @"user\{U+034F}")]
    [InlineData("user\uFE0F", @"user\{U+FE0F}")]
    [InlineData("user\U000E0100", @"user\{U+000E0100}")]
    [InlineData("user\U000E0001", @"user\{U+000E0001}")]
    public void Encode_PreservesLiteralWhitespaceAndDistinguishesFormatScalars(
        string userId,
        string expected)
    {
        GlobalAdministratorUserId.IsSupported(userId).ShouldBeTrue();

        GlobalAdministratorIdentityDisplay.Encode(userId).ShouldBe(expected);
    }

    [Theory]
    [InlineData("before\u0001after", @"before\{U+0001}after")]
    [InlineData("before\tafter", @"before\{U+0009}after")]
    [InlineData("before\nafter", @"before\{U+000A}after")]
    public void Encode_TokenizesControlScalars(string userId, string expected)
        => GlobalAdministratorIdentityDisplay.Encode(userId).ShouldBe(expected);

    [Fact]
    public void AccessibleEncodingTokenizesRepeatedAndEdgeWhitespaceWithoutChangingVisualEncoding()
    {
        const string userId = "  user  id ";

        GlobalAdministratorIdentityDisplay.Encode(userId).ShouldBe(userId);
        GlobalAdministratorIdentityDisplay.EncodeAccessible(userId).ShouldBe(
            @"\{U+0020}\{U+0020}user\{U+0020}\{U+0020}id\{U+0020}");
    }

    [Fact]
    public void IsSupported_RejectsUnpairedUtf16Surrogates()
    {
        GlobalAdministratorUserId.IsSupported(new string(['\uD800'])).ShouldBeFalse();
        GlobalAdministratorUserId.IsSupported(new string(['\uDC00'])).ShouldBeFalse();
        GlobalAdministratorUserId.IsSupported(string.Concat("before", new string(['\uD800']), "after"))
            .ShouldBeFalse();
    }

    [Fact]
    public void Encode_IsInjectiveForLiteralEscapeLookingTextAndFormatScalar()
    {
        string formatScalar = GlobalAdministratorIdentityDisplay.Encode("user\u200D");
        string literalText = GlobalAdministratorIdentityDisplay.Encode(@"user\{U+200D}");

        formatScalar.ShouldBe(@"user\{U+200D}");
        literalText.ShouldBe(@"user\\{U+200D}");
        literalText.ShouldNotBe(formatScalar);
    }
}
