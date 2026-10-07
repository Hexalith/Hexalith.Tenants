using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Tenants.Contracts.Identity;

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Hexalith.Tenants.Bootstrap;

/// <summary>
/// Obtains the delegated human credential of the configured bootstrap global administrator (EventStore Story 5.5).
/// </summary>
/// <remarks>
/// <para>
/// The global-administrator bootstrap is a human-delegated flow, never a workload or app-id grant: EventStore no longer
/// admits any internal caller by its Dapr application id, so the <c>BootstrapGlobalAdmin</c> command is authorized
/// only by a token that names the configured administrator as its subject.
/// </para>
/// <list type="bullet">
/// <item><description>Authority mode (<c>EventStore:Authentication:Authority</c> set): the administrator's own access
/// token, obtained with the configured <c>ClientId</c>, <c>Username</c>, and <c>Password</c>. A token whose
/// <c>sub</c> is not the configured administrator is discarded.</description></item>
/// <item><description>Symmetric Development mode (the shared <c>Authentication:JwtBearer</c> contract with a
/// <c>SigningKey</c>, in the Development environment only): a locally signed token for the configured administrator,
/// valid for <see cref="DevelopmentTokenLifetimeSeconds"/> seconds.</description></item>
/// </list>
/// <para>
/// When neither credential is available, nothing is issued and the command is not sent. Logs carry reasons only, never
/// tokens, passwords, or the administrator identifier.
/// </para>
/// </remarks>
internal sealed partial class TenantBootstrapCredentialProvider(
    IConfiguration configuration,
    IHostEnvironment environment,
    TimeProvider timeProvider,
    ILogger logger) {
    /// <summary>Gets the lifetime of the locally signed Development bootstrap credential.</summary>
    internal const int DevelopmentTokenLifetimeSeconds = 120;

    /// <summary>Gets the configuration section holding the shared JWT validation contract.</summary>
    internal const string JwtContractSection = "Authentication:JwtBearer";

    private const long MaxTokenResponseBytes = 64 * 1024;

    /// <summary>
    /// Acquires the delegated credential of the configured administrator.
    /// </summary>
    /// <param name="userId">The configured bootstrap global administrator.</param>
    /// <param name="httpClient">The HTTP client used to reach the authority's token endpoint.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The bearer token, or <see langword="null"/> when no delegated credential is available.</returns>
    public async Task<string?> AcquireAsync(string userId, HttpClient httpClient, CancellationToken cancellationToken) {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(httpClient);
        string? authority = configuration["EventStore:Authentication:Authority"];
        return string.IsNullOrWhiteSpace(authority)
            ? CreateDevelopmentToken(userId)
            : await AcquireFromAuthorityAsync(authority, userId, httpClient, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string?> AcquireFromAuthorityAsync(
        string authority,
        string userId,
        HttpClient httpClient,
        CancellationToken cancellationToken) {
        string? username = configuration["EventStore:Authentication:Username"];
        string? password = configuration["EventStore:Authentication:Password"];
        string clientId = configuration["EventStore:Authentication:ClientId"] ?? "hexalith-eventstore";
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) {
            Log.CredentialUnavailable(logger, "authority-credential-unconfigured");
            return null;
        }

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal) {
            ["grant_type"] = "password",
            ["client_id"] = clientId,
            ["username"] = username,
            ["password"] = password,
        });

        string tokenEndpoint = $"{authority.Trim().TrimEnd('/')}/protocol/openid-connect/token";
        using HttpResponseMessage response = await httpClient.PostAsync(tokenEndpoint, form, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) {
            Log.TokenRequestFailed(logger, (int)response.StatusCode);
            return null;
        }

        await response.Content.LoadIntoBufferAsync(MaxTokenResponseBytes, cancellationToken).ConfigureAwait(false);
        Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false)) {
            using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            string? token = document.RootElement.TryGetProperty("access_token", out JsonElement tokenElement)
                && tokenElement.ValueKind == JsonValueKind.String
                    ? tokenElement.GetString()
                    : null;
            if (string.IsNullOrWhiteSpace(token)) {
                Log.CredentialUnavailable(logger, "authority-response-invalid");
                return null;
            }

            // The credential must be the configured administrator's own: a token for another subject is not a
            // delegation of that administrator's authority.
            if (!string.Equals(ReadSubject(token), userId, StringComparison.Ordinal)) {
                Log.CredentialUnavailable(logger, "authority-subject-mismatch");
                return null;
            }

            return token;
        }
    }

    private string? CreateDevelopmentToken(string userId) {
        JwtBearerAuthenticationOptions contract = configuration.GetSection(JwtContractSection).Get<JwtBearerAuthenticationOptions>()
            ?? new JwtBearerAuthenticationOptions();
        if (!environment.IsDevelopment() || string.IsNullOrWhiteSpace(contract.SigningKey)) {
            Log.CredentialUnavailable(logger, "no-delegated-credential");
            return null;
        }

        if (!JwtBearerAuthenticationContract.Validate(contract, environment, JwtContractSection).Succeeded) {
            Log.CredentialUnavailable(logger, "development-contract-unusable");
            return null;
        }

        string? audience = !string.IsNullOrWhiteSpace(contract.Audience)
            ? contract.Audience.Trim()
            : contract.ValidAudiences.FirstOrDefault(static candidate => !string.IsNullOrWhiteSpace(candidate))?.Trim();
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        byte[] key = Encoding.UTF8.GetBytes(contract.SigningKey);
        try {
            var descriptor = new SecurityTokenDescriptor {
                Issuer = contract.Issuer.Trim(),
                Audience = audience,
                IssuedAt = now,
                NotBefore = now,
                Expires = now.AddSeconds(DevelopmentTokenLifetimeSeconds),
                Claims = new Dictionary<string, object>(StringComparer.Ordinal) {
                    [JwtRegisteredClaimNames.Sub] = userId,
                    [JwtRegisteredClaimNames.Jti] = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant(),
                    ["global_admin"] = "true",
                    ["eventstore:tenant"] = TenantIdentity.DefaultTenantId,
                },
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256),
            };
            return new JsonWebTokenHandler().CreateToken(descriptor);
        }
        finally {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static string? ReadSubject(string token) {
        try {
            return new JsonWebToken(token).Subject;
        }
        catch (ArgumentException) {
            // Includes SecurityTokenMalformedException: an unparseable token names no subject.
            return null;
        }
    }

    private static partial class Log {
        [LoggerMessage(
            EventId = 2005,
            Level = LogLevel.Warning,
            Message = "Bootstrap administrator token request failed: StatusCode={StatusCode}. The bootstrap command is not sent")]
        public static partial void TokenRequestFailed(ILogger logger, int statusCode);

        [LoggerMessage(
            EventId = 2006,
            Level = LogLevel.Warning,
            Message = "Bootstrap administrator credential unavailable: Reason={Reason}. The bootstrap command is not sent; configure the administrator's delegated credential")]
        public static partial void CredentialUnavailable(ILogger logger, string reason);
    }
}
