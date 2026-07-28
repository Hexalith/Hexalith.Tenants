using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Queries;

namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>
/// Executes typed direct Tenants REST reads without exposing the backend transport to the browser.
/// </summary>
internal sealed class TenantsRestQueryClient(HttpClient httpClient) : ITenantsRestQueryClient
{
    private const string ProvenanceHeader = "X-Hexalith-Query-Provenance";
    private const string ProjectionVersionHeader = "X-Hexalith-Projection-Version";
    private const string IsStaleHeader = "X-Hexalith-Is-Stale";
    private const string IsDegradedHeader = "X-Hexalith-Is-Degraded";
    private const string ServedAtHeader = "X-Hexalith-Served-At";
    private const int MaximumMetadataValueLength = 4096;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <inheritdoc />
    public Task<TenantsRestQueryResponse<PaginatedResult<TenantSummary>>> ListTenantsAsync(
        ListTenantsQuery query,
        string? eTag,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return SendAsync<PaginatedResult<TenantSummary>>(
            BuildUri("/api/tenants", ("cursor", query.Cursor), ("pageSize", query.PageSize)),
            eTag,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<TenantsRestQueryResponse<TenantDetail>> GetTenantAsync(
        GetTenantQuery query,
        string? eTag,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.TenantId);
        return SendAsync<TenantDetail>(
            $"/api/tenants/{EscapeRouteValue(query.TenantId)}",
            eTag,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<TenantsRestQueryResponse<PaginatedResult<TenantMember>>> GetTenantUsersAsync(
        GetTenantUsersQuery query,
        string? eTag,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.TenantId);
        return SendAsync<PaginatedResult<TenantMember>>(
            BuildUri(
                $"/api/tenants/{EscapeRouteValue(query.TenantId)}/users",
                ("cursor", query.Cursor),
                ("pageSize", query.PageSize)),
            eTag,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<TenantsRestQueryResponse<PaginatedResult<UserTenantMembership>>> GetUserTenantsAsync(
        GetUserTenantsQuery query,
        string? eTag,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.UserId);
        return SendAsync<PaginatedResult<UserTenantMembership>>(
            BuildUri(
                $"/api/users/{EscapeRouteValue(query.UserId)}/tenants",
                ("cursor", query.Cursor),
                ("pageSize", query.PageSize)),
            eTag,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<TenantsRestQueryResponse<PaginatedResult<TenantAuditEntry>>> GetTenantAuditAsync(
        GetTenantAuditQuery query,
        string? eTag,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.TenantId);
        return SendAsync<PaginatedResult<TenantAuditEntry>>(
            BuildUri(
                $"/api/tenants/{EscapeRouteValue(query.TenantId)}/audit",
                ("from", query.From?.ToString("O", CultureInfo.InvariantCulture)),
                ("to", query.To?.ToString("O", CultureInfo.InvariantCulture)),
                ("category", query.Category?.ToString()),
                ("cursor", query.Cursor),
                ("pageSize", query.PageSize)),
            eTag,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<TenantsRestQueryResponse<PaginatedResult<GlobalAdministratorSummary>>> GetGlobalAdministratorsAsync(
        GetGlobalAdministratorsQuery query,
        string? eTag,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return SendAsync<PaginatedResult<GlobalAdministratorSummary>>(
            BuildUri("/api/global-administrators", ("cursor", query.Cursor), ("pageSize", query.PageSize)),
            eTag,
            cancellationToken);
    }

    private async Task<TenantsRestQueryResponse<TPayload>> SendAsync<TPayload>(
        string path,
        string? eTag,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        EntityTagHeaderValue? validator = NormalizeValidator(eTag);
        if (validator is not null)
        {
            request.Headers.IfNoneMatch.Add(validator);
        }

        HttpResponseMessage response;
        try
        {
            response = await httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            return Failure<TPayload>(TenantsRestQueryFailureKind.Timeout, (int)HttpStatusCode.ServiceUnavailable);
        }
        catch (HttpRequestException)
        {
            return Failure<TPayload>(TenantsRestQueryFailureKind.Unavailable, (int)HttpStatusCode.ServiceUnavailable);
        }

        using (response)
        {
            int statusCode = (int)response.StatusCode;
            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                QueryResponseMetadata notModifiedMetadata = ReadMetadata(response, isNotModified: true);
                ReadModelFreshnessState freshness = ResolveFreshness(notModifiedMetadata);
                if (!IsSupportedNotModified(notModifiedMetadata, freshness))
                {
                    return Failure<TPayload>(TenantsRestQueryFailureKind.InvalidMetadata, statusCode);
                }

                return new(default, notModifiedMetadata, freshness, TenantsRestQueryFailureKind.None, statusCode);
            }

            if (!response.IsSuccessStatusCode)
            {
                return Failure<TPayload>(MapFailure(response.StatusCode), statusCode);
            }

            TPayload? payload;
            try
            {
                using Stream content = await response.Content
                    .ReadAsStreamAsync(cancellationToken)
                    .ConfigureAwait(false);
                payload = await JsonSerializer
                    .DeserializeAsync<TPayload>(content, JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (JsonException)
            {
                return Failure<TPayload>(TenantsRestQueryFailureKind.InvalidPayload, statusCode);
            }
            catch (NotSupportedException)
            {
                return Failure<TPayload>(TenantsRestQueryFailureKind.InvalidPayload, statusCode);
            }

            if (payload is null)
            {
                return Failure<TPayload>(TenantsRestQueryFailureKind.InvalidPayload, statusCode);
            }

            QueryResponseMetadata responseMetadata = ReadMetadata(response, isNotModified: false);
            return new(
                payload,
                responseMetadata,
                ResolveFreshness(responseMetadata),
                TenantsRestQueryFailureKind.None,
                statusCode);
        }
    }

    private static QueryResponseMetadata ReadMetadata(HttpResponseMessage response, bool isNotModified)
    {
        QueryResponseProvenance provenance = ParseEnumHeader<QueryResponseProvenance>(response, ProvenanceHeader);
        string? eTag = provenance == QueryResponseProvenance.ProjectionBacked
            ? GetStrongETag(response)
            : null;
        string? projectionVersion = provenance == QueryResponseProvenance.ProjectionBacked
            ? GetBoundedHeader(response, ProjectionVersionHeader)
            : null;
        bool hasLifecycleHeader = response.Headers.Contains(ProjectionLifecyclePolicy.HeaderName);
        ProjectionLifecycleState parsedLifecycle = ParseEnumHeader<ProjectionLifecycleState>(
            response,
            ProjectionLifecyclePolicy.HeaderName);
        ProjectionLifecycleState lifecycle = ProjectionLifecyclePolicy.Normalize(parsedLifecycle, provenance);
        bool hasIsStaleHeader = response.Headers.Contains(IsStaleHeader);
        bool? isStale = provenance == QueryResponseProvenance.ProjectionBacked
            ? ParseBooleanHeader(response, IsStaleHeader)
            : null;
        bool hasIsDegradedHeader = response.Headers.Contains(IsDegradedHeader);
        bool? isDegraded = ParseBooleanHeader(response, IsDegradedHeader);
        DateTimeOffset? servedAt = ParseDateHeader(response, ServedAtHeader);

        bool malformedFreshness = (hasLifecycleHeader && parsedLifecycle == ProjectionLifecycleState.Unknown)
            || (hasIsStaleHeader && isStale is null)
            || (hasIsDegradedHeader && isDegraded is null);
        bool contradiction = lifecycle switch
        {
            ProjectionLifecycleState.Current => isStale == true,
            ProjectionLifecycleState.Stale => isStale == false,
            _ => false,
        };
        if (malformedFreshness || contradiction)
        {
            lifecycle = ProjectionLifecycleState.Unknown;
            isStale = null;
        }
        else if (lifecycle is not ProjectionLifecycleState.Unknown)
        {
            isStale = ProjectionLifecyclePolicy.ProjectIsStale(lifecycle, isStale);
        }

        isDegraded = ProjectionLifecyclePolicy.ProjectIsDegraded(lifecycle, isDegraded);
        return new QueryResponseMetadata(
            ETag: eTag,
            IsNotModified: isNotModified,
            IsStale: isStale,
            IsDegraded: isDegraded,
            ProjectionVersion: projectionVersion,
            ServedAt: servedAt)
        {
            Provenance = provenance,
            Lifecycle = lifecycle,
        };
    }

    private static ReadModelFreshnessState ResolveFreshness(QueryResponseMetadata metadata)
    {
        if (metadata.Provenance != QueryResponseProvenance.ProjectionBacked
            || metadata.IsDegraded == true)
        {
            return ReadModelFreshnessState.Unknown;
        }

        return metadata.Lifecycle switch
        {
            ProjectionLifecycleState.Current => ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Stale => ReadModelFreshnessState.Stale,
            ProjectionLifecycleState.Unknown => metadata.IsStale switch
            {
                false => ReadModelFreshnessState.Current,
                true => ReadModelFreshnessState.Stale,
                _ => ReadModelFreshnessState.Unknown,
            },
            _ => ReadModelFreshnessState.Unknown,
        };
    }

    private static bool IsSupportedNotModified(
        QueryResponseMetadata metadata,
        ReadModelFreshnessState freshness)
        => metadata.Provenance == QueryResponseProvenance.ProjectionBacked
            && metadata.ETag is not null
            && metadata.ProjectionVersion is not null
            && metadata.IsDegraded != true
            && freshness is ReadModelFreshnessState.Current or ReadModelFreshnessState.Stale;

    private static TenantsRestQueryResponse<TPayload> Failure<TPayload>(
        TenantsRestQueryFailureKind failureKind,
        int statusCode)
        => new(
            default,
            new QueryResponseMetadata(),
            ReadModelFreshnessState.Unknown,
            failureKind,
            statusCode);

    private static TenantsRestQueryFailureKind MapFailure(HttpStatusCode statusCode)
        => statusCode switch
        {
            HttpStatusCode.Unauthorized => TenantsRestQueryFailureKind.Unauthorized,
            HttpStatusCode.Forbidden => TenantsRestQueryFailureKind.Forbidden,
            HttpStatusCode.NotFound => TenantsRestQueryFailureKind.NotFound,
            HttpStatusCode.BadRequest => TenantsRestQueryFailureKind.InvalidRequest,
            _ => TenantsRestQueryFailureKind.Unavailable,
        };

    private static EntityTagHeaderValue? NormalizeValidator(string? eTag)
    {
        if (string.IsNullOrWhiteSpace(eTag)
            || eTag.Any(char.IsControl)
            || eTag.Contains('"', StringComparison.Ordinal))
        {
            return null;
        }

        return EntityTagHeaderValue.TryParse($"\"{eTag}\"", out EntityTagHeaderValue? parsed)
            && parsed is { IsWeak: false }
            ? parsed
            : null;
    }

    private static string? GetStrongETag(HttpResponseMessage response)
    {
        EntityTagHeaderValue? eTag = response.Headers.ETag;
        return eTag is null || eTag.IsWeak || string.Equals(eTag.Tag, "*", StringComparison.Ordinal)
            ? null
            : eTag.Tag.Trim('"');
    }

    private static TEnum ParseEnumHeader<TEnum>(HttpResponseMessage response, string name)
        where TEnum : struct, Enum
    {
        string? value = GetBoundedHeader(response, name);
        return value is not null
            && Enum.TryParse(value, ignoreCase: true, out TEnum parsed)
            && Enum.IsDefined(parsed)
            ? parsed
            : default;
    }

    private static bool? ParseBooleanHeader(HttpResponseMessage response, string name)
    {
        string? value = GetBoundedHeader(response, name);
        return bool.TryParse(value, out bool parsed) ? parsed : null;
    }

    private static DateTimeOffset? ParseDateHeader(HttpResponseMessage response, string name)
    {
        string? value = GetBoundedHeader(response, name);
        return DateTimeOffset.TryParseExact(
            value,
            "O",
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out DateTimeOffset parsed)
            ? parsed
            : null;
    }

    private static string? GetBoundedHeader(HttpResponseMessage response, string name)
    {
        if (!response.Headers.TryGetValues(name, out IEnumerable<string>? values))
        {
            return null;
        }

        string[] materialized = values.ToArray();
        if (materialized.Length != 1)
        {
            return null;
        }

        string value = materialized[0];
        return string.IsNullOrWhiteSpace(value)
            || value.Length > MaximumMetadataValueLength
            || value.Any(char.IsControl)
            ? null
            : value;
    }

    private static string EscapeRouteValue(string value)
        => Uri.EscapeDataString(value);

    private static string BuildUri(string path, params (string Name, object? Value)[] parameters)
    {
        string[] fields = parameters
            .Where(static parameter => parameter.Value is not null)
            .Select(static parameter => $"{parameter.Name}={Uri.EscapeDataString(Convert.ToString(parameter.Value, CultureInfo.InvariantCulture)!)}")
            .ToArray();
        return fields.Length == 0 ? path : $"{path}?{string.Join('&', fields)}";
    }
}
