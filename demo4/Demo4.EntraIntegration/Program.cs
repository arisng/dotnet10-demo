using Demo4.EntraIntegration.Client.Services;
using Demo4.EntraIntegration.Shared.Models;
using Demo4.EntraIntegration.Components;
using Demo4.EntraIntegration.Components.Account;
using Demo4.EntraIntegration.Data;
using Demo4.EntraIntegration.Authorization;
using Demo4.EntraIntegration.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Identity.Web;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Configure Static Web Assets for non-Development environments
// This enables serving .client project assets in Staging/Production when running locally
if (!builder.Environment.IsDevelopment())
{
    StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);
}

// Add services to the container.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// Register the persisting provider to pass state to WASM
builder.Services.AddScoped<PersistingServerAuthenticationStateProvider>();

// Add HttpContextAccessor to allow services to access HttpContext in API endpoints
builder.Services.AddHttpContextAccessor();

// Register Server Services (for Prerendering and API endpoints)
builder.Services.AddScoped<IWeatherService, ServerWeatherService>();
builder.Services.AddScoped<IUserService, ServerUserService>();
builder.Services.AddScoped<IReportService, ServerReportService>();

// Authorization services
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IClaimsTransformation, PermissionClaimsTransformation>();

// Microsoft Graph service
builder.Services.AddScoped<IGraphService, GraphService>();

// Entra user provisioning service
builder.Services.AddScoped<IEntraUserProvisioningService, EntraUserProvisioningService>();

// Register policies for Blazor [Authorize] attributes
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("weather.read", policy => policy.AddRequirements(new PermissionRequirement("weather.read")))
    .AddPolicy("weather.write", policy => policy.AddRequirements(new PermissionRequirement("weather.write")))
    .AddPolicy("users.read", policy => policy.AddRequirements(new PermissionRequirement("users.read")))
    .AddPolicy("users.write", policy => policy.AddRequirements(new PermissionRequirement("users.write")))
    .AddPolicy("users.delete", policy => policy.AddRequirements(new PermissionRequirement("users.delete")))
    .AddPolicy("reports.view", policy => policy.AddRequirements(new PermissionRequirement("reports.view")))
    .AddPolicy("reports.export", policy => policy.AddRequirements(new PermissionRequirement("reports.export")));

// Register custom handlers
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApp(
        builder.Configuration.GetSection("AzureAd"),
        openIdConnectScheme: "MicrosoftEntra",
        cookieScheme: null,
        subscribeToOpenIdConnectMiddlewareDiagnosticsEvents: true)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("DownstreamApi", builder.Configuration.GetSection("DownstreamApi"))
    .AddInMemoryTokenCaches();

// Configure OIDC events for auto-provisioning
builder.Services.Configure<OpenIdConnectOptions>("MicrosoftEntra", options =>
{
    options.Events = new OpenIdConnectEvents
    {
        OnTokenValidated = async context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var principal = context.Principal;

            if (principal == null)
            {
                logger.LogWarning("OnTokenValidated: Principal is null");
                return;
            }

            // Check if this is an Entra ID user (has oid claim)
            var oid = principal.GetObjectId();
            if (string.IsNullOrEmpty(oid))
            {
                // Not an Entra user, skip provisioning
                return;
            }

            logger.LogInformation("OnTokenValidated: Entra user detected with OID: {Oid}", oid);

            try
            {
                // Auto-provision user in database
                var provisioningService = context.HttpContext.RequestServices
                    .GetRequiredService<IEntraUserProvisioningService>();

                var user = await provisioningService.ProvisionUserAsync(principal, context.HttpContext.RequestAborted);

                logger.LogInformation("OnTokenValidated: User provisioning completed for {Email} (ID: {UserId})",
                    user.Email, user.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OnTokenValidated: Failed to provision Entra user with OID: {Oid}", oid);
                
                // Fail the authentication to prevent incomplete user state
                context.Fail($"Failed to provision user: {ex.Message}");
            }
        },

        OnAuthenticationFailed = async context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Exception, "OIDC Authentication failed: {Error}", context.Exception?.Message);
        }
    };
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Demo4.EntraIntegration.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Weather API
var weatherApi = app.MapGroup("/api/weather");

weatherApi.MapGet("/", async (IWeatherService service) => await service.GetForecastAsync())
    .RequirePermission("weather.read");

weatherApi.MapPost("/", () => Results.Created())
    .RequirePermission("weather.write");

// User Management API
var usersApi = app.MapGroup("/api/users");

usersApi.MapGet("/", async (IUserService service) => await service.GetUsersAsync())
    .RequirePermission("users.read");

usersApi.MapPost("/", async (CreateUserDto input, IUserService service) =>
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
.RequirePermission("users.write");

usersApi.MapDelete("/{id}", async (string id, IUserService service) =>
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
.RequirePermission("users.delete");

// Reports API
var reportsApi = app.MapGroup("/api/reports");

reportsApi.MapGet("/", async (IReportService service) => await service.GetReportsAsync())
    .RequirePermission("reports.view");

reportsApi.MapGet("/export", async (IReportService service) =>
{
    var fileBytes = await service.ExportReportsAsync();
    return Results.File(fileBytes, "text/plain", "report.txt");
})
.RequirePermission("reports.export");

// Graph API endpoints
var graphApi = app.MapGroup("/api/graph");

graphApi.MapGet("/profile", async (IGraphService graphService) =>
{
    var profile = await graphService.GetUserProfileAsync();
    return profile != null ? Results.Ok(profile) : Results.NotFound();
})
.RequireAuthorization(); // Any authenticated user can access their own profile

graphApi.MapGet("/profile/photo", async (IGraphService graphService) =>
{
    var photoBytes = await graphService.GetUserPhotoAsync();
    if (photoBytes == null)
    {
        return Results.NotFound();
    }
    return Results.File(photoBytes, "image/jpeg");
})
.RequireAuthorization(); // Any authenticated user can access their own photo

// Admin Role Mapping Management API
var adminApi = app.MapGroup("/api/admin");

adminApi.MapGet("/roles", async (RoleManager<IdentityRole> roleManager) =>
{
    var roles = await roleManager.Roles
        .OrderBy(r => r.Name)
        .Select(r => r.Name!)
        .ToListAsync();
    return Results.Ok(roles);
})
.RequirePermission("admin.manage-roles");

adminApi.MapPost("/role-mappings", async (CreateRoleMappingDto input, ApplicationDbContext db) =>
{
    // Validate that the local role exists
    var roleExists = await db.Roles.AnyAsync(r => r.Name == input.LocalRoleName);
    if (!roleExists)
    {
        return Results.BadRequest($"Local role '{input.LocalRoleName}' does not exist.");
    }

    // Check for duplicate Entra role
    var existing = await db.RoleMappingConfigurations
        .FirstOrDefaultAsync(rmc => rmc.EntraAppRoleValue == input.EntraAppRoleValue);
    if (existing != null)
    {
        return Results.Conflict($"A mapping already exists for Entra role '{input.EntraAppRoleValue}'.");
    }

    var mapping = new RoleMappingConfiguration
    {
        EntraAppRoleValue = input.EntraAppRoleValue,
        LocalRoleName = input.LocalRoleName,
        CreatedAt = DateTime.UtcNow,
        Notes = input.Notes
    };

    db.RoleMappingConfigurations.Add(mapping);
    await db.SaveChangesAsync();

    return Results.Created($"/api/admin/role-mappings/{mapping.Id}", mapping);
})
.RequirePermission("admin.manage-roles");

adminApi.MapPut("/role-mappings/{id:int}", async (int id, UpdateRoleMappingDto input, ApplicationDbContext db) =>
{
    var mapping = await db.RoleMappingConfigurations.FindAsync(id);
    if (mapping == null)
    {
        return Results.NotFound();
    }

    // Validate that the local role exists
    var roleExists = await db.Roles.AnyAsync(r => r.Name == input.LocalRoleName);
    if (!roleExists)
    {
        return Results.BadRequest($"Local role '{input.LocalRoleName}' does not exist.");
    }

    // Check for duplicate Entra role (excluding this mapping)
    var duplicate = await db.RoleMappingConfigurations
        .FirstOrDefaultAsync(rmc => rmc.Id != id && rmc.EntraAppRoleValue == input.EntraAppRoleValue);
    if (duplicate != null)
    {
        return Results.Conflict($"A mapping already exists for Entra role '{input.EntraAppRoleValue}'.");
    }

    mapping.EntraAppRoleValue = input.EntraAppRoleValue;
    mapping.LocalRoleName = input.LocalRoleName;
    mapping.Notes = input.Notes;

    await db.SaveChangesAsync();
    return Results.Ok(mapping);
})
.RequirePermission("admin.manage-roles");

adminApi.MapDelete("/role-mappings/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var mapping = await db.RoleMappingConfigurations.FindAsync(id);
    if (mapping == null)
    {
        return Results.NotFound();
    }

    db.RoleMappingConfigurations.Remove(mapping);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.RequirePermission("admin.manage-roles");

await DbSeeder.SeedDataAsync(app.Services);

app.Run();

