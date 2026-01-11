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
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add Authentication and Authorization
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Bearer";
    })
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

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
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Weather Endpoints
var weatherApi = app.MapGroup("/api/weather");
weatherApi.MapGet("/", async (ServerWeatherService service) => await service.GetForecastAsync())
    .RequireApiPermission("weather.read");

// User Management Endpoints
var usersApi = app.MapGroup("/api/users");

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
.RequireAuthorization("Api.Access");

usersApi.MapGet("/", async (ServerUserService service) => await service.GetUsersAsync())
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
.RequireApiPermission("users.delete");

// Reports Endpoints
var reportsApi = app.MapGroup("/api/reports");

reportsApi.MapGet("/", async (ServerReportService service) => await service.GetReportsAsync())
    .RequireApiPermission("reports.view");

reportsApi.MapGet("/export", async (ServerReportService service) =>
{
    var fileBytes = await service.ExportReportsAsync();
    return Results.File(fileBytes, "text/plain", "report.txt");
})
.RequireApiPermission("reports.export");


// Identity Provisioning Endpoint (Called by BFF on login)
app.MapPost("/api/identity/provision", async (HttpContext context, ApplicationDbContext db, UserManager<ApplicationUser> userManager) =>
{
    // Minimal provisioning logic - just ensuring the user exists.
    // In a real app, use a service.
    
    // The Token must be valid (checked by Middleware)
    var principal = context.User;
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
.RequireAuthorization("Api.Access"); // Requires access_as_user scope

await DbSeeder.SeedDataAsync(app.Services);

app.Run();
