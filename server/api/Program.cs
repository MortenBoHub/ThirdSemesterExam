using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using dataccess;
using api.Etc;
using api.Services;
using Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Sieve.Models;
using Sieve.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using api.Models;
using Microsoft.AspNetCore.RateLimiting;

namespace api;

public class Program
{
    // Backward-compatible overload for test startup code
    public static void ConfigureServices(IServiceCollection services)
    {
        var tmp = WebApplication.CreateBuilder();
        ConfigureServices(services, tmp.Configuration, tmp.Environment);
    }

    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
    {
        
        
        services.AddSingleton(TimeProvider.System);
        services.InjectAppOptions();
        
        services.AddMyDbContext();
        // Sieve (filtering/sorting/paging)
        services.AddOptions<SieveOptions>();
        services.AddScoped<ISieveProcessor, ApplicationSieveProcessor>();
        services.AddControllers().AddJsonOptions(opts =>
        {
            // After DTO shaping, avoid $id/$ref artifacts
            opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            opts.JsonSerializerOptions.MaxDepth = 128;
            // standardize casing to camelCase
            opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
        services.AddOpenApiDocument();
        // CORS
        services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
            {
                if (env.IsDevelopment())
                {
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
                else
                {
                    var origins = configuration.GetSection("AppOptions:AllowedCorsOrigins").Get<string[]>() ?? Array.Empty<string>();
                    if (origins.Length > 0)
                    {
                        policy
                            .WithOrigins(origins)
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    }
                    else
                    {
                        policy.SetIsOriginAllowed(_ => false);
                    }
                }
            });
        });
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IGameService, GameService>();
        // Password hashing for players
        services.AddScoped<IPasswordHasher<Player>, BcryptPasswordHasher>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        // Rate limiting (login)
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("login", opt =>
            {
                opt.PermitLimit = 10;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });
        });

        // Authentication & Authorization
        // Use a single source of truth for JwtSecret from configuration
        var jwtSecret = configuration["AppOptions:JwtSecret"] ?? "thisisjustadefaultsecretfortestingpurposes";
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = env.IsProduction();
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = nameof(Models.JwtClaims.Id), // "Id"
                    RoleClaimType = nameof(Models.JwtClaims.Role) // "Role"
                };
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogError(context.Exception, "JWT authentication failed");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var db = context.HttpContext.RequestServices.GetRequiredService<dataccess.MyDbContext>();
                        var id = context.Principal?.FindFirst(nameof(api.Models.JwtClaims.Id))?.Value;
                        var role = context.Principal?.FindFirst(nameof(api.Models.JwtClaims.Role))?.Value;
                        var isMock = context.Principal?.FindFirst(nameof(api.Models.JwtClaims.IsMock))?.Value == "True";

                        if (string.IsNullOrEmpty(id) || isMock) return;

                        bool isDeleted = role == "Admin" 
                            ? await db.Admins.AnyAsync(a => a.Id == id && a.Isdeleted)
                            : await db.Players.AnyAsync(p => p.Id == id && p.Isdeleted);

                        if (isDeleted)
                        {
                            context.Fail("User is deleted");
                        }
                    }
                };
            });
        services.AddAuthorization();
        
    }

    public static void Main()
    {
        var builder = WebApplication.CreateBuilder();

        ConfigureServices(builder.Services, builder.Configuration, builder.Environment);
        var app = builder.Build();
        
        // Apply named CORS policy
        app.UseCors("Frontend");
        
        /* Example endpoint placeholder
        app.MapGet("/", ([FromServices]MyDbContext dbContext) => 
        {
            return Results.Ok("Service is up");
        });*/
        
        app.UseExceptionHandler(config => { });
        
        // Safety guard, warn if mock logins are enabled in Production
        var appOptions = app.Services.GetRequiredService<AppOptions>();
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        // Log non-reversible fingerprints of JwtSecret for signer/validator parity checks
        try
        {
            var configSecret = app.Configuration["AppOptions:JwtSecret"] ?? string.Empty;
            var signerHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(appOptions.JwtSecret ?? string.Empty))).ToLowerInvariant();
            var validatorHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(configSecret))).ToLowerInvariant();
            logger.LogInformation("JWT secret fingerprints — Signer(AppOptions): {Signer}, Validator(Config): {Validator}", signerHash[..8], validatorHash[..8]);
        }
        catch
        {
            
        }
        if (app.Environment.IsProduction() &&
            (appOptions.EnableMockLogin || appOptions.EnableMockLoginAdmin || appOptions.EnableMockLoginUser))
        {
            logger.LogWarning(
                "Mock login is ENABLED in Production. AdminMock={AdminMock}, UserMock={UserMock}, LegacyMock={LegacyMock}",
                appOptions.EnableMockLoginAdmin, appOptions.EnableMockLoginUser, appOptions.EnableMockLogin);
        }
        app.UseOpenApi();
        app.UseSwaggerUi();
        app.MapScalarApiReference(options => options.OpenApiRoutePattern = "/swagger/v1/swagger.json"
        );
        // Enable authentication/authorization middleware
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        
        // Health endpoint
        app.MapGet("/api/health", () => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow }))
            .AllowAnonymous();
        app.MapControllers();
        if (app.Environment.IsDevelopment())
        {
            app.GenerateApiClientsFromOpenApi("/../../client/src/core/generated-client.ts").GetAwaiter().GetResult();
        }

        app.Run();
    }
    
}
