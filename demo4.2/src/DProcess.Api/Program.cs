using DProcess.Api.Authorization;
using DProcess.Shared.Dto;
using DProcess.Shared.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var authBuilder = builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "BearerSelector";
    })
    .AddPolicyScheme("BearerSelector", "Entra or Local Bearer", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
            {
                var token = authHeader["Bearer ".Length..].Trim();
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    try
                    {
                        var jwtToken = handler.ReadJwtToken(token);
                        var localIssuer = builder.Configuration["Idp:Issuer"]
                            ?? builder.Configuration["Idp:Authority"];
                        if (!string.IsNullOrWhiteSpace(localIssuer))
                        {
                            var expectedIssuer = localIssuer.TrimEnd('/');
                            var tokenIssuer = jwtToken.Issuer?.TrimEnd('/');
                            if (!string.IsNullOrWhiteSpace(tokenIssuer) &&
                                string.Equals(tokenIssuer, expectedIssuer, StringComparison.OrdinalIgnoreCase))
                            {
                                return "LocalBearer";
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Fall back to default scheme.
                    }
                }
            }

            return "Bearer";
        };
    });

authBuilder.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"), "Bearer");

authBuilder.AddJwtBearer("LocalBearer", options =>
    {
        options.Authority = builder.Configuration["Idp:Authority"];
        options.Audience = builder.Configuration["Idp:Audience"] ?? "api";
        options.RequireHttpsMetadata = true;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Api.Access", policy => policy.AddRequirements(new ScopeRequirement("access_as_user")))
    .AddPolicy(PermissionNames.WeatherRead, policy => policy.AddRequirements(new PermissionRequirement(PermissionNames.WeatherRead)))
    .AddPolicy(PermissionNames.WeatherWrite, policy => policy.AddRequirements(new PermissionRequirement(PermissionNames.WeatherWrite)))
    .AddPolicy(PermissionNames.UsersRead, policy => policy.AddRequirements(new PermissionRequirement(PermissionNames.UsersRead)))
    .AddPolicy(PermissionNames.UsersWrite, policy => policy.AddRequirements(new PermissionRequirement(PermissionNames.UsersWrite)))
    .AddPolicy(PermissionNames.UsersDelete, policy => policy.AddRequirements(new PermissionRequirement(PermissionNames.UsersDelete)))
    .AddPolicy(PermissionNames.ReportsView, policy => policy.AddRequirements(new PermissionRequirement(PermissionNames.ReportsView)))
    .AddPolicy(PermissionNames.ReportsExport, policy => policy.AddRequirements(new PermissionRequirement(PermissionNames.ReportsExport)));

builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, ScopeAuthorizationHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

var weatherApi = app.MapGroup("/api/weather");

weatherApi.MapGet("/", () =>
    Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]))
    .ToArray())
    .RequirePermission(PermissionNames.WeatherRead);

weatherApi.MapPost("/", () => Results.Created("/api/weather", new { status = "created" }))
    .RequirePermission(PermissionNames.WeatherWrite);

var usersApi = app.MapGroup("/api/users");

usersApi.MapGet("/", () =>
    new[]
    {
        new UserPermissionsDto(
            "admin",
            "admin@local.app",
            PermissionNames.AllPermissions)
    })
    .RequirePermission(PermissionNames.UsersRead);

usersApi.MapPost("/", (UserPermissionsDto input) =>
        Results.Created($"/api/users/{input.UserId}", input))
    .RequirePermission(PermissionNames.UsersWrite);

usersApi.MapDelete("/{id}", (string id) => Results.NoContent())
    .RequirePermission(PermissionNames.UsersDelete);

var reportsApi = app.MapGroup("/api/reports");

reportsApi.MapGet("/", () => new[] { "Quarterly Report", "KPI Summary" })
    .RequirePermission(PermissionNames.ReportsView);

reportsApi.MapGet("/export", () =>
    Results.File(Encoding.UTF8.GetBytes("Report export"), "text/plain", "report.txt"))
    .RequirePermission(PermissionNames.ReportsExport);

app.MapGet("/api/entra/profile", (ClaimsPrincipal user) => new
    {
        Name = user.Identity?.Name,
        TenantId = user.FindFirst("tid")?.Value,
        ObjectId = user.FindFirst("oid")?.Value
    })
    .RequireAuthorization("Api.Access");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
