using Microsoft.Identity.Web;
using System.Security.Claims;

namespace Demo5_1.Web.Services;

public interface IApiTokenProvider
{
    Task<string?> GetTokenAsync();
}

public class HybridApiTokenProvider : IApiTokenProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly IConfiguration _configuration;

    public HybridApiTokenProvider(
        IHttpContextAccessor httpContextAccessor,
        ITokenAcquisition tokenAcquisition,
        IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _tokenAcquisition = tokenAcquisition;
        _configuration = configuration;
    }

    public async Task<string?> GetTokenAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || user.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        // Check if it's an Entra user (has oid claim or specific idp claim)
        var oid = user.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier") 
                  ?? user.FindFirstValue("oid");
        
        if (!string.IsNullOrEmpty(oid))
        {
            // Entra ID user
            var scopes = _configuration.GetSection("ApiService:Scopes").Get<string[]>() ?? Array.Empty<string>();
            return await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);
        }

        // Check for local token in claims (stored during local login)
        var localToken = user.FindFirstValue("api_access_token");
        return localToken;
    }
}
