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
    private const int MaximumETagValueLength = 1024;
    private const int MaximumMetadataValueLength = 4096;

    /// <summary>Upper bound on a Problem Details body inspected for the invalid-cursor sentinel.</summary>
    private const int MaximumProblemDetailsLength = 8192;

    /// <summary>Top-level property where ASP.NET serializes the Problem Details extension set.</summary>
    private const string ProblemDetailsReasonProperty = "reason";

    /// <summary>Shared <c>QueryAdapterFailureReason.InvalidCursor</c> sentinel value.</summary>
    private const string InvalidCursorReason = "invalid-cursor";

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
        using var request = new HttpRequestMessage(HttpMethod.Get, CreateRequestUri(path));
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
        catch (OperationCanceledException)
        {
            // Not the caller's token: an internal HttpClient timeout or a linked source inside a delegating
            // handler. Only TaskCanceledException was caught before, so a plain OperationCanceledException
            // propagated raw out of the client and past every caller's cancellation-only handler.
            return Failure<TPayload>(TenantsRestQueryFailureKind.Timeout, (int)HttpStatusCode.ServiceUnavailable);
        }
        catch (HttpRequestException)
        {
            return Failure<TPayload>(TenantsRestQueryFailureKind.Unavailable, (int)HttpStatusCode.ServiceUnavailable);
        }
        catch (IOException)
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
                if (!IsSupportedNotModified(notModifiedMetadata, freshness, validator))
                {
                    return Failure<TPayload>(TenantsRestQueryFailureKind.InvalidMetadata, statusCode);
                }

                return new(default, notModifiedMetadata, freshness, TenantsRestQueryFailureKind.None, statusCode);
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                TenantsRestQueryFailureKind failure = MapFailure(response.StatusCode);
                if (failure == TenantsRestQueryFailureKind.InvalidRequest
                    && await HasInvalidCursorSignalAsync(response, cancellationToken).ConfigureAwait(false))
                {
                    failure = TenantsRestQueryFailureKind.InvalidCursor;
                }

                return Failure<TPayload>(failure, statusCode);
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
            catch (OperationCanceledException)
            {
                return Failure<TPayload>(TenantsRestQueryFailureKind.Timeout, statusCode);
            }
            catch (HttpRequestException)
            {
                return Failure<TPayload>(TenantsRestQueryFailureKind.Unavailable, statusCode);
            }
            catch (IOException)
            {
                return Failure<TPayload>(TenantsRestQueryFailureKind.Unavailable, statusCode);
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

            if (!HasValidPayloadShape(payload))
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

            // An absent lifecycle header with a definite X-Hexalith-Is-Stale is deliberately classified from
            // that header, not collapsed to Unknown. X-Hexalith-Is-Stale is the platform's freshness wire
            // signal -- ToQueryResponseMetadata emits current/stale/unknown through it, and Aging is dormant
            // on the wire -- so a projection-backed, non-degraded `false` is real freshness evidence rather
            // than an inference from HTTP success. Absent (null) still yields Unknown, and the
            // non-projection/degraded cases above already fail closed before reaching here.
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
        ReadModelFreshnessState freshness,
        EntityTagHeaderValue? requestValidator)
        => metadata.Provenance == QueryResponseProvenance.ProjectionBacked
            && requestValidator is { IsWeak: false }
            && metadata.ETag is not null
            && string.Equals(metadata.ETag, requestValidator.Tag.Trim('"'), StringComparison.Ordinal)
            && metadata.ProjectionVersion is not null
            && metadata.IsDegraded != true
            && freshness is ReadModelFreshnessState.Current or ReadModelFreshnessState.Stale;

    private static bool HasValidPayloadShape<TPayload>(TPayload payload)
        => payload switch
        {
            PaginatedResult<TenantSummary> page => IsValidPage(page.Items, page.Cursor, page.HasMore),
            PaginatedResult<TenantMember> page => IsValidPage(page.Items, page.Cursor, page.HasMore),
            PaginatedResult<UserTenantMembership> page => IsValidPage(page.Items, page.Cursor, page.HasMore),
            PaginatedResult<TenantAuditEntry> page => IsValidPage(page.Items, page.Cursor, page.HasMore),
            PaginatedResult<GlobalAdministratorSummary> page => IsValidPage(page.Items, page.Cursor, page.HasMore),
            _ => true,
        };

    private static bool IsValidPage<TItem>(IReadOnlyList<TItem>? items, string? cursor, bool hasMore)
        => items is not null && (!hasMore || !string.IsNullOrWhiteSpace(cursor));

    private static TenantsRestQueryResponse<TPayload> Failure<TPayload>(
        TenantsRestQueryFailureKind failureKind,
        int statusCode)
        => new(
            default,
            new QueryResponseMetadata(),
            ReadModelFreshnessState.Unknown,
            failureKind,
            statusCode);

    /// <summary>
    /// Detects the shared <c>invalid-cursor</c> sentinel on a <c>400</c> response.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reads exactly one top-level Problem Details property — <c>reason</c>, where ASP.NET serializes the
    /// extension set — and compares it to a single known literal. Nothing from the body is retained,
    /// returned, rendered, or logged; the method yields only a boolean. That keeps the "Problem Details are
    /// neither exposed nor logged" rule intact while still honouring the explicit contract signal the
    /// service actually sends (<c>QueryExecutionFailedExceptionHandler</c> sets
    /// <c>Extensions[reason] = QueryAdapterFailureReason.InvalidCursor</c>).
    /// </para>
    /// <para>
    /// Any parse failure, oversized body, or absent property is treated as "no signal", so an
    /// undifferentiated 400 stays <see cref="TenantsRestQueryFailureKind.InvalidRequest"/> and never
    /// triggers a silent page-one retry.
    /// </para>
    /// </remarks>
    private static async Task<bool> HasInvalidCursorSignalAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength > MaximumProblemDetailsLength)
        {
            return false;
        }

        try
        {
            using Stream content = await response.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            using JsonDocument document = await JsonDocument
                .ParseAsync(content, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty(ProblemDetailsReasonProperty, out JsonElement reason)
                && reason.ValueKind == JsonValueKind.String
                && string.Equals(reason.GetString(), InvalidCursorReason, StringComparison.Ordinal);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // A body that cannot be read or parsed carries no signal. Absence of proof is not proof of an
            // invalid cursor, so the caller keeps the conservative InvalidRequest classification.
            return false;
        }
    }

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
            || eTag.Length > MaximumETagValueLength
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
        if (!response.Headers.TryGetValues("ETag", out IEnumerable<string>? values))
        {
            return null;
        }

        string[] materialized = values.ToArray();
        if (materialized.Length != 1
            || materialized[0].Length > MaximumETagValueLength + 2
            || materialized[0].Any(char.IsControl)
            || !EntityTagHeaderValue.TryParse(materialized[0], out EntityTagHeaderValue? eTag))
        {
            return null;
        }

        string? tag = eTag.Tag.Trim('"');
        return eTag.IsWeak
            || string.Equals(eTag.Tag, "*", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(tag)
            || tag.Length > MaximumETagValueLength
            || tag.Any(char.IsControl)
            ? null
            : tag;
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
        => value.All(static character => character == '.')
            ? string.Concat(Enumerable.Repeat("%2E", value.Length))
            : Uri.EscapeDataString(value);

    private Uri CreateRequestUri(string path)
    {
        Uri baseAddress = httpClient.BaseAddress
            ?? throw new InvalidOperationException("The Tenants REST client requires an absolute base address.");

        // Keep any path prefix on the configured base address. Building from the authority alone discarded
        // it, so a gateway or reverse-proxy address such as https://host/tenants-api/ silently retargeted
        // every read at https://host/api/... -- which 404s, and a 404 renders as authorization-safe absence
        // rather than as the misconfiguration it is. Canonicalization stays disabled so the dot-only route
        // escaping applied by EscapeRouteValue is not undone.
        string basePath = baseAddress.GetLeftPart(UriPartial.Path).TrimEnd('/');
        return new Uri(
            basePath + path,
            new UriCreationOptions { DangerousDisablePathAndQueryCanonicalization = true });
    }

    private static string BuildUri(string path, params (string Name, object? Value)[] parameters)
    {
        string[] fields = parameters
            .Where(static parameter => parameter.Value is not null)
            .Select(static parameter => $"{parameter.Name}={Uri.EscapeDataString(Convert.ToString(parameter.Value, CultureInfo.InvariantCulture)!)}")
            .ToArray();
        return fields.Length == 0 ? path : $"{path}?{string.Join('&', fields)}";
    }
}
