using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class AppOptions
{
    [Required] [MinLength(1)] public string Db { get; set; } = null!;
    [Required] [MinLength(1)] public string JwtSecret { get; set; } = "thisisjustadefaultsecretfortestingpurposes";
    // Allows using mock logins (admin/admin and user/user) as a fallback when DB is unavailable
    public bool EnableMockLogin { get; set; } = true;
}