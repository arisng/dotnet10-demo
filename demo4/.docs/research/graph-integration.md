# Microsoft Graph API Integration with ASP.NET Core

### IDownstreamApi Usage Patterns

**Package**: `Microsoft.Identity.Web.DownstreamApi`

**Configuration**:

```json
{
  "DownstreamApi": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": [ "user.read", "user.readbasic.all" ]
  }
}
```

**Service Registration**:

```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("DownstreamApi", 
        builder.Configuration.GetSection("DownstreamApi"))
    .AddDistributedTokenCaches();
```

**Usage in Controllers/Services**:

```csharp
public class UserController : ControllerBase
{
    private readonly IDownstreamApi _downstreamApi;
    
    public UserController(IDownstreamApi downstreamApi)
    {
        _downstreamApi = downstreamApi;
    }
    
    public async Task<IActionResult> GetUserProfile()
    {
        // Call Graph API on behalf of user
        var user = await _downstreamApi.GetForUserAsync<User>(
            "DownstreamApi",
            options => options.RelativePath = "me"
        );
        
        return Ok(user);
    }
}
```

**Strongly-Typed Calls**:

```csharp
// GET request with typed response
var user = await _downstreamApi.CallApiForUserAsync<User>(
    "DownstreamApi",
    options =>
    {
        options.HttpMethod = HttpMethod.Get;
        options.RelativePath = "me";
    });

// POST request with body and response
var createdItem = await _downstreamApi.CallApiForUserAsync<InputModel, OutputModel>(
    "DownstreamApi",
    inputData,
    options =>
    {
        options.HttpMethod = HttpMethod.Post;
        options.RelativePath = "users";
    });
```

### Microsoft Graph SDK Integration

**Package**: `Microsoft.Identity.Web.GraphServiceClient`

**Configuration**:

```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddMicrosoftGraph(builder.Configuration.GetSection("DownstreamApi"))
    .AddDistributedTokenCaches();
```

**Usage**:

```csharp
public class GraphController : ControllerBase
{
    private readonly GraphServiceClient _graphClient;
    
    public GraphController(GraphServiceClient graphClient)
    {
        _graphClient = graphClient;
    }
    
    public async Task<IActionResult> GetMe()
    {
        var user = await _graphClient.Me
            .GetAsync();
        
        return Ok(user);
    }
    
    public async Task<IActionResult> GetPhoto()
    {
        try
        {
            using var photoStream = await _graphClient.Me
                .Photo
                .Content
                .GetAsync();
                
            return File(photoStream, "image/jpeg");
        }
        catch (ServiceException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound("No photo available");
        }
    }
}
```

### On-Behalf-Of (OBO) Flow Implementation

**Scenario**: Web API needs to call downstream API on behalf of the user

**Web API Configuration**:

```csharp
// API Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("Graph", builder.Configuration.GetSection("GraphAPI"))
    .AddInMemoryTokenCaches();
```

**API Controller**:

```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserDataController : ControllerBase
{
    private readonly IDownstreamApi _downstreamApi;
    
    public UserDataController(IDownstreamApi downstreamApi)
    {
        _downstreamApi = downstreamApi;
    }
    
    [HttpGet("profile")]
    public async Task<IActionResult> GetUserProfile()
    {
        // OBO token exchange happens automatically
        var response = await _downstreamApi.CallApiForUserAsync(
            "Graph",
            options =>
            {
                options.RelativePath = "/me";
            });
            
        return Ok(await response.Content.ReadFromJsonAsync<User>());
    }
}
```

**How OBO Works**:
1. Client calls Web API with access token (Token A)
2. Web API validates Token A
3. Web API calls Microsoft Entra ID with Token A to request Token B for downstream API
4. Microsoft Entra ID validates Token A and issues Token B
5. Web API calls downstream API with Token B

**Key Requirements**:
- API must have `api://{clientId}/access_as_user` scope exposed
- Client must request delegated permission to the API
- API must request delegated permission to downstream API (e.g., Graph)

### Recommended Scopes for User Profile and Photo Access

**Microsoft Graph API Scopes**:

```json
{
  "DownstreamApi": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": [
      "user.read",           // Read signed-in user profile
      "user.readbasic.all",  // Read all users' basic profiles
      "user.readwrite",      // Read and write signed-in user profile
      "profile",             // Read user's profile
      "email",               // Read user's email address
      "openid"               // Sign in and read user profile
    ]
  }
}
```

**For Photo Access**:
- `User.Read` (includes profile photo)
- `User.ReadBasic.All` (basic info only, no photo)

**Graph API Endpoints**:
- User profile: `GET /me`
- User photo: `GET /me/photo/$value`
- User photo metadata: `GET /me/photo`
- All users: `GET /users`

### Error Handling and Retry Policies

**Handling MFA/Conditional Access Requirements**:

```csharp
try
{
    var result = await _downstreamApi.CallApiForUserAsync(...);
}
catch (MsalUiRequiredException ex)
{
    // User needs to complete MFA or accept consent
    // Return 401 with WWW-Authenticate header containing claims challenge
    Response.StatusCode = StatusCodes.Status401Unauthorized;
    Response.Headers.Add("WWW-Authenticate", 
        $"Bearer claims=\"{ex.Claims}\", error=\"{ex.Message}\"");
    return;
}
```

**Client Handling Claims Challenge**:

```csharp
// Client receives 401 and initiates new auth flow with claims
var authResult = await _tokenAcquisition.GetAuthenticationResultForUserAsync(
    scopes,
    claims: claimsFromWwwAuthenticate);
```

**Retry Policy for Transient Errors**:

```csharp
builder.Services.AddHttpClient("Graph", client =>
{
    client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0");
})
.AddPolicyHandler(GetRetryPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
```