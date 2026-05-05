using Dapr.Client;

using Hexalith.EventStore.Contracts.Projections;

using Microsoft.AspNetCore.Http;

namespace Hexalith.Tenants.Projections;

/// <summary>
/// Routes <see cref="ProjectionRequest"/> to the projection handler responsible for the
/// requested <see cref="ProjectionRequest.Domain"/>. Unknown domains fail closed with
/// <see cref="StatusCodes.Status400BadRequest"/> instead of silently being projected as tenants.
/// </summary>
public sealed class ProjectionDispatcher(DaprClient daprClient) {
    public const string TenantsDomain = "tenants";
    public const string GlobalAdministratorsDomain = "global-administrators";

    public async Task<IResult> DispatchAsync(ProjectionRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        switch (request.Domain) {
            case TenantsDomain:
                ProjectionResponse tenantsResponse = await new TenantProjectionHandler(daprClient)
                    .ProjectAsync(request).ConfigureAwait(false);
                return Results.Ok(tenantsResponse);

            case GlobalAdministratorsDomain:
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
