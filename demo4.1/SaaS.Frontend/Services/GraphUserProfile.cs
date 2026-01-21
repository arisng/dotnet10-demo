namespace SaaS.Frontend.Services;

public sealed record GraphUserProfile(
    string? Id,
    string? DisplayName,
    string? Mail,
    string? UserPrincipalName);

