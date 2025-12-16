using api.Models;
using api.Models.Requests;
using api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost(nameof(Login))]
    [EnableRateLimiting("login")]
    public async Task<JwtResponse> Login([FromBody] LoginRequestDto dto)
    {
        return await authService.Login(dto);
    }

    [AllowAnonymous]
    [HttpPost(nameof(Register))]
    public async Task<JwtResponse> Register([FromBody] RegisterRequestDto dto)
    {
        return await authService.Register(dto);
    }

    [Authorize]
    [HttpPost(nameof(WhoAmI))]
    public async Task<JwtClaims> WhoAmI()
    {
        var jwtClaims = await authService.VerifyAndDecodeToken(Request.Headers.Authorization.FirstOrDefault());
        return jwtClaims;
    }
}