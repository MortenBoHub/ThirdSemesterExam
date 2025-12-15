using System.Text.Json.Serialization;
using api.Etc;
using api.Services;
using Scalar.AspNetCore;
using Sieve.Models;
using Sieve.Services;

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
        });
        services.AddOpenApiDocument();
        services.AddCors();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IGameService, GameService>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
    }

    public static void Main()
    {
        var builder = WebApplication.CreateBuilder();
		builder.Services.AddCors();

        ConfigureServices(builder.Services);
        var app = builder.Build();

        app.UseCors(config => config.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().SetIsOriginAllowed(x => true));
        /*app.MapGet("/", ([FromServices]MyDbContext dbContext) => 
        {
            dbContext.dødeduer.ToList();
        });*/
        app.UseExceptionHandler(config => { });
        app.UseOpenApi();
        app.UseSwaggerUi();
        app.MapScalarApiReference(options => options.OpenApiRoutePattern = "/swagger/v1/swagger.json"
        );
        app.MapControllers();
        app.GenerateApiClientsFromOpenApi("/../../client/src/core/generated-client.ts").GetAwaiter().GetResult();

        app.Run();
    }
}