using System.ComponentModel.DataAnnotations;

namespace api.Models.Requests;

public class UpdatePlayerRequestDto
{
    [MinLength(1)] public string? Name { get; set; }
    [EmailAddress] public string? Email { get; set; }
    [MinLength(3)] public string? Phonenumber { get; set; }
}
