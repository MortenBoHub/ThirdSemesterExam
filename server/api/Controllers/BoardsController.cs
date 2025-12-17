using api.Models.Responses;
using api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BoardsController(IGameService gameService, IAuthService authService) : ControllerBase
{
    [HttpGet("active")]
    public async Task<ActionResult<object>> GetActive()
    {
        var board = await gameService.GetActiveBoard();
        if (board == null) return NotFound();
        return new
        {
            board.Id,
            board.Year,
            Week = board.Weeknumber,
            board.Startdate,
            board.Enddate,
            Numbers = board.Drawnnumbers.Select(d => d.Drawnnumber1).OrderBy(n => n).ToList()
        };
    }

    [HttpGet("participants")]
    public async Task<List<ActiveParticipantDto>> GetParticipants()
    {
        return await gameService.GetActiveParticipants();
    }

    [HttpGet("history")]
    public async Task<List<GameHistoryDto>> GetHistory([FromQuery] int take = 10)
    {
        return await gameService.GetGameHistory(take);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("draw")]
    public async Task<ActionResult> Draw([FromBody] DrawNumbersRequest dto)
    {
        var jwt = await authService.VerifyAndDecodeToken(Request.Headers.Authorization.FirstOrDefault());
        if (!string.Equals(jwt.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        await gameService.DrawWinningNumbersAndAdvance(jwt.Id, dto.Numbers);
        return Ok();
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}/activate")]
    public async Task<ActionResult> Activate([FromRoute] string id)
    {
        var jwt = await authService.VerifyAndDecodeToken(Request.Headers.Authorization.FirstOrDefault());
        if (!string.Equals(jwt.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var board = await gameService.ActivateBoard(id);
        return Ok(new { board.Id, board.Year, Week = board.Weeknumber, board.Isactive });
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}/deactivate")]
    public async Task<ActionResult> Deactivate([FromRoute] string id)
    {
        var jwt = await authService.VerifyAndDecodeToken(Request.Headers.Authorization.FirstOrDefault());
        if (!string.Equals(jwt.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var board = await gameService.DeactivateBoard(id);
        return Ok(new { board.Id, board.Year, Week = board.Weeknumber, board.Isactive });
    }
}

public class DrawNumbersRequest
{
    public List<int> Numbers { get; set; } = new();
}
