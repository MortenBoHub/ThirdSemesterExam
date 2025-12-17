using api.Services;
using dataccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminsController(IGameService gameService, IAuthService authService) : ControllerBase
{
    // Soft delete an admin (mark isDeleted = true)
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}/soft-delete")]
    public async Task<ActionResult<object>> SoftDelete([FromRoute] string id)
    {
        var jwt = await authService.VerifyAndDecodeToken(Request.Headers.Authorization.FirstOrDefault());
        if (!string.Equals(jwt.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var updated = await gameService.SoftDeleteAdmin(id);
        return Ok(new
        {
            updated.Id,
            updated.Email,
            updated.Name,
            updated.Isdeleted,
            updated.Createdat
        });
    }

    // Restore an admin (set isDeleted = false)
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}/restore")]
    public async Task<ActionResult<object>> Restore([FromRoute] string id)
    {
        var jwt = await authService.VerifyAndDecodeToken(Request.Headers.Authorization.FirstOrDefault());
        if (!string.Equals(jwt.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var updated = await gameService.RestoreAdmin(id);
        return Ok(new
        {
            updated.Id,
            updated.Email,
            updated.Name,
            updated.Isdeleted,
            updated.Createdat
        });
    }
}
