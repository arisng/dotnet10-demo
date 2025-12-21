using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SaaS.ServiceDefaults;
using SaaS.Shared;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var tenantId = builder.Configuration["AzureAd:TenantId"];
        var audience = builder.Configuration["AzureAd:Audience"] ?? builder.Configuration["AzureAd:ClientId"];

        options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidAudiences = GetValidAudiences(audience),
            // Tokens for Entra ID custom APIs can have either the v2 issuer or the legacy AAD STS issuer.
            // Accept both to avoid common issuer-mismatch 401s.
            ValidIssuers = GetValidIssuers(tenantId),
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var hasAuthHeader = context.Request.Headers.Authorization.Count > 0;
                if (!hasAuthHeader)
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("SaaS.Backend.JwtBearer");
                    logger.LogWarning("No Authorization header on {Method} {Path}", context.Request.Method, context.Request.Path);
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("SaaS.Backend.JwtBearer");
                logger.LogWarning(context.Exception, "JWT authentication failed: {Message}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("SaaS.Backend.JwtBearer");

                var aud = context.Principal?.FindFirstValue("aud");
                var scp = context.Principal?.FindFirstValue("scp")
                          ?? context.Principal?.FindFirstValue("http://schemas.microsoft.com/identity/claims/scope");

                logger.LogInformation("JWT validated. aud={Audience} scp={Scopes}", aud, scp);
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "WeatherGet",
        policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(ctx =>
            {
                var scopes = ctx.User.FindFirst("scp")?.Value
                    ?? ctx.User.FindFirst("http://schemas.microsoft.com/identity/claims/scope")?.Value;

                return scopes?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("Weather.Get") == true;
            });
        });
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// In dev, the app is typically reached via service discovery using HTTP.
// Redirects can cause HttpClient to drop Authorization headers, resulting in 401s.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weather-forecast", () =>
{
    var forecast = Enumerable.Range(1, 5)
        .Select(index =>
            new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
            ))
        .ToArray();
    return forecast;
})
.RequireAuthorization("WeatherGet")
.WithName("GetWeatherForecast");

app.Run();

static IEnumerable<string> GetValidAudiences(string? audience)
{
    if (string.IsNullOrWhiteSpace(audience))
    {
        return [];
    }

    audience = audience.Trim();

    // Normalize common misconfigurations like "api://{clientId}/Weather.Get".
    if (audience.StartsWith("api://", StringComparison.OrdinalIgnoreCase) &&
        Uri.TryCreate(audience, UriKind.Absolute, out var audienceUri))
    {
        // Reconstruct without path/query/fragment.
        // For api://{clientId}/Weather.Get, this becomes api://{clientId}.
        var normalizedApiAudience = $"api://{audienceUri.Host}";
        return new[] { normalizedApiAudience, audienceUri.Host };
    }

    var slashIndex = audience.IndexOf('/');
    if (slashIndex > 0)
    {
        audience = audience[..slashIndex];
    }

    // Common patterns:
    // - api://{clientId}
    // - {clientId}
    // Be permissive in dev to reduce configuration friction.
    if (audience.StartsWith("api://", StringComparison.OrdinalIgnoreCase))
    {
        var withoutScheme = audience["api://".Length..].TrimEnd('/');
        return new[] { audience.TrimEnd('/'), withoutScheme };
    }

    return new[] { audience };
}

static IEnumerable<string> GetValidIssuers(string? tenantId)
{
    if (string.IsNullOrWhiteSpace(tenantId))
    {
        return [];
    }

    tenantId = tenantId.Trim();

    // If tenantId is a domain (e.g. contoso.onmicrosoft.com), we can't reliably construct the STS issuer.
    if (!Guid.TryParse(tenantId, out _))
    {
        return new[]
        {
            $"https://login.microsoftonline.com/{tenantId}/v2.0",
            $"https://login.microsoftonline.com/{tenantId}/",
        };
    }

    return new[]
    {
        // v2 issuer (sometimes used)
        $"https://login.microsoftonline.com/{tenantId}/v2.0",
        // legacy STS issuer (commonly used for access tokens)
        $"https://sts.windows.net/{tenantId}/",
    };
}
