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

/// <summary>
/// User profile data from Microsoft Graph API
/// </summary>
public class UserProfile
{
    public string? Id { get; set; }
    public string? DisplayName { get; set; }
    public string? JobTitle { get; set; }
    public string? Mail { get; set; }
    public string? UserPrincipalName { get; set; }
}
