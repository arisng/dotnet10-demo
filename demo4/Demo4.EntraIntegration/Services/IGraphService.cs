using Demo4.EntraIntegration.Shared.Models;

namespace Demo4.EntraIntegration.Services;

/// <summary>
/// Service for calling Microsoft Graph API on behalf of authenticated Entra users
/// </summary>
public interface IGraphService
{
    /// <summary>
    /// Get user profile from Microsoft Graph API (/me endpoint)
    /// </summary>
    Task<UserProfile?> GetUserProfileAsync();
    
    /// <summary>
    /// Get user profile photo from Microsoft Graph API (/me/photo/$value endpoint)
    /// </summary>
    Task<byte[]?> GetUserPhotoAsync();
}
