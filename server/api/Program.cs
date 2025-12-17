using System.Text;
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
    public static void ConfigureServices(IServiceCollection services)
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
            // Stage 3: standardize casing to camelCase
            opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
        services.AddOpenApiDocument();
        services.AddCors();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IGameService, GameService>();
        // Password hashing for players (Stage 3)
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

        // Authentication & Authorization (Stage 1)
        // Read JwtSecret from AppOptions (already registered as singleton)
        using (var sp = services.BuildServiceProvider())
        {
            var appOptions = sp.GetRequiredService<AppOptions>();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appOptions.JwtSecret));

            var isProd = string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Production", StringComparison.OrdinalIgnoreCase);
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = isProd;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = key,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1),
                        NameClaimType = nameof(Models.JwtClaims.Id), // "Id"
                        RoleClaimType = nameof(Models.JwtClaims.Role) // "Role"
                    };
                });
        }
        services.AddAuthorization();
        
    }

    public static void Main()
    {
        var builder = WebApplication.CreateBuilder();
		builder.Services.AddCors();

        ConfigureServices(builder.Services);
        var app = builder.Build();
        

        app.UseCors(config => config.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().SetIsOriginAllowed(x => true));
        
        /* Example endpoint placeholder
        app.MapGet("/", ([FromServices]MyDbContext dbContext) => 
        {
            return Results.Ok("Service is up");
        });*/
        
        app.UseExceptionHandler(config => { });
        
        // Safety guard: warn if mock logins are enabled in Production
        var appOptions = app.Services.GetRequiredService<AppOptions>();
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
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
        // Stage 1: Enable authentication/authorization middleware
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