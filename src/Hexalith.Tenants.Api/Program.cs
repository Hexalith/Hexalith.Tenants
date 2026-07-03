using System.Text;

using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.ServiceDefaults;
using Hexalith.Tenants.Api.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// This host is the external-facing Tenants REST surface. Controllers are generated from the
// [RestRoute]-annotated Hexalith.Tenants.Contracts contracts and delegate to EventStore through the
// gateway client; the interactive UI uses EventStore client libraries directly.
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            ValidIssuer = builder.Configuration["EventStore:Authentication:Issuer"] ?? "hexalith-dev",
            ValidAudience = builder.Configuration["EventStore:Authentication:Audience"] ?? "hexalith-eventstore",
        };

        string? authority = builder.Configuration["EventStore:Authentication:Authority"];
        if (!string.IsNullOrWhiteSpace(authority))
        {
            options.Authority = authority;
            options.RequireHttpsMetadata = builder.Configuration.GetValue("EventStore:Authentication:RequireHttpsMetadata", true);
        }
        else
        {
            string signingKey = builder.Configuration["EventStore:Authentication:SigningKey"]
                ?? throw new InvalidOperationException("EventStore:Authentication:SigningKey is required when Authority is not configured.");
            options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        }
    });
builder.Services.AddAuthorization();

string daprHttpEndpoint = builder.Configuration["DAPR_HTTP_ENDPOINT"]
    ?? $"http://localhost:{builder.Configuration["DAPR_HTTP_PORT"] ?? "3500"}";
string? daprApiToken = builder.Configuration["DAPR_API_TOKEN"];

builder.Services.AddTransient<InboundBearerForwardingHandler>();
builder.Services.AddEventStoreGatewayClient(options => options.BaseAddress = new Uri(daprHttpEndpoint))
    .AddHttpMessageHandler<InboundBearerForwardingHandler>()
    .AddHttpMessageHandler(() => new DaprAppIdHandler("eventstore", daprApiToken));

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    _ = app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

public partial class Program;
