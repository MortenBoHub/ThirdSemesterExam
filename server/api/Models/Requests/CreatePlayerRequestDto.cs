using System.ComponentModel.DataAnnotations;

namespace api.Models.Requests;

public record CreatePlayerRequestDto
{
    [Required] [MinLength(1)] public string Name { get; set; } = string.Empty;
    [Required] [EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] [MinLength(5)] public string PhoneNumber { get; set; } = string.Empty;
    [Required] [MinLength(8)] public string Password { get; set; } = string.Empty;
}
