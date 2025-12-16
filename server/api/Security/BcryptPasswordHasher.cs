using dataccess;
using Microsoft.AspNetCore.Identity;

namespace Api.Security;

public class BcryptPasswordHasher : IPasswordHasher<Player>
{
    public string HashPassword(Player player, string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public PasswordVerificationResult VerifyHashedPassword(
        Player player,
        string hashedPassword,
        string providedPassword
    )
    {
        var isValid = BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
        return isValid ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
    }
}