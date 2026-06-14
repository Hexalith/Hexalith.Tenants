using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.Contracts.Authorization;
using Hexalith.EventStore.Contracts.Problems;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Identity;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Queries;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using IQueryCursorCodec = Hexalith.EventStore.Client.Queries.IQueryCursorCodec;

namespace Hexalith.Tenants.Controllers;

/// <summary>
/// Thin REST controller that translates GET endpoints into in-process query dispatches via the platform
/// <see cref="DomainQueryDispatcher"/>. Query logic and authorization live in the per-query
/// <see cref="Hexalith.Tenants.Queries.Handlers.TenantQueryHandlerBase"/> implementations.
/// </summary>
[ApiController]
[Authorize]
[Route("api/tenants")]
[Tags("Tenants")]
public sealed partial class TenantsQueryController(
    IQueryCursorCodec cursorCodec,
    ITenantValidator tenantValidator,
    IRbacValidator rbacValidator,
    ILogger<TenantsQueryController> logger) : ControllerBase {
    internal const string ProjectionVersionHeaderName = "X-Hexalith-Projection-Version";
    internal const string ServedAtHeaderName = "X-Hexalith-Served-At";
    private const string SystemTenant = "system";
    private static readonly System.Text.RegularExpressions.Regex _identifierRegex = new(@"^[a-zA-Z0-9][a-zA-Z0-9._-]{0,255}$", System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// Gets full details for a specific tenant.
    /// </summary>
    [HttpGet("{tenantId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default) {
        if (!IsValidIdentifier(tenantId)) {
            return BadRequest();
        }

        string? userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        var envelope = new QueryEnvelope(
            tenantId: SystemTenant,
            domain: GetTenantQuery.Domain,
            aggregateId: tenantId,
            queryType: GetTenantQuery.QueryType,
            payload: [],
            correlationId: GetCorrelationId(),
            userId: userId,
            entityId: tenantId);

        return await DispatchAsync(envelope, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets tenant audit entries for a date range and optional category.
    /// </summary>
    [HttpGet("{tenantId}/audit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTenantAuditAsync(
        string tenantId,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] string? category = null,
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default) {
        if (!IsValidIdentifier(tenantId)) {
            return BadRequest();
        }

        AuditEventCategory? auditCategory = null;
        if (!string.IsNullOrWhiteSpace(category)) {
            if (!Enum.TryParse(category, ignoreCase: true, out AuditEventCategory parsed)) {
                return BadRequest();
            }

            auditCategory = parsed;
        }

        string? userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        // Reject invalid windows before doing any cursor work so clients see the actual validation
        // error rather than a misleading generic "Invalid cursor" 400.
        if (from is not null && to is not null && from > to) {
            return BadRequest();
        }

        pageSize = TenantQueryPaginationPolicy.ClampAuditPageSize(pageSize);
        string correlationId = GetCorrelationId();
        string scope = TenantQueryCursorScopes.GetTenantAudit(tenantId, from, to, auditCategory);
        IActionResult? cursorValidation = ValidateSubmittedCursor(
            cursor,
            GetTenantAuditQuery.QueryType,
            scope,
            "get-tenant-audit",
            correlationId,
            tenantId,
            userId);
        if (cursorValidation is not null) {
            return cursorValidation;
        }

        byte[] payloadBytes = JsonSerializer.SerializeToUtf8Bytes(new { from, to, category = auditCategory?.ToString(), cursor, pageSize });

        var envelope = new QueryEnvelope(
            tenantId: SystemTenant,
            domain: GetTenantAuditQuery.Domain,
            aggregateId: tenantId,
            queryType: GetTenantAuditQuery.QueryType,
            payload: payloadBytes,
            correlationId: correlationId,
            userId: userId,
            entityId: tenantId);

        return await DispatchAsync(envelope, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets users in a specific tenant with their roles.
    /// </summary>
    [HttpGet("{tenantId}/users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantUsersAsync(
        string tenantId,
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) {
        if (!IsValidIdentifier(tenantId)) {
            return BadRequest();
        }

        string? userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        pageSize = TenantQueryPaginationPolicy.ClampStandardPageSize(pageSize);
        string correlationId = GetCorrelationId();
        IActionResult? cursorValidation = ValidateSubmittedCursor(
            cursor,
            GetTenantUsersQuery.QueryType,
            TenantQueryCursorScopes.GetTenantUsers(tenantId),
            "get-tenant-users",
            correlationId,
            tenantId,
            userId);
        if (cursorValidation is not null) {
            return cursorValidation;
        }

        byte[] payloadBytes = JsonSerializer.SerializeToUtf8Bytes(new { cursor, pageSize });

        var envelope = new QueryEnvelope(
            tenantId: SystemTenant,
            domain: GetTenantUsersQuery.Domain,
            aggregateId: tenantId,
            queryType: GetTenantUsersQuery.QueryType,
            payload: payloadBytes,
            correlationId: correlationId,
            userId: userId,
            entityId: tenantId);

        return await DispatchAsync(envelope, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets tenants that a specific user belongs to with their role in each.
    /// </summary>
    [HttpGet("~/api/users/{userId}/tenants")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserTenantsAsync(
        string userId,
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) {
        if (!IsValidIdentifier(userId)) {
            return BadRequest();
        }

        string? authenticatedUserId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(authenticatedUserId)) {
            return Unauthorized();
        }

        pageSize = TenantQueryPaginationPolicy.ClampStandardPageSize(pageSize);
        string correlationId = GetCorrelationId();
        IActionResult? cursorValidation = ValidateSubmittedCursor(
            cursor,
            GetUserTenantsQuery.QueryType,
            TenantQueryCursorScopes.GetUserTenants(authenticatedUserId, userId),
            "get-user-tenants",
            correlationId,
            tenantId: string.Empty,
            authenticatedUserId);
        if (cursorValidation is not null) {
            return cursorValidation;
        }

        byte[] payloadBytes = JsonSerializer.SerializeToUtf8Bytes(new { cursor, pageSize });

        var envelope = new QueryEnvelope(
            tenantId: SystemTenant,
            domain: GetUserTenantsQuery.Domain,
            aggregateId: "index",
            queryType: GetUserTenantsQuery.QueryType,
            payload: payloadBytes,
            correlationId: correlationId,
            userId: authenticatedUserId,
            entityId: userId);

        return await DispatchAsync(envelope, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets global administrators from the fixed platform authority scope.
    /// </summary>
    [HttpGet("~/api/global-administrators")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetGlobalAdministratorsAsync(
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) {
        string? userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        pageSize = TenantQueryPaginationPolicy.ClampStandardPageSize(pageSize);
        string correlationId = GetCorrelationId();
        IActionResult? cursorValidation = ValidateSubmittedCursor(
            cursor,
            GetGlobalAdministratorsQuery.QueryType,
            TenantQueryCursorScopes.GetGlobalAdministrators(userId),
            "get-global-administrators",
            correlationId,
            TenantIdentity.GlobalAdministratorsAggregateId,
            userId);
        if (cursorValidation is not null) {
            return cursorValidation;
        }

        byte[] payloadBytes = JsonSerializer.SerializeToUtf8Bytes(new { cursor, pageSize });

        var envelope = new QueryEnvelope(
            tenantId: SystemTenant,
            domain: GetGlobalAdministratorsQuery.Domain,
            aggregateId: TenantIdentity.GlobalAdministratorsAggregateId,
            queryType: GetGlobalAdministratorsQuery.QueryType,
            payload: payloadBytes,
            correlationId: correlationId,
            userId: userId,
            entityId: TenantIdentity.GlobalAdministratorsAggregateId);

        return await DispatchAsync(envelope, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists tenants visible to the authenticated user with cursor-based pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListTenantsAsync(
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) {
        string? userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        pageSize = TenantQueryPaginationPolicy.ClampStandardPageSize(pageSize);
        string correlationId = GetCorrelationId();
        IActionResult? cursorValidation = ValidateSubmittedCursor(
            cursor,
            ListTenantsQuery.QueryType,
            TenantQueryCursorScopes.ListTenants(userId),
            "list-tenants",
            correlationId,
            tenantId: string.Empty,
            userId);
        if (cursorValidation is not null) {
            return cursorValidation;
        }

        byte[] payloadBytes = JsonSerializer.SerializeToUtf8Bytes(new { cursor, pageSize });

        var envelope = new QueryEnvelope(
            tenantId: SystemTenant,
            domain: ListTenantsQuery.Domain,
            aggregateId: "index",
            queryType: ListTenantsQuery.QueryType,
            payload: payloadBytes,
            correlationId: correlationId,
            userId: userId,
            entityId: userId);

        return await DispatchAsync(envelope, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsValidIdentifier(string? value)
            => !string.IsNullOrWhiteSpace(value) && _identifierRegex.IsMatch(value);

    // Replicates the platform SubmitQueryHandler error-to-status mapping for in-process dispatch.
    private static bool IsNotFound(string? errorMessage)
        => !string.IsNullOrWhiteSpace(errorMessage)
            && (errorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase)
                || errorMessage.Contains("no projection state available", StringComparison.OrdinalIgnoreCase));

    private static bool IsNotImplemented(string? errorMessage)
        => string.Equals(errorMessage, QueryAdapterFailureReason.UnsupportedQueryType, StringComparison.Ordinal)
            || string.Equals(errorMessage, QueryAdapterFailureReason.UnknownQueryType, StringComparison.Ordinal)
            || (!string.IsNullOrWhiteSpace(errorMessage)
                && (errorMessage.Contains("not implemented", StringComparison.OrdinalIgnoreCase)
                    || errorMessage.Contains("not yet implemented", StringComparison.OrdinalIgnoreCase)));

    private async Task<IActionResult> DispatchAsync(QueryEnvelope envelope, CancellationToken cancellationToken) {
        IActionResult? authorizationFailure = await ValidateAuthorizationAsync(envelope, cancellationToken).ConfigureAwait(false);
        if (authorizationFailure is not null) {
            return authorizationFailure;
        }

        QueryResult result = await DomainQueryDispatcher
            .ExecuteAsync(HttpContext.RequestServices, envelope, cancellationToken)
            .ConfigureAwait(false);

        if (result.Success) {
            QueryResponseMetadata? metadata = result is TenantQueryResult tenantResult ? tenantResult.Metadata : null;
            if (metadata is not null) {
                ApplyFreshnessHeaders(metadata);
                if (IsNotModified(metadata.ETag)) {
                    return StatusCode(StatusCodes.Status304NotModified);
                }
            }

            return Ok(result.GetPayload());
        }

        string correlationId = envelope.CorrelationId;
        string? error = result.ErrorMessage;

        if (string.Equals(error, QueryAdapterFailureReason.Forbidden, StringComparison.OrdinalIgnoreCase)
            || string.Equals(error, "Forbidden", StringComparison.OrdinalIgnoreCase)) {
            return QueryProblem(
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "You do not have permission to access this resource.",
                correlationId,
                AuthorizationFailureReasonExtensions.InsufficientPermission);
        }

        if (IsNotFound(error)) {
            return QueryProblem(
                StatusCodes.Status404NotFound,
                "Not Found",
                "The requested resource was not found.",
                correlationId,
                reasonCode: null);
        }

        if (IsNotImplemented(error)) {
            return QueryProblem(
                StatusCodes.Status501NotImplemented,
                "Not Implemented",
                error!,
                correlationId,
                QueryProblemReasonCodes.NotImplemented);
        }

        return QueryProblem(
            StatusCodes.Status500InternalServerError,
            "Query Failed",
            error ?? "Projection query execution failed.",
            correlationId,
            QueryProblemReasonCodes.InternalError);
    }

    private async Task<IActionResult?> ValidateAuthorizationAsync(QueryEnvelope envelope, CancellationToken cancellationToken) {
        TenantValidationResult tenantResult = await tenantValidator
            .ValidateAsync(User, envelope.TenantId, cancellationToken, envelope.AggregateId)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("ITenantValidator.ValidateAsync returned null.");
        if (!tenantResult.IsAuthorized) {
            return QueryProblem(
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "You do not have permission to access this resource.",
                envelope.CorrelationId,
                tenantResult.ReasonCode.ToReasonCode());
        }

        RbacValidationResult rbacResult = await rbacValidator
            .ValidateAsync(User, envelope.TenantId, envelope.Domain, envelope.QueryType, "query", cancellationToken, envelope.AggregateId)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("IRbacValidator.ValidateAsync returned null.");
        if (!rbacResult.IsAuthorized) {
            return QueryProblem(
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "You do not have permission to access this resource.",
                envelope.CorrelationId,
                rbacResult.ReasonCode.ToReasonCode());
        }

        return null;
    }

    private string GetCorrelationId()
        => Activity.Current?.Id ?? HttpContext.TraceIdentifier;

    private void ApplyFreshnessHeaders(QueryResponseMetadata metadata) {
        if (!string.IsNullOrWhiteSpace(metadata.ETag)) {
            Response.Headers.ETag = QuoteStrongETag(metadata.ETag);
        }

        string? projectionVersion = string.IsNullOrWhiteSpace(metadata.ProjectionVersion)
            ? metadata.ETag
            : metadata.ProjectionVersion;
        if (!string.IsNullOrWhiteSpace(projectionVersion)) {
            Response.Headers[ProjectionVersionHeaderName] = projectionVersion;
        }

        if (metadata.ServedAt is not null) {
            Response.Headers[ServedAtHeaderName] = metadata.ServedAt.Value.ToString("O", CultureInfo.InvariantCulture);
        }
    }

    private bool IsNotModified(string? currentETag) {
        if (string.IsNullOrWhiteSpace(currentETag)) {
            return false;
        }

        string? ifNoneMatch = Request.Headers.IfNoneMatch.ToString();
        if (string.IsNullOrWhiteSpace(ifNoneMatch)) {
            return false;
        }

        foreach (string candidate in ifNoneMatch.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)) {
            if (candidate == "*" || candidate.StartsWith("W/", StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            if (EntityTagHeaderValue.TryParse(candidate, out EntityTagHeaderValue? parsed)
                && parsed is not null
                && !parsed.IsWeak
                && string.Equals(parsed.Tag.Trim('"'), currentETag, StringComparison.Ordinal)) {
                return true;
            }

            if (string.Equals(candidate.Trim('"'), currentETag, StringComparison.Ordinal)) {
                return true;
            }
        }

        return false;
    }

    private static string QuoteStrongETag(string eTag)
        => "\"" + eTag.Trim().Trim('"').Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";

    private IActionResult QueryProblem(int statusCode, string title, string detail, string correlationId, string? reasonCode) {
        var problemDetails = new ProblemDetails {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = HttpContext.Request.Path,
            Extensions =
            {
                [GatewayProblemDetailsExtensions.CorrelationId] = correlationId,
            },
        };

        if (reasonCode is not null) {
            problemDetails.Extensions[GatewayProblemDetailsExtensions.ReasonCode] = reasonCode;
        }

        return new ObjectResult(problemDetails) {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" },
        };
    }

    private IActionResult? ValidateSubmittedCursor(
        string? cursor,
        string queryType,
        string scope,
        string endpoint,
        string correlationId,
        string tenantId,
        string userId) {
        if (cursorCodec.TryDecode(cursor, queryType, scope, out _, out string? failureReason)) {
            return null;
        }

        Log.InvalidCursorRejected(
            logger,
            correlationId,
            queryType,
            endpoint,
            tenantId,
            userId,
            failureReason ?? "unknown");

        return QueryProblem(
            StatusCodes.Status400BadRequest,
            "Bad Request",
            "Invalid cursor.",
            correlationId,
            "invalid-cursor");
    }

    private static partial class Log {
        [LoggerMessage(
            EventId = 1901,
            Level = LogLevel.Warning,
            Message = "Invalid tenant query cursor rejected: CorrelationId={CorrelationId}, QueryType={QueryType}, Endpoint={Endpoint}, TenantId={TenantId}, UserId={UserId}, FailureReason={FailureReason}, Stage=TenantQueryCursorValidation")]
        public static partial void InvalidCursorRejected(
            ILogger logger,
            string correlationId,
            string queryType,
            string endpoint,
            string tenantId,
            string userId,
            string failureReason);
    }
}
