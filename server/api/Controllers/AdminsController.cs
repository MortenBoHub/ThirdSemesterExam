using api.Services;
using dataccess;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminsController(IGameService gameService, IAuthService authService) : ControllerBase
{
    // Soft delete an admin (mark isDeleted = true)
    [HttpPatch("{id}/soft-delete")]
    public async Task<ActionResult<Admin>> SoftDelete([FromRoute] string id)
    {
        var jwt = await authService.VerifyAndDecodeToken(Request.Headers.Authorization.FirstOrDefault());
        if (!string.Equals(jwt.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var updated = await gameService.SoftDeleteAdmin(id);
        return Ok(updated);
    }

    // Restore an admin (set isDeleted = false)
    [HttpPatch("{id}/restore")]
    public async Task<ActionResult<Admin>> Restore([FromRoute] string id)
    {
        var jwt = await authService.VerifyAndDecodeToken(Request.Headers.Authorization.FirstOrDefault());
        if (!string.Equals(jwt.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var updated = await gameService.RestoreAdmin(id);
        return Ok(updated);
    }
}
