using System;
using System.Threading.Tasks;
using api.Models;
using api.Models.Requests;
using api.Services;
using dataccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace JerneIF.Tests;

public class AuthServiceTests
{
    private static MyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var ctx = new MyDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private static AuthService CreateAuthService(MyDbContext ctx, AppOptions? opts = null, TimeProvider? time = null)
    {
        var logger = LoggerFactory.Create(builder => { }).CreateLogger<AuthService>();
        time ??= TimeProvider.System;
        opts ??= new AppOptions
        {
            Db = "inmemory",
            JwtSecret = "test-secret-1234567890",
            EnableMockLogin = false,
            EnableMockLoginAdmin = true,
            EnableMockLoginUser = true,
            JwtTtlMinutes = 60
        };
        var hasher = new Api.Security.BcryptPasswordHasher();
        return new AuthService(ctx, logger, time, opts, hasher);
    }

    [Fact]
    public async Task Register_Then_Login_Succeeds_And_WrongPassword_Fails()
    {
        await using var ctx = CreateDbContext();
        var svc = CreateAuthService(ctx, new AppOptions
        {
            Db = "inmemory",
            JwtSecret = "secret-1",
            EnableMockLogin = false,
            EnableMockLoginAdmin = false,
            EnableMockLoginUser = false,
            JwtTtlMinutes = 60
        });

        var email = "user1@example.com";
        var pass = "Password123!";
        var reg = await svc.Register(new RegisterRequestDto { Email = email, Password = pass });
        Assert.NotNull(reg);
        Assert.False(string.IsNullOrWhiteSpace(reg.Token));

        var ok = await svc.Login(new LoginRequestDto { Email = email, Password = pass });
        Assert.False(string.IsNullOrWhiteSpace(ok.Token));

        var act = async () => await svc.Login(new LoginRequestDto { Email = email, Password = "wrong" });
        await Assert.ThrowsAsync<Bogus.ValidationException>(act);
    }

    // TTL tests will be added later when time can be controlled; current implementation clamps TTL to >=1

    [Fact]
    public async Task Mock_Admin_Login_Depends_On_Flag()
    {
        await using var ctx1 = CreateDbContext();
        var svcWithMock = CreateAuthService(ctx1, new AppOptions
        {
            Db = "inmemory",
            JwtSecret = "secret-3",
            EnableMockLogin = false,
            EnableMockLoginAdmin = true,
            EnableMockLoginUser = false,
            JwtTtlMinutes = 60
        });
        var adminRes = await svcWithMock.Login(new LoginRequestDto { Email = "admin", Password = "admin" });
        Assert.False(string.IsNullOrWhiteSpace(adminRes.Token));

        await using var ctx2 = CreateDbContext();
        var svcNoMock = CreateAuthService(ctx2, new AppOptions
        {
            Db = "inmemory",
            JwtSecret = "secret-4",
            EnableMockLogin = false,
            EnableMockLoginAdmin = false,
            EnableMockLoginUser = false,
            JwtTtlMinutes = 60
        });
        var act = async () => await svcNoMock.Login(new LoginRequestDto { Email = "admin", Password = "admin" });
        await Assert.ThrowsAsync<Bogus.ValidationException>(act);
    }

    [Fact]
    public async Task ChangePassword_Flow_Works()
    {
        await using var ctx = CreateDbContext();
        var auth = CreateAuthService(ctx, new AppOptions
        {
            Db = "inmemory",
            JwtSecret = "secret-cp",
            EnableMockLogin = false,
            EnableMockLoginAdmin = false,
            EnableMockLoginUser = false,
            JwtTtlMinutes = 60
        });
        var hasher = new Api.Security.BcryptPasswordHasher();
        var game = new GameService(ctx, TimeProvider.System, hasher);

        var email = $"cp{Guid.NewGuid():N}@example.com";
        var original = await game.CreatePlayer(new CreatePlayerRequestDto
        {
            Name = "User",
            Email = email,
            PhoneNumber = "12345",
            Password = "OldPass123!"
        });

        // Old login works
        var token1 = await auth.Login(new LoginRequestDto { Email = email, Password = "OldPass123!" });
        Assert.False(string.IsNullOrWhiteSpace(token1.Token));

        // Wrong current password rejected
        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(async () =>
            await game.ChangePassword(original.Id, "Wrong", "NewPass123!"));

        // Change with correct current
        await game.ChangePassword(original.Id, "OldPass123!", "NewPass123!");

        // Old login fails
        await Assert.ThrowsAsync<Bogus.ValidationException>(async () =>
            await auth.Login(new LoginRequestDto { Email = email, Password = "OldPass123!" }));

        // New login works
        var token2 = await auth.Login(new LoginRequestDto { Email = email, Password = "NewPass123!" });
        Assert.False(string.IsNullOrWhiteSpace(token2.Token));
    }

    [Fact]
    public async Task Token_Expiry_Respects_TimeProvider()
    {
        await using var ctx = CreateDbContext();
        var fake = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(DateTimeOffset.UtcNow);
        var opts = new AppOptions
        {
            Db = "inmemory",
            JwtSecret = "secret-exp",
            EnableMockLogin = false,
            EnableMockLoginAdmin = false,
            EnableMockLoginUser = false,
            JwtTtlMinutes = 1
        };
        var svc = CreateAuthService(ctx, opts, fake);

        // Register issues a token using fake time
        var reg = await svc.Register(new RegisterRequestDto { Email = $"exp{Guid.NewGuid():N}@ex.com", Password = "Passw0rd!" });
        Assert.False(string.IsNullOrWhiteSpace(reg.Token));

        // Shortly before expiry -> valid
        fake.Advance(TimeSpan.FromSeconds(59));
        var claimsOk = await svc.VerifyAndDecodeToken(reg.Token);
        Assert.Equal("User", claimsOk.Role);

        // After expiry -> throws
        fake.Advance(TimeSpan.FromSeconds(2));
        await Assert.ThrowsAsync<Bogus.ValidationException>(async () => await svc.VerifyAndDecodeToken(reg.Token));
    }
}
