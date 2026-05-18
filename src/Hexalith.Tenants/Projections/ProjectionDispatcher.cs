using Dapr.Client;

using Hexalith.EventStore.Contracts.Projections;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.Tenants.Projections;

/// <summary>
/// Routes <see cref="ProjectionRequest"/> to the projection handler responsible for the
/// requested <see cref="ProjectionRequest.Domain"/>. Unknown domains fail closed with
/// <see cref="StatusCodes.Status400BadRequest"/> instead of silently being projected as tenants.
/// </summary>
public sealed class ProjectionDispatcher(DaprClient daprClient, ILoggerFactory? loggerFactory = null) {
    public const string TenantsDomain = "tenants";
    public const string GlobalAdministratorsDomain = "global-administrators";

    private readonly ILoggerFactory _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

    public async Task<IResult> DispatchAsync(ProjectionRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        switch (request.Domain) {
            case TenantsDomain:
                ProjectionResponse tenantsResponse = await new TenantProjectionHandler(
                    daprClient,
                    _loggerFactory.CreateLogger<TenantProjectionHandler>())
                    .ProjectAsync(request).ConfigureAwait(false);
                return Results.Ok(tenantsResponse);

            case GlobalAdministratorsDomain:
                if (!GlobalAdministratorProjectionHandler.IsValidGlobalAdministratorIdentity(request)) {
                    return Results.Problem(
                        detail: "Global-administrator projections must use tenant 'system' and aggregate 'global-administrators'.",
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid global administrator projection identity");
                }

                ProjectionResponse globalAdminResponse = await new GlobalAdministratorProjectionHandler(daprClient)
                    .ProjectAsync(request).ConfigureAwait(false);
                return Results.Ok(globalAdminResponse);

            default:
                return Results.Problem(
                    detail: $"Unsupported projection domain '{request.Domain}'.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Unsupported projection domain");
        }
    }
}
