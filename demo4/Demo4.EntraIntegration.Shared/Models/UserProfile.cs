namespace Demo4.EntraIntegration.Shared.Models;

/// <summary>
/// User profile data from Microsoft Graph API
/// </summary>
public class UserProfile
{
    public string? Id { get; set; }
    public string? DisplayName { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? OfficeLocation { get; set; }
    public string? MobilePhone { get; set; }
    public string? Mail { get; set; }
    public string? UserPrincipalName { get; set; }
}