using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using api.Models;
using api.Models.Requests;
using dataccess;
using JWT;
using JWT.Algorithms;
using JWT.Builder;
using JWT.Serializers;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using ValidationException = Bogus.ValidationException;

namespace api.Services;

public class AuthService(
    MyDbContext ctx,
    ILogger<AuthService> logger,
    TimeProvider timeProvider,
    api.Models.AppOptions appOptions,
    IPasswordHasher<Player> passwordHasher) : IAuthService
{
    public async Task<JwtClaims> VerifyAndDecodeToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ValidationException("No token attached!");

        // Allow passing full Authorization header value
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            token = token.Substring("Bearer ".Length).Trim();

        // Validate token using the same secret/algorithm family as the JwtBearer middleware
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appOptions.JwtSecret));
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = nameof(JwtClaims.Id),
            RoleClaimType = nameof(JwtClaims.Role)
        };

        ClaimsPrincipal principal;
        try
        {
            principal = handler.ValidateToken(token, validationParams, out _);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to validate JWT token");
            throw new ValidationException("Failed to verify JWT");
        }

        // Map claims
        var id = principal.FindFirst(nameof(JwtClaims.Id))?.Value;
        var email = principal.FindFirst(nameof(JwtClaims.Email))?.Value;
        var role = principal.FindFirst(nameof(JwtClaims.Role))?.Value ?? "User";
        var isMockStr = principal.FindFirst(nameof(JwtClaims.IsMock))?.Value;
        bool isMock = false;
        if (!string.IsNullOrWhiteSpace(isMockStr))
            bool.TryParse(isMockStr, out isMock);

        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(email))
            throw new ValidationException("Authentication failed!");

        var jwtClaims = new JwtClaims(id, email, role, isMock);

        // For mock users (when enabled), do not require DB presence
        var mockAllowed = appOptions.EnableMockLogin || appOptions.EnableMockLoginAdmin || appOptions.EnableMockLoginUser;
        if (!(jwtClaims.IsMock && mockAllowed))
        {
            var roleNorm = (jwtClaims.Role ?? "User").Trim();
            var exists = roleNorm.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                ? ctx.Admins.Any(u => u.Id == jwtClaims.Id)
                : ctx.Players.Any(u => u.Id == jwtClaims.Id);

            if (!exists)
                throw new ValidationException("Authentication is valid, but user is not found!");
        }

        return jwtClaims;
    }

    public async Task<JwtResponse> Login(LoginRequestDto dto)
    {
        // Mock login fallback (granular control)
        var allowAdminMock = appOptions.EnableMockLoginAdmin || appOptions.EnableMockLogin;
        var allowUserMock = appOptions.EnableMockLoginUser || appOptions.EnableMockLogin;
        if (allowAdminMock || allowUserMock)
        {
            if (allowAdminMock && dto.Email.Equals("admin", StringComparison.OrdinalIgnoreCase) && dto.Password == "admin")
            {
                var tokenMockAdmin = CreateJwt(new JwtClaims(
                    Id: "admin-mock",
                    Email: "admin@mock.local",
                    Role: "Admin",
                    IsMock: true));
                return new JwtResponse(tokenMockAdmin);
            }
            if (allowUserMock && dto.Email.Equals("user", StringComparison.OrdinalIgnoreCase) && dto.Password == "user")
            {
                var tokenMockUser = CreateJwt(new JwtClaims(
                    Id: "user-mock",
                    Email: "user@mock.local",
                    Role: "User",
                    IsMock: true));
                return new JwtResponse(tokenMockUser);
            }
        }

        // Real DB login
        try
        {
            // Try Admin first (DB admin path remains as-is; mock admin exists for bootstrap)
            var admin = ctx.Admins.FirstOrDefault(u => u.Email == dto.Email);
            if (admin != null)
            {
                var adminHash = SHA512.HashData(Encoding.UTF8.GetBytes(dto.Password))
                    .Aggregate("", (current, b) => current + b.ToString("x2"));
                if (admin.Passwordhash != adminHash)
                    throw new ValidationException("Password is incorrect!");

                var tokenAdmin = CreateJwt(new JwtClaims(admin.Id, admin.Email, "Admin", false));
                return new JwtResponse(tokenAdmin);
            }

            // Then Player
            var player = ctx.Players.FirstOrDefault(u => u.Email == dto.Email)
                         ?? throw new ValidationException("User is not found!");
            var verify = passwordHasher.VerifyHashedPassword(player, player.Passwordhash, dto.Password);
            if (verify == PasswordVerificationResult.Failed)
                throw new ValidationException("Password is incorrect!");

            var token = CreateJwt(new JwtClaims(player.Id, player.Email, "User", false));
            return new JwtResponse(token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Login failed");
            // If DB error and mock login enabled, allow user/user fallback only
            if ((appOptions.EnableMockLoginUser || appOptions.EnableMockLogin) && dto.Email.Equals("user", StringComparison.OrdinalIgnoreCase) && dto.Password == "user")
            {
                var tokenMockUser = CreateJwt(new JwtClaims(
                    Id: "user-mock",
                    Email: "user@mock.local",
                    Role: "User",
                    IsMock: true));
                return new JwtResponse(tokenMockUser);
            }
            throw;
        }
    }

    public async Task<JwtResponse> Register(RegisterRequestDto dto)
    {
        Validator.ValidateObject(dto, new ValidationContext(dto), true);

        var isEmailTaken = ctx.Players.Any(u => u.Email == dto.Email) || ctx.Admins.Any(a => a.Email == dto.Email);
        if (isEmailTaken)
            throw new ValidationException("Email is already taken");

        var player = new Player
        {
            Email = dto.Email,
            Name = dto.Email.Split('@').FirstOrDefault() ?? dto.Email,
            Phonenumber = "00000000",
            Createdat = timeProvider.GetUtcNow().DateTime.ToUniversalTime(),
            Id = Guid.NewGuid().ToString(),
            Passwordhash = string.Empty,
            Funds = 0m,
            Isdeleted = false
        };
        player.Passwordhash = passwordHasher.HashPassword(player, dto.Password);
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync();

        var token = CreateJwt(new JwtClaims(player.Id, player.Email, "User", false));
        return new JwtResponse(token);
    }

    private JwtBuilder CreateJwtBuilder()
    {
        return JwtBuilder.Create()
            .WithAlgorithm(new HMACSHA512Algorithm())
            .WithSecret(appOptions.JwtSecret)
            .WithUrlEncoder(new JwtBase64UrlEncoder())
            .WithJsonSerializer(new JsonNetSerializer())
            .MustVerifySignature();
    }

    private string CreateJwt(JwtClaims claims)
    {
        var ttlMinutes = appOptions.JwtTtlMinutes <= 0 ? 180 : appOptions.JwtTtlMinutes;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appOptions.JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

        var nowEpoch = new DateTimeOffset(now).ToUnixTimeSeconds().ToString();

        var jwt = new JwtSecurityToken(
            claims: new[]
            {
                new Claim(nameof(JwtClaims.Id), claims.Id),
                new Claim(nameof(JwtClaims.Email), claims.Email),
                new Claim(nameof(JwtClaims.Role), claims.Role),
                new Claim(nameof(JwtClaims.IsMock), claims.IsMock.ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, nowEpoch, ClaimValueTypes.Integer64)
            },
            notBefore: now,
            expires: now.AddMinutes(ttlMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
