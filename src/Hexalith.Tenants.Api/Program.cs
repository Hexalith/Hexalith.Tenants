using System.Text;

using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.ServiceDefaults;
using Hexalith.EventStore.ServiceDefaults.Authentication;
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
using Hexalith.EventStore.ServiceDefaults.Routing;
#endif
using Hexalith.Tenants.Api.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// This host is the external-facing Tenants REST surface. Controllers are generated from the
// [RestRoute]-annotated Hexalith.Tenants.Contracts contracts and delegate to EventStore through the
// gateway client; the interactive UI uses EventStore client libraries directly.
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
#if HEXALITH_DEVELOPMENT_CATALOG_COHORT
builder.Services.AddEventStoreDevelopmentGeneratedRestCatalog(builder.Configuration, "tenants-api",
    typeof(Hexalith.Tenants.Contracts.Routing.TenantCatalogDeclarations).Assembly);
#elif HEXALITH_DEVELOPMENT_CATALOG_SOURCE
builder.Services.AddEventStoreRouteCatalog(builder.Configuration);
string? catalogAppId = builder.Configuration["EventStore:DomainService:AppId"];
if (!string.IsNullOrEmpty(builder.Configuration["EventStore:RouteCatalog:DevelopmentBootstrap:Directory"])
    && string.IsNullOrWhiteSpace(catalogAppId))
{
    throw new InvalidOperationException("Private catalog bootstrap requires EventStore:DomainService:AppId for the actual host identity.");
}
builder.Services.AddEventStoreDevelopmentRouteCatalogBootstrap(builder.Configuration, async (_, token) =>
{
    var actual = await DevelopmentRouteCatalogInputs.CaptureAsync(builder.Configuration, catalogAppId!,
        CompiledRouteCatalogDeclarations.Read([typeof(Hexalith.Tenants.Contracts.Routing.TenantCatalogDeclarations).Assembly]),
        [], [], [], [], token).ConfigureAwait(false);
    return actual with { Operations = CompiledRouteCatalogDeclarations.ReadOperations([typeof(Hexalith.Tenants.Contracts.Routing.TenantCatalogDeclarations).Assembly]) };
});
#endif

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        string? issuer = builder.Configuration["EventStore:Authentication:Issuer"];
        string? audience = builder.Configuration["EventStore:Authentication:Audience"];

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            ValidAudience = string.IsNullOrWhiteSpace(audience) ? "hexalith-eventstore" : audience,
        };

        string? authority = FirstConfigured(
            builder.Configuration["EventStore:Authentication:Authority"],
            builder.Configuration["Oidc:Authority"]);
        if (!string.IsNullOrWhiteSpace(authority))
        {
            options.Authority = authority;
            options.RequireHttpsMetadata = builder.Configuration.GetValue("EventStore:Authentication:RequireHttpsMetadata", true);
            if (!string.IsNullOrWhiteSpace(issuer))
            {
                options.TokenValidationParameters.ValidIssuer = issuer;
            }
        }
        else
        {
            options.TokenValidationParameters.ValidIssuer = string.IsNullOrWhiteSpace(issuer) ? "hexalith-dev" : issuer;

            string? signingKey = FirstConfigured(
                builder.Configuration["EventStore:Authentication:SigningKey"],
                builder.Configuration["Jwt:SigningKey"]);
            if (string.IsNullOrWhiteSpace(signingKey))
            {
                throw new InvalidOperationException("EventStore:Authentication:SigningKey or Jwt:SigningKey is required when neither EventStore:Authentication:Authority nor Oidc:Authority is configured.");
            }

            if (Encoding.UTF8.GetByteCount(signingKey) < 32)
            {
                throw new InvalidOperationException("The configured EventStore:Authentication:SigningKey or Jwt:SigningKey must be at least 32 bytes (256 bits) for HS256 token validation.");
            }

            options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        }
    });
builder.Services.AddAuthorization();
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
builder.Services.AddEventStoreHttpHostSecurity();
#else
// The published runtime predates the shared human policy. Keep public generated
// routes bound to human bearer credentials using its published credential kinds.
builder.Services.AddAuthorizationBuilder().SetDefaultPolicy(
    new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .RequireAssertion(context => !context.User.HasClaim(claim =>
            claim.Type is EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType
                or EventStoreWorkloadAuthenticationDefaults.ChannelClaimType
                or EventStoreWorkloadAuthenticationDefaults.OperationClaimType)
            && (context.Resource is not HttpContext http
                || !http.Request.Headers.ContainsKey(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName)
                    && http.Request.Headers.Authorization.Count <= 1))
        .Build());
#endif

string daprHttpEndpoint = DaprHttpEndpointResolver.Resolve(builder.Configuration);
string? daprApiToken = builder.Configuration["DAPR_API_TOKEN"];

builder.Services.AddTransient<InboundBearerForwardingHandler>();
builder.Services.AddEventStoreGatewayClient(options => options.BaseAddress = new Uri(daprHttpEndpoint))
    .AddHttpMessageHandler<InboundBearerForwardingHandler>()
    .AddEventStoreDaprServiceInvocation("eventstore", daprApiToken);
string? gatewayReadinessEndpoint = builder.Configuration["EventStore:GatewayReadiness:BaseAddress"];
IHttpClientBuilder gatewayReadinessClient = builder.Services.AddHttpClient(
    EventStoreGatewayReadinessHealthCheck.ClientName,
    client => client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(gatewayReadinessEndpoint) ? daprHttpEndpoint : gatewayReadinessEndpoint));
if (string.IsNullOrWhiteSpace(gatewayReadinessEndpoint))
{
    _ = gatewayReadinessClient.AddEventStoreDaprServiceInvocation("eventstore", daprApiToken);
}
builder.Services.AddHealthChecks().AddCheck<EventStoreGatewayReadinessHealthCheck>(
    "eventstore-gateway",
    failureStatus: HealthStatus.Unhealthy,
    tags: ["ready"],
    timeout: TimeSpan.FromSeconds(3));

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    _ = app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
#if HEXALITH_DEVELOPMENT_CATALOG_COHORT
app.UseEventStoreDevelopmentGeneratedRestCatalogReceiver();
#endif
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
app.UseEventStoreHttpHostSecurity();
#endif
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

static string? FirstConfigured(string? legacy, string? alias)
    => !string.IsNullOrWhiteSpace(legacy) ? legacy : alias;

public partial class Program;
