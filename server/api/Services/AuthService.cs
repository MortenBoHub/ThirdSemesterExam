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
using ValidationException = Bogus.ValidationException;

namespace api.Services;

public class AuthService(
    MyDbContext ctx,
    ILogger<AuthService> logger,
    TimeProvider timeProvider,
    api.Models.AppOptions appOptions) : IAuthService
{
    public async Task<JwtClaims> VerifyAndDecodeToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ValidationException("No token attached!");

        // Allow passing full Authorization header value
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            token = token.Substring("Bearer ".Length).Trim();

        var builder = CreateJwtBuilder();

        string jsonString;
        try
        {
            jsonString = builder.Decode(token)
                         ?? throw new ValidationException("Authentication failed!");
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            throw new ValidationException("Failed to verify JWT");
        }

        var jwtClaims = JsonSerializer.Deserialize<JwtClaims>(jsonString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new ValidationException("Authentication failed!");

        // For mock users (when enabled), do not require DB presence
        if (!(jwtClaims.IsMock && appOptions.EnableMockLogin))
        {
            var role = (jwtClaims.Role ?? "User").Trim();
            var exists = role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                ? ctx.Admins.Any(u => u.Id == jwtClaims.Id)
                : ctx.Players.Any(u => u.Id == jwtClaims.Id);

            if (!exists)
                throw new ValidationException("Authentication is valid, but user is not found!");
        }

        return jwtClaims;
    }

    public async Task<JwtResponse> Login(LoginRequestDto dto)
    {
        // Mock login fallback
        if (appOptions.EnableMockLogin)
        {
            if (dto.Email.Equals("admin", StringComparison.OrdinalIgnoreCase) && dto.Password == "admin")
            {
                var tokenMockAdmin = CreateJwt(new JwtClaims(
                    Id: "admin-mock",
                    Email: "admin@mock.local",
                    Role: "Admin",
                    IsMock: true));
                return new JwtResponse(tokenMockAdmin);
            }
            if (dto.Email.Equals("user", StringComparison.OrdinalIgnoreCase) && dto.Password == "user")
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
            // Try Admin first
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
            var playerHash = SHA512.HashData(Encoding.UTF8.GetBytes(dto.Password))
                .Aggregate("", (current, b) => current + b.ToString("x2"));
            if (player.Passwordhash != playerHash)
                throw new ValidationException("Password is incorrect!");

            var token = CreateJwt(new JwtClaims(player.Id, player.Email, "User", false));
            return new JwtResponse(token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Login failed");
            // If DB error and mock login enabled, allow user/user fallback only
            if (appOptions.EnableMockLogin && dto.Email.Equals("user", StringComparison.OrdinalIgnoreCase) && dto.Password == "user")
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

        var hash = SHA512.HashData(Encoding.UTF8.GetBytes(dto.Password))
            .Aggregate("", (current, b) => current + b.ToString("x2"));
        var player = new Player
        {
            Email = dto.Email,
            Name = dto.Email.Split('@').FirstOrDefault() ?? dto.Email,
            Phonenumber = "00000000",
            Createdat = timeProvider.GetUtcNow().DateTime.ToUniversalTime(),
            Id = Guid.NewGuid().ToString(),
            Passwordhash = hash,
            Funds = 0m,
            Isdeleted = false
        };
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
        return CreateJwtBuilder()
            .AddClaim(nameof(JwtClaims.Id), claims.Id)
            .AddClaim(nameof(JwtClaims.Email), claims.Email)
            .AddClaim(nameof(JwtClaims.Role), claims.Role)
            .AddClaim(nameof(JwtClaims.IsMock), claims.IsMock)
            .Encode();
    }
}