using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Problems;
using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed class TenantsQueryApiClient(HttpClient httpClient) : ITenantsQueryApiClient {
    private const string ProjectionVersionHeaderName = "X-Hexalith-Projection-Version";
    private const string ServedAtHeaderName = "X-Hexalith-Served-At";
    private const string IsStaleHeaderName = "X-Hexalith-Is-Stale";
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<EventStoreQueryResult<T>> SendAsync<T>(
        TenantsQueryApiRequest request,
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request.Path);
        string? normalizedIfNoneMatch = NormalizeIfNoneMatch(ifNoneMatch);
        if (normalizedIfNoneMatch is not null) {
            httpRequest.Headers.IfNoneMatch.ParseAdd(normalizedIfNoneMatch);
        }

        using HttpResponseMessage response = await SendCoreAsync(httpRequest, cancellationToken)
            .ConfigureAwait(false);

        string? eTag = GetETag(response);
        QueryResponseMetadata metadata = CreateMetadata(response, eTag, response.StatusCode == HttpStatusCode.NotModified);

        if (response.StatusCode == HttpStatusCode.NotModified) {
            return new EventStoreQueryResult<T>(null, default, IsNotModified: true, eTag) {
                Metadata = metadata,
            };
        }

        if (!response.IsSuccessStatusCode) {
            await ThrowGatewayExceptionAsync(response, cancellationToken).ConfigureAwait(false);
        }

        T? payload;
        try {
            payload = await response.Content
                .ReadFromJsonAsync<T>(s_jsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException ex) {
            throw new EventStoreGatewayException(
                (int)response.StatusCode,
                response.ReasonPhrase ?? "OK",
                detail: "Tenant query response payload could not be deserialized.",
                innerException: ex);
        }

        if (payload is null) {
            throw new EventStoreGatewayException(
                (int)response.StatusCode,
                response.ReasonPhrase ?? "OK",
                detail: "Tenant query response did not contain a payload.");
        }

        return new EventStoreQueryResult<T>(null, payload, IsNotModified: false, eTag) {
            Metadata = metadata,
        };
    }

    // Translate transport-level failures into a 503 EventStoreGatewayException so the gateway's
    // existing ServiceUnavailable -> degraded/unavailable surface mapping renders a fail-closed
    // state, instead of letting a raw HttpRequestException or timeout escape into the Blazor
    // circuit. Genuine caller cancellation is propagated unchanged.
    private async Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken) {
        try {
            return await httpClient
                .SendAsync(httpRequest, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (HttpRequestException ex) {
            throw new EventStoreGatewayException(
                (int)HttpStatusCode.ServiceUnavailable,
                "Service Unavailable",
                detail: "The tenant query service is unavailable.",
                innerException: ex);
        }
        catch (TaskCanceledException ex) {
            // The caller's token did not fire, so this is an HttpClient timeout, not a cancellation.
            throw new EventStoreGatewayException(
                (int)HttpStatusCode.ServiceUnavailable,
                "Service Unavailable",
                detail: "The tenant query service did not respond in time.",
                innerException: ex);
        }
    }

    private static QueryResponseMetadata CreateMetadata(HttpResponseMessage response, string? eTag, bool isNotModified) {
        string? projectionVersion = response.Headers.TryGetValues(ProjectionVersionHeaderName, out IEnumerable<string>? versions)
            ? versions.FirstOrDefault()
            : null;
        DateTimeOffset? servedAt = null;
        if (response.Headers.TryGetValues(ServedAtHeaderName, out IEnumerable<string>? servedAtValues)
            && DateTimeOffset.TryParse(
                servedAtValues.FirstOrDefault(),
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out DateTimeOffset parsedServedAt)) {
            servedAt = parsedServedAt;
        }

        bool? isStale = null;
        if (response.Headers.TryGetValues(IsStaleHeaderName, out IEnumerable<string>? isStaleValues)
            && bool.TryParse(isStaleValues.FirstOrDefault(), out bool parsedIsStale)) {
            isStale = parsedIsStale;
        }

        return new QueryResponseMetadata(
            ETag: eTag,
            IsNotModified: isNotModified,
            IsStale: isStale,
            ProjectionVersion: projectionVersion,
            ServedAt: servedAt);
    }

    private static string? GetETag(HttpResponseMessage response) {
        EntityTagHeaderValue? eTag = response.Headers.ETag;
        if (eTag is null) {
            return null;
        }

        if (eTag.IsWeak) {
            throw new EventStoreGatewayException(
                (int)response.StatusCode,
                response.ReasonPhrase ?? "OK",
                detail: "Tenant query response contained an unsupported weak ETag.");
        }

        return eTag.Tag;
    }

    private static string? NormalizeIfNoneMatch(string? ifNoneMatch) {
        if (string.IsNullOrWhiteSpace(ifNoneMatch)) {
            return null;
        }

        string value = ifNoneMatch.Trim();
        if (value == "*"
            || value.Contains(',', StringComparison.Ordinal)
            || value.StartsWith("W/", StringComparison.OrdinalIgnoreCase)) {
            return null;
        }

        if (value.StartsWith('"')) {
            if (!EntityTagHeaderValue.TryParse(value, out EntityTagHeaderValue? parsed)
                || parsed is null
                || parsed.IsWeak
                || parsed.Tag == "*") {
                return null;
            }

            return parsed.Tag;
        }

        if (value.Any(static c => char.IsWhiteSpace(c) || char.IsControl(c) || c is '"' or ',')) {
            return null;
        }

        return $"\"{value}\"";
    }

    private static async Task ThrowGatewayExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken) {
        JsonElement? problem = null;
        try {
            problem = await response.Content
                .ReadFromJsonAsync<JsonElement>(s_jsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException) {
        }

        string title = response.ReasonPhrase ?? "Tenant query failed";
        string? detail = null;
        string? correlationId = null;
        string? reasonCode = null;
        if (problem is { ValueKind: JsonValueKind.Object } value) {
            title = TryGetString(value, "title") ?? title;
            detail = TryGetString(value, "detail");
            correlationId = TryGetString(value, GatewayProblemDetailsExtensions.CorrelationId);
            reasonCode = TryGetString(value, GatewayProblemDetailsExtensions.ReasonCode);
        }

        throw new EventStoreGatewayException(
            (int)response.StatusCode,
            title,
            detail: detail,
            correlationId: correlationId,
            reasonCode: reasonCode);
    }

    private static string? TryGetString(JsonElement value, string propertyName)
        => value.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
}
