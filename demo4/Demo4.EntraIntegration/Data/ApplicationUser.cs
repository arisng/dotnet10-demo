using Microsoft.AspNetCore.Identity;

namespace Demo4.EntraIntegration.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// External authentication provider (e.g., "MicrosoftEntra", "Google")
    /// Null for local passkey/password accounts
    /// </summary>
    public string? ExternalAuthenticationProvider { get; set; }
    
    /// <summary>
    /// Microsoft Entra ID Object ID (oid claim)
    /// Used to link Entra identity to local user record
    /// </summary>
    public string? EntraObjectId { get; set; }
    
    /// <summary>
    /// Display name synchronized from Microsoft Graph API
    /// </summary>
    public string? DisplayName { get; set; }
    
    /// <summary>
    /// Job title synchronized from Microsoft Graph API
    /// </summary>
    public string? JobTitle { get; set; }
}


