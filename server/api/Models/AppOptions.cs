using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class AppOptions
{
    [Required] [MinLength(1)] public string Db { get; set; } = null!;
    [Required] [MinLength(1)] public string JwtSecret { get; set; } = "thisisjustadefaultsecretfortestingpurposes";
    // Allows using mock logins (admin/admin and user/user) as a fallback when DB is unavailable
    public bool EnableMockLogin { get; set; } = true;

    // Stage 3: granular mock flags (legacy EnableMockLogin maps to both when true)
    public bool EnableMockLoginAdmin { get; set; } = true;
    public bool EnableMockLoginUser { get; set; } = true;

    // Stage 3: configurable token TTL (minutes)
    public int JwtTtlMinutes { get; set; } = 180;
}