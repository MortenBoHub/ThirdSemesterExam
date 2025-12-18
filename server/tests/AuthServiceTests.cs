using System;
using System.Threading.Tasks;
using api.Models;
using api.Models.Requests;
using api.Services;
using dataccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace JerneIF.Tests;

public class AuthServiceTests
{
    private readonly IAuthService _authService;
    private readonly MyDbContext _context;
    private readonly FakeTimeProvider _fakeTimeProvider;

    public AuthServiceTests(IAuthService authService, MyDbContext context, FakeTimeProvider fakeTimeProvider)
    {
        _authService = authService;
        _context = context;
        _fakeTimeProvider = fakeTimeProvider;
    }

    [Fact]
    public async Task Register_Then_Login_Succeeds_And_WrongPassword_Fails()
    {
        var email = $"user{Guid.NewGuid()}@example.com";
        var pass = "Password123!";
        var reg = await _authService.Register(new RegisterRequestDto { Email = email, Password = pass });
        Assert.NotNull(reg);
        Assert.False(string.IsNullOrWhiteSpace(reg.Token));

        var ok = await _authService.Login(new LoginRequestDto { Email = email, Password = pass });
        Assert.False(string.IsNullOrWhiteSpace(ok.Token));

        var act = async () => await _authService.Login(new LoginRequestDto { Email = email, Password = "wrong" });
        await Assert.ThrowsAsync<Bogus.ValidationException>(act);
    }

    [Fact]
    public async Task Login_UserNotFound_Throws()
    {
        var act = async () => await _authService.Login(new LoginRequestDto { Email = "nonexistent@example.com", Password = "any" });
        await Assert.ThrowsAsync<Bogus.ValidationException>(act);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Throws()
    {
        var email = $"dup{Guid.NewGuid()}@example.com";
        await _authService.Register(new RegisterRequestDto { Email = email, Password = "Password123!" });
        
        var act = async () => await _authService.Register(new RegisterRequestDto { Email = email, Password = "Password123!" });
        await Assert.ThrowsAsync<Bogus.ValidationException>(act);
    }

    [Fact]
    public async Task VerifyAndDecodeToken_InvalidToken_Throws()
    {
        var act = async () => await _authService.VerifyAndDecodeToken("invalid.token.here");
        await Assert.ThrowsAsync<Bogus.ValidationException>(act);
    }

    [Fact]
    public async Task VerifyAndDecodeToken_ExpiredToken_Throws()
    {
        var email = $"expired{Guid.NewGuid()}@example.com";
        var reg = await _authService.Register(new RegisterRequestDto { Email = email, Password = "Password123!" });
        
        // Advance time beyond TTL (default 60 mins in Startup.cs for tests) + ClockSkew (1 min)
        _fakeTimeProvider.Advance(TimeSpan.FromMinutes(62));
        
        var act = async () => await _authService.VerifyAndDecodeToken(reg.Token);
        await Assert.ThrowsAsync<Bogus.ValidationException>(act);
    }
}
