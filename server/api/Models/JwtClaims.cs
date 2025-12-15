namespace api.Models;

public record JwtClaims(string Id, string Email, string Role, bool IsMock = false)
{
    public string Id { get; set; } = Id;
    public string Email { get; set; } = Email;
    public string Role { get; set; } = Role;
    public bool IsMock { get; set; } = IsMock;
}