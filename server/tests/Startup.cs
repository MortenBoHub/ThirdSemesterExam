using api;
using api.Models;
using dataccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace tests;

public class Startup
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public void ConfigureServices(IServiceCollection services)
    {
        // Start the container and wait for it to be ready
        _postgreSqlContainer.StartAsync().GetAwaiter().GetResult();
        var connectionString = _postgreSqlContainer.GetConnectionString();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppOptions:Db"] = connectionString,
                ["AppOptions:JwtSecret"] = "test-secret-1234567890-very-long-secret-to-satisfy-hs512-requirements-exp",
                ["AppOptions:EnableMockLogin"] = "false",
                ["AppOptions:EnableMockLoginAdmin"] = "false",
                ["AppOptions:EnableMockLoginUser"] = "false",
                ["AppOptions:JwtTtlMinutes"] = "60"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        
        // Use the existing Program.ConfigureServices but we need to override the DbContext
        Program.ConfigureServices(services, configuration, new MockHostEnvironment());

        // Override the DbContext to use the containerized Postgres
        services.RemoveAll(typeof(MyDbContext));
        services.RemoveAll(typeof(DbContextOptions<MyDbContext>));
        services.AddDbContext<MyDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        context.Database.OpenConnection();
        using (var command = context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "CREATE SCHEMA IF NOT EXISTS \"dødeduer\";";
            command.ExecuteNonQuery();
        }
        context.Database.EnsureCreated();

        // Add a FakeTimeProvider for tests that need to control time
        var ftp = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
        services.AddSingleton(ftp);
        // Also provide it as TimeProvider
        services.Replace(ServiceDescriptor.Singleton<TimeProvider>(ftp));
    }

    private class MockHostEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = "";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
