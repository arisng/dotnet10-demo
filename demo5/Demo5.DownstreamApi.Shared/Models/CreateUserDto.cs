using System.ComponentModel.DataAnnotations;

namespace Demo5.DownstreamApi.Shared.Models;

public class CreateUserDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;
}
