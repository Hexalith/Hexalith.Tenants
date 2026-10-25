using System.Security.Authentication;

using Hexalith.Tenants.IntegrationTests.Fixtures;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Checks failure diagnostics without starting a topology or retaining private exception text.</summary>
public sealed class AspireTopologyTlsDiagnosticsTests
{
    [Theory]
    [InlineData("UntrustedRoot")]
    [InlineData("PartialChain")]
    [InlineData("RemoteCertificateNameMismatch")]
    [InlineData("NotTimeValid")]
    public void NestedTlsCausesRemainVisibleWithoutCredentialsOrUrls(string cause)
    {
        const string credential = "credential-sentinel-8759";
        const string privateUrl = "https://private.invalid/diagnostic?access_token=private-sentinel";
        var exception = new HttpRequestException(HttpRequestError.SecureConnectionError,
            $"Bearer {credential} {privateUrl}",
            new AuthenticationException($"Certificate fault {cause}: password={credential}",
                new InvalidOperationException(privateUrl)));

        string diagnostic = AspireTopologyFixture.DescribeTransportFailure(exception);

        diagnostic.ShouldContain(nameof(HttpRequestError.SecureConnectionError));
        diagnostic.ShouldContain(nameof(AuthenticationException));
        diagnostic.ShouldContain(cause);
        diagnostic.ShouldNotContain(credential);
        diagnostic.ShouldNotContain(privateUrl);
        diagnostic.ShouldNotContain("Bearer");
        diagnostic.ShouldNotContain("access_token");
        diagnostic.Length.ShouldBeLessThanOrEqualTo(256);
    }

    [Fact]
    public void ExceptionTraversalIsBoundedAndNeverCopiesDeepMessages()
    {
        var exception = new HttpRequestException(HttpRequestError.SecureConnectionError, "outer-private",
            new AuthenticationException("UntrustedRoot",
                new InvalidOperationException("nested-private",
                    new IOException("third-private", new AuthenticationException("NotTimeValid deep-private")))));

        string diagnostic = AspireTopologyFixture.DescribeTransportFailure(exception);

        diagnostic.ShouldContain("UntrustedRoot");
        diagnostic.ShouldNotContain("NotTimeValid");
        diagnostic.ShouldNotContain("private");
        diagnostic.Length.ShouldBeLessThanOrEqualTo(256);
    }
}
