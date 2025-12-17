using api.Models;
using api.Services;
using Microsoft.AspNetCore.Mvc;
using Sieve.Models;
using Sieve.Services;
using dataccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FundRequestsController(
    IGameService gameService,
    IAuthService authService,
    ISieveProcessor sieve,
    MyDbContext ctx) : ControllerBase
{
    /// <summary>
    /// Player creates a fund request for adding money to their account
    /// </summary>
    [Authorize(Roles = "User")]
    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] CreateFundRequestDto dto)
    {
        var jwt = await authService.VerifyAndDecodeToken(Request.Headers.Authorization.FirstOrDefault());
        if (!string.Equals(jwt.Role, "User", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var created = await gameService.CreateFundRequest(jwt.Id, dto.Amount, dto.TransactionNumber);
        return Ok(new
        {
            created.Id,
            PlayerId = created.Playerid,
            created.Amount,
            TransactionNumber = created.Transactionnumber,
            created.Status,
            CreatedAt = created.Createdat
        });
    }

    /// <summary>
    /// Admin: list fund requests with optional status and Sieve query (filter/sort/page).
    /// Defaults to oldest first (CreatedAt ascending) when no Sorts are provided.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<List<object>>> List([FromQuery] string? status = null, [FromQuery] SieveModel? sieveModel = null)
    {
        var jwt = await authService.VerifyAndDecodeToken(Request.Headers.Authorization.FirstOrDefault());
        if (!string.Equals(jwt.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        IQueryable<Fundrequest> query = ctx.Fundrequests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim().ToLowerInvariant();
            // Keep backward-compatible status filter param
            if (normalized is "pending" or "approved" or "denied")
            {
                query = query.Where(r => r.Status == normalized);
            }
        }

        if (sieveModel != null)
        {
            query = sieve.Apply(sieveModel, query);

            // If no explicit Sorts were provided, enforce default ordering oldest-first
            var hasSorts = !string.IsNullOrWhiteSpace(sieveModel.Sorts);
            if (!hasSorts)
            {
                query = query.OrderBy(r => r.Createdat);
            }
        }
        else
        {
            // Default ordering when Sieve is not used
            query = query.OrderBy(r => r.Createdat);
        }

        var list = await query.ToListAsync();

        return Ok(list.Select(r => new
        {
            r.Id,
            PlayerId = r.Playerid,
            r.Amount,
            TransactionNumber = r.Transactionnumber,
            r.Status,
            CreatedAt = r.Createdat,
            ProcessedAt = r.Processedat,
            ProcessedByAdminId = r.Processedbyadminid
        }).ToList());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/approve")]
    public async Task<ActionResult<object>> Approve([FromRoute] string id)
    {
        var jwt = await authService.VerifyAndDecodeToken(Request.Headers.Authorization.FirstOrDefault());
        if (!string.Equals(jwt.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var updated = await gameService.ApproveFundRequest(id, jwt.Id);
        return Ok(new
        {
            updated.Id,
            updated.Status,
            ProcessedAt = updated.Processedat,
            ProcessedByAdminId = updated.Processedbyadminid
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/deny")]
    public async Task<ActionResult<object>> Deny([FromRoute] string id)
    {
        var jwt = await authService.VerifyAndDecodeToken(Request.Headers.Authorization.FirstOrDefault());
        if (!string.Equals(jwt.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var updated = await gameService.DenyFundRequest(id, jwt.Id);
        return Ok(new
        {
            updated.Id,
            updated.Status,
            ProcessedAt = updated.Processedat,
            ProcessedByAdminId = updated.Processedbyadminid
        });
    }
}

public class CreateFundRequestDto
{
    public decimal Amount { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
}
