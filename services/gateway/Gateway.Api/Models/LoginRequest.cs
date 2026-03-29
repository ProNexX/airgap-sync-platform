using System.ComponentModel.DataAnnotations;

namespace Gateway.Api.Models;

public class LoginRequest
{
    [Required]
    [MaxLength(256)]
    public string Username { get; set; } = default!;

    [Required]
    [MaxLength(256)]
    public string Password { get; set; } = default!;
}
