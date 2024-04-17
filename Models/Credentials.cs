using System.ComponentModel.DataAnnotations;

namespace trb_auth.Models;

public class Credentials
{
    [Required] [MinLength(1)] public string Email { get; set; } = null!;
    [Required] [MinLength(1)] public string Password { get; set; } = null!;
}