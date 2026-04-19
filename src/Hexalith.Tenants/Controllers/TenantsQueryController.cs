using System.Text.Json;

using Hexalith.EventStore.Server.Pipeline.Queries;
using Hexalith.Tenants.Contracts;
using Hexalith.Tenants.Contracts.Queries;

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
public sealed class TenantsQueryController(IMediator mediator) : ControllerBase {
    /// <summary>
    /// Lists tenants visible to the authenticated user with cursor-based pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListTenants(
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) {
        string? userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        pageSize = ClampPageSize(pageSize);
        byte[] payloadBytes = JsonSerializer.SerializeToUtf8Bytes(new { cursor, pageSize });

        var query = new SubmitQuery(
            Tenant: "system",
            Domain: ListTenantsQuery.Domain,
            AggregateId: "index",
            QueryType: ListTenantsQuery.QueryType,
            Payload: payloadBytes,
            CorrelationId: Guid.NewGuid().ToString(),
            UserId: userId,
            EntityId: userId,
            ProjectionActorType: TenantProjectionRouting.ActorTypeName);

        SubmitQueryResult result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Ok(result.Payload);
    }

    /// <summary>
    /// Gets full details for a specific tenant.
    /// </summary>
    [HttpGet("{tenantId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenant(string tenantId, CancellationToken cancellationToken = default) {
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
            CorrelationId: Guid.NewGuid().ToString(),
            UserId: userId,
            EntityId: tenantId,
            ProjectionActorType: TenantProjectionRouting.ActorTypeName);

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
    public async Task<IActionResult> GetTenantUsers(
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

        pageSize = ClampPageSize(pageSize);
        byte[] payloadBytes = JsonSerializer.SerializeToUtf8Bytes(new { cursor, pageSize });

        var query = new SubmitQuery(
            Tenant: "system",
            Domain: GetTenantUsersQuery.Domain,
            AggregateId: tenantId,
            QueryType: GetTenantUsersQuery.QueryType,
            Payload: payloadBytes,
            CorrelationId: Guid.NewGuid().ToString(),
            UserId: userId,
            EntityId: tenantId,
            ProjectionActorType: TenantProjectionRouting.ActorTypeName);

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
    public async Task<IActionResult> GetUserTenants(
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

        pageSize = ClampPageSize(pageSize);
        byte[] payloadBytes = JsonSerializer.SerializeToUtf8Bytes(new { cursor, pageSize });

        var query = new SubmitQuery(
            Tenant: "system",
            Domain: GetUserTenantsQuery.Domain,
            AggregateId: "index",
            QueryType: GetUserTenantsQuery.QueryType,
            Payload: payloadBytes,
            CorrelationId: Guid.NewGuid().ToString(),
            UserId: authenticatedUserId,
            EntityId: userId,
            ProjectionActorType: TenantProjectionRouting.ActorTypeName);

        SubmitQueryResult result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Ok(result.Payload);
    }

    /// <summary>
    /// Tenant audit endpoint (MVP: returns 501 Not Implemented).
    /// </summary>
    [HttpGet("{tenantId}/audit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTenantAudit(
        string tenantId,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default) {
        if (!IsValidIdentifier(tenantId)) {
            return BadRequest();
        }

        string? userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId)) {
            return Unauthorized();
        }

        byte[] payloadBytes = JsonSerializer.SerializeToUtf8Bytes(new { from, to });

        var query = new SubmitQuery(
            Tenant: "system",
            Domain: GetTenantAuditQuery.Domain,
            AggregateId: tenantId,
            QueryType: GetTenantAuditQuery.QueryType,
            Payload: payloadBytes,
            CorrelationId: Guid.NewGuid().ToString(),
            UserId: userId,
            EntityId: tenantId,
            ProjectionActorType: TenantProjectionRouting.ActorTypeName);

        SubmitQueryResult result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Ok(result.Payload);
    }

    private static readonly System.Text.RegularExpressions.Regex s_identifierRegex = new(@"^[a-zA-Z0-9][a-zA-Z0-9._-]{0,255}$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static bool IsValidIdentifier(string? value)
        => !string.IsNullOrWhiteSpace(value) && s_identifierRegex.IsMatch(value);

    private static int ClampPageSize(int pageSize)
        => pageSize <= 0 ? 20 : pageSize > 100 ? 100 : pageSize;
}
