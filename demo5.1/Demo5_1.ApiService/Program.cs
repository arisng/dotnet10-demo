using Demo5_1.ServiceDefaults;
using Demo5_1.ApiService.Data;
using Demo5_1.ApiService.Authorization;
using Demo5_1.ApiService.Services;
using Demo5_1.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Scalar.AspNetCore;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add Authentication and Authorization
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
                var token = authHeader.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    try 
                    {
                        var jwtToken = handler.ReadJwtToken(token);
                        var localIssuer = builder.Configuration["Jwt:Issuer"];
                        if (jwtToken.Issuer.Equals(localIssuer, StringComparison.OrdinalIgnoreCase))
                        {
                            return "LocalBearer";
                        }
                    }
                    catch (Exception)
                    {
                        // Fallback to default
                    }
                }
            }
            return "Bearer"; // Default to Entra Identity
        };
    });

authBuilder.AddMicrosoftIdentityWebApi(builder.Configuration, "AzureAd", "Bearer");

authBuilder.AddJwtBearer("LocalBearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Api.Access", policy => policy.AddRequirements(new ScopeRequirement("access_as_user")))
    .AddPolicy("weather.read", policy => policy.AddRequirements(new PermissionRequirement("weather.read")))
    .AddPolicy("users.read", policy => policy.AddRequirements(new PermissionRequirement("users.read")))
    .AddPolicy("users.write", policy => policy.AddRequirements(new PermissionRequirement("users.write")))
    .AddPolicy("users.delete", policy => policy.AddRequirements(new PermissionRequirement("users.delete")))
    .AddPolicy("reports.view", policy => policy.AddRequirements(new PermissionRequirement("reports.view")))
    .AddPolicy("reports.export", policy => policy.AddRequirements(new PermissionRequirement("reports.export")));

builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, ScopeAuthorizationHandler>();
builder.Services.AddScoped<IClaimsTransformation, PermissionClaimsTransformation>();

// Add Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "DataSource=Data/app.db;Cache=Shared";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Add Application Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<ServerUserService>();
builder.Services.AddScoped<ServerReportService>();
builder.Services.AddScoped<ServerWeatherService>(); 

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "Demo5.1 ApiService",
            Version = "v1",
            Description = "Distributed Modular Monolith API with OAuth scopes and local RBAC"
        };
        
        // Define Security Schemes
        document.Components ??= new();
        document.Components.SecuritySchemes.Add("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below. Example: 'Bearer 12345abcdef'"
        });

        // Add Security Requirement globally (optional, or per endpoint)
        // Here we'll leave it to per-endpoint or just defined in components for now
        
        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => 
    {
        options.WithTitle("Demo5.1 API Reference")
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
               .WithPreferredScheme("Bearer");
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Weather Endpoints
var weatherApi = app.MapGroup("/api/weather")
    .WithTags("Weather");

weatherApi.MapGet("/", GetWeatherForecast)
    .RequireApiPermission("weather.read");

// User Management Endpoints
var usersApi = app.MapGroup("/api/users")
    .WithTags("User Management");

usersApi.MapGet("/permissions", async (HttpContext context, IPermissionService service) => 
{
    var oid = context.User.GetObjectId();
    if (string.IsNullOrEmpty(oid)) return Results.Unauthorized();
    // In Hybrid scenario, we map OID to local UserID via Login table? 
    // Or we assume the user is looked up by claim?
    // PermissionClaimsTransformation does the work during Auth.
    // So we can just return the User.Claims where type == "permission"?
    // YES! PermissionClaimsTransformation adds "permission" claims.
    var permissions = context.User.FindAll("permission").Select(c => c.Value).ToList();
    return Results.Ok(permissions);
})
.WithSummary("Get user permissions")
.WithDescription("Retrieves the current user's permissions from claims. Requires API access.")
.RequireAuthorization("Api.Access");

usersApi.MapGet("/", async (ServerUserService service) => await service.GetUsersAsync())
    .WithSummary("Get all users")
    .WithDescription("Retrieves a list of all users in the system. Requires users.read permission.")
    .RequireApiPermission("users.read");

usersApi.MapPost("/", async (CreateUserDto input, ServerUserService service) =>
{
    try
    {
        var user = await service.CreateUserAsync(input);
        return Results.Created($"/api/users/{user.Id}", user);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }
})
.WithSummary("Create new user")
.WithDescription("Creates a new user in the system. Requires users.write permission.")
.RequireApiPermission("users.write");

usersApi.MapDelete("/{id}", async (string id, ServerUserService service) =>
{
    try
    {
        await service.DeleteUserAsync(id);
        return Results.NoContent();
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }
})
.WithSummary("Delete user")
.WithDescription("Deletes a user from the system by ID. Requires users.delete permission.")
.RequireApiPermission("users.delete");

// Reports Endpoints
var reportsApi = app.MapGroup("/api/reports")
    .WithTags("Reports");

reportsApi.MapGet("/", async (ServerReportService service) => await service.GetReportsAsync())
    .WithSummary("Get reports")
    .WithDescription("Retrieves available reports data. Requires reports.view permission.")
    .RequireApiPermission("reports.view");

reportsApi.MapGet("/export", async (ServerReportService service) =>
{
    var fileBytes = await service.ExportReportsAsync();
    return Results.File(fileBytes, "text/plain", "report.txt");
})
.WithSummary("Export reports")
.WithDescription("Exports reports data as a text file. Requires reports.export permission.")
.RequireApiPermission("reports.export");


// Identity Provisioning Endpoint (Called by BFF on login)
app.MapPost("/api/identity/token", async (LoginRequest request, UserManager<ApplicationUser> userManager, IConfiguration config) =>
{
    var user = await userManager.FindByEmailAsync(request.Email);
    if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
    {
        return Results.Unauthorized();
    }

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id),
        new Claim(JwtRegisteredClaimNames.Email, user.Email!),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim("scp", "access_as_user"),
        new Claim("idp", "local")
    };

    var token = new JwtSecurityToken(
        issuer: config["Jwt:Issuer"],
        audience: config["Jwt:Audience"],
        claims: claims,
        expires: DateTime.Now.AddHours(1),
        signingCredentials: creds
    );

    return Results.Ok(new TokenResponse
    {
        AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
        ExpiresIn = 3600
    });
})
.WithTags("Identity")
.WithSummary("Authenticate local user")
.WithDescription("Authenticates a local user and returns a JWT token for API access.");

app.MapPost("/api/identity/provision", async (HttpContext context, ApplicationDbContext db, UserManager<ApplicationUser> userManager) =>
{
    // Minimal provisioning logic - just ensuring the user exists.
    // In a real app, use a service.
    
    // The Token must be valid (checked by Middleware)
    var principal = context.User;

    // Check if it's a local user (already has idp=local claim or issuer is our local issuer)
    if (principal.HasClaim("idp", "local"))
    {
        // Local user is already in our DB (they wouldn't have a token otherwise)
        return Results.Ok();
    }

    var oid = principal.GetObjectId(); // From Microsoft.Identity.Web
    if (string.IsNullOrEmpty(oid)) return Results.BadRequest("Missing OID claim");

    // Check if user exists by OID (login)
    var user = await userManager.FindByLoginAsync("MicrosoftEntra", oid);
    if (user == null)
    {
        // Provision
        var email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("preferred_username");
        if (string.IsNullOrEmpty(email)) return Results.BadRequest("Missing Email claim");

        user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded) return Results.BadRequest(result.Errors);

        await userManager.AddLoginAsync(user, new UserLoginInfo("MicrosoftEntra", oid, "Microsoft Entra ID"));
        
        // Add default role
        await userManager.AddToRoleAsync(user, "User");
    }
    
    return Results.Ok();
})
.WithTags("Identity")
.WithSummary("Provision user")
.WithDescription("Provisions a user in the system after authentication. Requires API access scope.")
.RequireAuthorization("Api.Access"); // Requires access_as_user scope

await DbSeeder.SeedDataAsync(app.Services);

app.Run();

// Re-using partial Program for static methods allows XML doc comments to be picked up correctly
public partial class Program
{
    /// <summary>
    /// Retrieves the weather forecast.
    /// </summary>
    /// <remarks>
    /// This endpoint returns a 5-day weather forecast. 
    /// Requires 'weather.read' permission.
    /// </remarks>
    /// <param name="service">The weather service.</param>
    /// <returns>A list of weather forecasts.</returns>
    [ProducesResponseType<WeatherForecast[]>(StatusCodes.Status200OK, Description = "The weather forecast list")]
    public static async Task<IResult> GetWeatherForecast(ServerWeatherService service)
        => TypedResults.Ok(await service.GetForecastAsync());
}
