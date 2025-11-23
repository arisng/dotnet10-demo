using Demo3.BffRbac.Client.Services;
using Demo3.BffRbac.Shared.Models;
using Demo3.BffRbac.Components;
using Demo3.BffRbac.Components.Account;
using Demo3.BffRbac.Data;
using Demo3.BffRbac.Authorization;
using Demo3.BffRbac.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;

using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
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

// Register policies for Blazor [Authorize] attributes
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("weather.read", policy => policy.AddRequirements(new PermissionRequirement("weather.read")));
    options.AddPolicy("weather.write", policy => policy.AddRequirements(new PermissionRequirement("weather.write")));
    options.AddPolicy("users.read", policy => policy.AddRequirements(new PermissionRequirement("users.read")));
    options.AddPolicy("users.write", policy => policy.AddRequirements(new PermissionRequirement("users.write")));
    options.AddPolicy("users.delete", policy => policy.AddRequirements(new PermissionRequirement("users.delete")));
    options.AddPolicy("reports.view", policy => policy.AddRequirements(new PermissionRequirement("reports.view")));
    options.AddPolicy("reports.export", policy => policy.AddRequirements(new PermissionRequirement("reports.export")));
});

// Register custom handlers
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

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
    .AddAdditionalAssemblies(typeof(Demo3.BffRbac.Client._Imports).Assembly);

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

await DbSeeder.SeedDataAsync(app.Services);

app.Run();

