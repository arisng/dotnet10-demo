namespace Demo5_1.Shared.Models;

public class UserInfo
{
    public required string UserId { get; set; }
    public required string Email { get; set; }
    public List<string> Roles { get; set; } = [];
    public List<string> Permissions { get; set; } = [];
}
