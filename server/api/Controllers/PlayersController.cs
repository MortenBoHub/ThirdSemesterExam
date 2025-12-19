using api.Models.Requests;
using api.Services;
using dataccess;
using Microsoft.AspNetCore.Mvc;
using Sieve.Models;
using Sieve.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController(IGameService gameService, ISieveProcessor sieve, MyDbContext ctx, IAuthService authService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<object>>> GetPlayers([FromQuery] SieveModel? sieveModel)
    {
        // Support filtering/sorting/paging via Sieve
        var query = ctx.Players.AsNoTracking().Where(p => !p.Isdeleted);
        if (sieveModel != null)
        {
            query = sieve.Apply(sieveModel, query);
            // Paging is also applied by Sieve
        }
        else
        {
            query = query.OrderByDescending(p => p.Createdat);
        }

        var list = await query.ToListAsync();
        var projected = list.Select(p => new
        {
            p.Id,
            p.Name,
            p.Email,
            Phonenumber = p.Phonenumber,
            p.Funds,
            p.Createdat
        }).ToList<object>();
        return Ok(projected);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetPlayerById([FromRoute] string id)
    {
        var player = await gameService.GetPlayerById(id);
        if (player == null) return NotFound();
        return Ok(new
        {
            player.Id,
            player.Name,
            player.Email,
            Phonenumber = player.Phonenumber,
            player.Funds
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> CreatePlayer([FromBody] CreatePlayerRequestDto dto)
    {
        var created = await gameService.CreatePlayer(dto);
        return Ok(new
        {
            created.Id,
            created.Name,
            created.Email,
            Phonenumber = created.Phonenumber,
            created.Funds,
            created.Createdat
        });
    }

    [HttpPost("{playerId}/boards")]
    [Authorize]
    public async Task<ActionResult<List<Playerboard>>> CreateBoards([FromRoute] string playerId, [FromBody] CreatePlayerBoardsRequestDto dto)
    {
        var jwt = await authService.VerifyAndDecodeToken(Request.Headers.Authorization.FirstOrDefault());
        var isAdmin = string.Equals(jwt.Role, "Admin", StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && !string.Equals(jwt.Id, playerId, StringComparison.Ordinal))
            return Forbid();
        var list = await gameService.CreatePlayerBoards(playerId, dto);
        return Ok(list);
    }

    [HttpPatch("{id}")]
    [Authorize]
    public async Task<ActionResult<object>> UpdatePlayer([FromRoute] string id, [FromBody] UpdatePlayerRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var jwt = await authService.VerifyAndDecodeToken(Request.Headers.Authorization.FirstOrDefault());
        var isAdmin = string.Equals(jwt.Role, "Admin", StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && !string.Equals(jwt.Id, id, StringComparison.Ordinal))
            return Forbid();
        var updated = await gameService.UpdatePlayer(id, dto);
        return Ok(new
        {
            updated.Id,
            updated.Name,
            updated.Email,
            Phonenumber = updated.Phonenumber,
            updated.Funds
        });
    }

    [HttpPost("{id}/change-password")]
    [Authorize(Roles = "User")]
    public async Task<ActionResult> ChangePassword([FromRoute] string id, [FromBody] ChangePasswordRequestDto dto)
    {
        var jwt = await authService.VerifyAndDecodeToken(Request.Headers.Authorization.FirstOrDefault());
        if (!string.Equals(jwt.Role, "User", StringComparison.OrdinalIgnoreCase) || !string.Equals(jwt.Id, id, StringComparison.Ordinal))
            return Forbid();

        await gameService.ChangePassword(id, dto.CurrentPassword, dto.NewPassword);
        return Ok();
    }

    [HttpPatch("{id}/soft-delete")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> SoftDelete([FromRoute] string id)
    {
        var updated = await gameService.SoftDeletePlayer(id);
        return Ok(new { updated.Id, updated.Isdeleted });
    }

    [HttpPatch("{id}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> Restore([FromRoute] string id)
    {
        var updated = await gameService.RestorePlayer(id);
        return Ok(new { updated.Id, updated.Isdeleted });
    }
}
