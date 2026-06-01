using System.Diagnostics;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Problems;
using Hexalith.EventStore.Server.Pipeline.Queries;
using Hexalith.Tenants.Contracts;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Queries;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hexalith.Tenants.Controllers;

/// <summary>
/// Thin REST controller that translates GET endpoints into SubmitQuery MediatR dispatches.
/// Query logic and authorization live in <see cref="Actors.TenantsProjectionActor"/>.
/// </summary>
[ApiController]
[Authorize]
[Route("api/tenants")]
[Tags("Tenants")]
public sealed partial class TenantsQueryController(
    IMediator mediator,
    ITenantQueryCursorCodec cursorCodec,
    ILogger<TenantsQueryController> logger) : ControllerBase {
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

        var query = new SubmitQuery(
            Tenant: "system",
            Domain: GetTenantQuery.Domain,
            AggregateId: tenantId,
            QueryType: GetTenantQuery.QueryType,
            Payload: [],
            CorrelationId: GetCorrelationId(),
            UserId: userId,
            EntityId: tenantId,
            ProjectionType: TenantProjectionRouting.ActorTypeName);

        SubmitQueryResult result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Ok(result.Payload);
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

        var query = new SubmitQuery(
            Tenant: "system",
            Domain: GetTenantAuditQuery.Domain,
            AggregateId: tenantId,
            QueryType: GetTenantAuditQuery.QueryType,
            Payload: payloadBytes,
            CorrelationId: correlationId,
            UserId: userId,
            EntityId: tenantId,
            ProjectionType: TenantProjectionRouting.ActorTypeName);

        SubmitQueryResult result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Ok(result.Payload);
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

        var query = new SubmitQuery(
            Tenant: "system",
            Domain: GetTenantUsersQuery.Domain,
            AggregateId: tenantId,
            QueryType: GetTenantUsersQuery.QueryType,
            Payload: payloadBytes,
            CorrelationId: correlationId,
            UserId: userId,
            EntityId: tenantId,
            ProjectionType: TenantProjectionRouting.ActorTypeName);

        SubmitQueryResult result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Ok(result.Payload);
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

        var query = new SubmitQuery(
            Tenant: "system",
            Domain: GetUserTenantsQuery.Domain,
            AggregateId: "index",
            QueryType: GetUserTenantsQuery.QueryType,
            Payload: payloadBytes,
            CorrelationId: correlationId,
            UserId: authenticatedUserId,
            EntityId: userId,
            ProjectionType: TenantProjectionRouting.ActorTypeName);

        SubmitQueryResult result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Ok(result.Payload);
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

        var query = new SubmitQuery(
            Tenant: "system",
            Domain: ListTenantsQuery.Domain,
            AggregateId: "index",
            QueryType: ListTenantsQuery.QueryType,
            Payload: payloadBytes,
            CorrelationId: correlationId,
            UserId: userId,
            EntityId: userId,
            ProjectionType: TenantProjectionRouting.ActorTypeName);

        SubmitQueryResult result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Ok(result.Payload);
    }

    private static bool IsValidIdentifier(string? value)
            => !string.IsNullOrWhiteSpace(value) && _identifierRegex.IsMatch(value);

    private string GetCorrelationId()
        => Activity.Current?.Id ?? HttpContext.TraceIdentifier;

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
        var problemDetails = new ProblemDetails {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bad Request",
            Detail = "Invalid cursor.",
            Instance = HttpContext.Request.Path,
            Extensions =
            {
                [GatewayProblemDetailsExtensions.CorrelationId] = correlationId,
                [GatewayProblemDetailsExtensions.ReasonCode] = "invalid-cursor",
            },
        };

        return new ObjectResult(problemDetails) {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { "application/problem+json" },
        };
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
