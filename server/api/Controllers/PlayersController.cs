using api.Models.Requests;
using api.Services;
using dataccess;
using Microsoft.AspNetCore.Mvc;
using Sieve.Models;
using Sieve.Services;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController(IGameService gameService, ISieveProcessor sieve, MyDbContext ctx, IAuthService authService) : ControllerBase
{
    [HttpGet]
    public async Task<List<Player>> GetPlayers([FromQuery] SieveModel? sieveModel)
    {
        // Support filtering/sorting/paging via Sieve while keeping default behaviour
        var query = ctx.Players.AsNoTracking().Where(p => !p.Isdeleted);
        if (sieveModel != null)
        {
            query = sieve.Apply(sieveModel, query);
            // Paging is also applied by Sieve; fall through to ToListAsync
        }
        else
        {
            query = query.OrderByDescending(p => p.Createdat);
        }

        return await query.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Player>> GetPlayerById([FromRoute] string id)
    {
        var player = await gameService.GetPlayerById(id);
        if (player == null) return NotFound();
        return player;
    }

    [HttpPost]
    public async Task<Player> CreatePlayer([FromBody] CreatePlayerRequestDto dto)
    {
        return await gameService.CreatePlayer(dto);
    }

    [HttpPost("{playerId}/boards")]
    public async Task<List<Playerboard>> CreateBoards([FromRoute] string playerId, [FromBody] CreatePlayerBoardsRequestDto dto)
    {
        return await gameService.CreatePlayerBoards(playerId, dto);
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<Player>> UpdatePlayer([FromRoute] string id, [FromBody] UpdatePlayerRequestDto dto)
    {
        var updated = await gameService.UpdatePlayer(id, dto);
        return Ok(updated);
    }

    [HttpPost("{id}/change-password")]
    public async Task<ActionResult> ChangePassword([FromRoute] string id, [FromBody] ChangePasswordRequestDto dto)
    {
        var jwt = await authService.VerifyAndDecodeToken(Request.Headers.Authorization.FirstOrDefault());
        if (!string.Equals(jwt.Role, "User", StringComparison.OrdinalIgnoreCase) || !string.Equals(jwt.Id, id, StringComparison.Ordinal))
            return Forbid();

        await gameService.ChangePassword(id, dto.CurrentPassword, dto.NewPassword);
        return Ok();
    }

    [HttpPatch("{id}/soft-delete")]
    public async Task<ActionResult<Player>> SoftDelete([FromRoute] string id)
    {
        var updated = await gameService.SoftDeletePlayer(id);
        return Ok(updated);
    }

    [HttpPatch("{id}/restore")]
    public async Task<ActionResult<Player>> Restore([FromRoute] string id)
    {
        var updated = await gameService.RestorePlayer(id);
        return Ok(updated);
    }
}
