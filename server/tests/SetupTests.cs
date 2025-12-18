using api.Models.Requests;
using api.Services;
using Xunit;

namespace JerneIF.Tests;

public class SetupTests
{
    private readonly IAuthService _authService;

    public SetupTests(IAuthService authService)
    {
        _authService = authService;
    }

    [Fact]
    public async Task RegisterReturnsJwtWhichCanVerifyAgain()
    {
        var result = await _authService.Register(new RegisterRequestDto
        {
            Email = $"t{Guid.NewGuid():N}@email.dk",
            Password = "Password123!"
        });
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        var token = await _authService.VerifyAndDecodeToken(result.Token); // no throw == success
        Assert.Equal("User", token.Role);
        Assert.False(string.IsNullOrWhiteSpace(token.Id));
    }
}
