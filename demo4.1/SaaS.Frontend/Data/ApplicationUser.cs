using Microsoft.AspNetCore.Identity;

namespace SaaS.Frontend.Data;

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

    /// <summary>
    /// Department synchronized from Microsoft Graph API
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// Office location synchronized from Microsoft Graph API
    /// </summary>
    public string? OfficeLocation { get; set; }

    /// <summary>
    /// Mobile phone synchronized from Microsoft Graph API
    /// </summary>
    public string? MobilePhone { get; set; }

    /// <summary>
    /// Last time the profile was synchronized from Microsoft Graph API
    /// </summary>
    public DateTimeOffset? LastGraphSync { get; set; }
}


