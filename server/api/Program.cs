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
            opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
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

        // Authentication & Authorization (Stage 1)
        // Read JwtSecret from AppOptions (already registered as singleton)
        using (var sp = services.BuildServiceProvider())
        {
            var appOptions = sp.GetRequiredService<AppOptions>();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appOptions.JwtSecret));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
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
        app.UseOpenApi();
        app.UseSwaggerUi();
        app.MapScalarApiReference(options => options.OpenApiRoutePattern = "/swagger/v1/swagger.json"
        );
        // Stage 1: Enable authentication/authorization middleware
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.GenerateApiClientsFromOpenApi("/../../client/src/core/generated-client.ts").GetAwaiter().GetResult();

        app.Run();
    }
    
}