using System.Linq;
using api;
using api.Models;
using dataccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JerneIF.Tests.Integration;

public class TestWebAppFactory : WebApplicationFactory<Program>
{
    public TestWebAppFactory()
    {
        // Ensure Program sees Production to avoid Testcontainers path during ConfigureServices
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Production");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use Production to avoid Testcontainers path in AddMyDbContext; we'll replace DbContext below
        builder.UseEnvironment("Production");

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registrations
            var descriptors = services.Where(d => d.ServiceType == typeof(DbContextOptions<MyDbContext>) || d.ServiceType == typeof(MyDbContext)).ToList();
            foreach (var d in descriptors)
            {
                services.Remove(d);
            }

            // Replace with InMemory DB
            services.AddDbContext<MyDbContext>(options =>
            {
                options.UseInMemoryDatabase($"integration-tests-{Guid.NewGuid():N}");
            });

            // Replace AppOptions with a test instance
            var appOptsDesc = services.FirstOrDefault(d => d.ServiceType == typeof(AppOptions));
            if (appOptsDesc != null) services.Remove(appOptsDesc);
            services.AddSingleton(new AppOptions
            {
                Db = "inmemory",
                JwtSecret = "int-tests-secret",
                EnableMockLogin = true,
                EnableMockLoginAdmin = true,
                EnableMockLoginUser = true,
                JwtTtlMinutes = 60
            });
        });
    }
}
