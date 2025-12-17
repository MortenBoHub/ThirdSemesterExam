using api.Models;
using api.Models.Requests;
using api.Services;
using dataccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JerneIF.Tests;

public class SetupTests
{
    private static MyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new MyDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private static AuthService CreateAuthService(MyDbContext ctx)
    {
        var logger = LoggerFactory.Create(b => { }).CreateLogger<AuthService>();
        var time = TimeProvider.System;
        var opts = new AppOptions
        {
            Db = "inmemory",
            JwtSecret = "test-secret-1234567890",
            EnableMockLogin = false,
            EnableMockLoginAdmin = false,
            EnableMockLoginUser = false,
            JwtTtlMinutes = 60
        };
        var hasher = new Api.Security.BcryptPasswordHasher();
        return new AuthService(ctx, logger, time, opts, hasher);
    }

    [Fact]
    public async Task RegisterReturnsJwtWhichCanVerifyAgain()
    {
        await using var ctx = CreateDbContext();
        var auth = CreateAuthService(ctx);
        var result = await auth.Register(new RegisterRequestDto
        {
            Email = $"t{Guid.NewGuid():N}@email.dk",
            Password = "Password123!"
        });
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        var token = await auth.VerifyAndDecodeToken(result.Token); // no throw == success
        Assert.Equal("User", token.Role);
        Assert.False(string.IsNullOrWhiteSpace(token.Id));
    }
}