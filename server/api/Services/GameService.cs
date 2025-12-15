using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using api.Models.Requests;
using api.Models.Responses;
using dataccess;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public class GameService(MyDbContext ctx, TimeProvider timeProvider) : IGameService
{
    public async Task<Player> CreatePlayer(CreatePlayerRequestDto dto)
    {
        Validator.ValidateObject(dto, new ValidationContext(dto), true);

        var emailTaken = await ctx.Players.AnyAsync(p => p.Email == dto.Email);
        if (emailTaken) throw new ValidationException("Email is already taken");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var player = new Player
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            Email = dto.Email,
            Phonenumber = dto.PhoneNumber,
            Passwordhash = ComputeSha512(dto.Password),
            Createdat = now,
            Funds = 0m,
            Isdeleted = false
        };

        ctx.Players.Add(player);
        await ctx.SaveChangesAsync();
        return player;
    }

    public async Task<List<Playerboard>> CreatePlayerBoards(string playerId, CreatePlayerBoardsRequestDto dto)
    {
        Validator.ValidateObject(dto, new ValidationContext(dto), true);

        var player = await ctx.Players.FirstOrDefaultAsync(p => p.Id == playerId)
                     ?? throw new ValidationException("Player not found");

        // Validate numbers
        var numbers = dto.SelectedNumbers.Distinct().OrderBy(n => n).ToList();
        if (numbers.Count < 5 || numbers.Count > 8)
            throw new ValidationException("You must select between 5 and 8 unique numbers");
        if (numbers.Any(n => n < 1 || n > 16))
            throw new ValidationException("Numbers must be between 1 and 16");

        // Determine current ISO week and year
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var currentWeek = ISOWeek.GetWeekOfYear(now);
        var currentYear = ISOWeek.GetYear(now);

        // Find the next N boards starting from current week
        var boards = await ctx.Boards
            .Where(b => b.Year > currentYear || (b.Year == currentYear && b.Weeknumber >= currentWeek))
            .OrderBy(b => b.Year).ThenBy(b => b.Weeknumber)
            .Take(dto.RepeatWeeks)
            .ToListAsync();

        if (boards.Count < dto.RepeatWeeks)
            throw new ValidationException("Not enough future boards are available to cover repeat weeks");

        var created = new List<Playerboard>();
        foreach (var board in boards)
        {
            var pb = new Playerboard
            {
                Id = Guid.NewGuid().ToString(),
                Playerid = player.Id,
                Boardid = board.Id,
                Createdat = now,
                Iswinner = false
            };
            ctx.Playerboards.Add(pb);

            foreach (var n in numbers)
            {
                ctx.Playerboardnumbers.Add(new Playerboardnumber
                {
                    Id = Guid.NewGuid().ToString(),
                    Playerboardid = pb.Id,
                    Selectednumber = n
                });
            }

            created.Add(pb);
        }

        await ctx.SaveChangesAsync();
        return created;
    }

    public async Task<List<Player>> GetPlayers()
    {
        return await ctx.Players
            .Where(p => !p.Isdeleted)
            .OrderByDescending(p => p.Createdat)
            .ToListAsync();
    }

    public async Task<Player?> GetPlayerById(string id)
    {
        return await ctx.Players
            .Include(p => p.Playerboards)
                .ThenInclude(pb => pb.Playerboardnumbers)
            .Where(p => !p.Isdeleted && p.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<Player> UpdatePlayer(string id, UpdatePlayerRequestDto dto)
    {
        var player = await ctx.Players.FirstOrDefaultAsync(p => p.Id == id)
                     ?? throw new ValidationException("Player not found");

        // Apply updates if provided
        if (!string.IsNullOrWhiteSpace(dto.Name)) player.Name = dto.Name!.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Phonenumber)) player.Phonenumber = dto.Phonenumber!.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var newEmail = dto.Email!.Trim();
            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(newEmail))
                throw new ValidationException("Email is invalid");
            if (!string.Equals(player.Email, newEmail, StringComparison.OrdinalIgnoreCase))
            {
                var taken = await ctx.Players.AnyAsync(p => p.Email == newEmail && p.Id != id)
                            || await ctx.Admins.AnyAsync(a => a.Email == newEmail);
                if (taken) throw new ValidationException("Email is already taken");
                player.Email = newEmail;
            }
        }

        await ctx.SaveChangesAsync();
        return player;
    }

    public async Task ChangePassword(string playerId, string currentPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(playerId)) throw new ValidationException("Player required");
        if (string.IsNullOrWhiteSpace(currentPassword)) throw new ValidationException("Current password required");
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Trim().Length < 6)
            throw new ValidationException("New password must be at least 6 characters");

        var player = await ctx.Players.FirstOrDefaultAsync(p => p.Id == playerId && !p.Isdeleted)
                     ?? throw new ValidationException("Player not found");

        var currentHash = ComputeSha512(currentPassword);
        if (!string.Equals(player.Passwordhash, currentHash, StringComparison.Ordinal))
            throw new ValidationException("Current password is incorrect");

        player.Passwordhash = ComputeSha512(newPassword);
        await ctx.SaveChangesAsync();
    }

    public async Task<Player> SoftDeletePlayer(string id)
    {
        var player = await ctx.Players.FirstOrDefaultAsync(p => p.Id == id)
                     ?? throw new ValidationException("Player not found");
        if (!player.Isdeleted)
        {
            player.Isdeleted = true;
            await ctx.SaveChangesAsync();
        }
        return player;
    }

    public async Task<Player> RestorePlayer(string id)
    {
        var player = await ctx.Players.FirstOrDefaultAsync(p => p.Id == id)
                     ?? throw new ValidationException("Player not found");
        if (player.Isdeleted)
        {
            player.Isdeleted = false;
            await ctx.SaveChangesAsync();
        }
        return player;
    }

    // Admin soft delete / restore
    public async Task<Admin> SoftDeleteAdmin(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ValidationException("Admin id required");
        var admin = await ctx.Admins.FirstOrDefaultAsync(a => a.Id == id)
                    ?? throw new ValidationException("Admin not found");
        if (!admin.Isdeleted)
        {
            admin.Isdeleted = true;
            await ctx.SaveChangesAsync();
        }
        return admin;
    }

    public async Task<Admin> RestoreAdmin(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ValidationException("Admin id required");
        var admin = await ctx.Admins.FirstOrDefaultAsync(a => a.Id == id)
                    ?? throw new ValidationException("Admin not found");
        if (admin.Isdeleted)
        {
            admin.Isdeleted = false;
            await ctx.SaveChangesAsync();
        }
        return admin;
    }

    public async Task<Board?> GetActiveBoard()
    {
        return await ctx.Boards
            .Include(b => b.Drawnnumbers)
            .Where(b => b.Isactive)
            .OrderByDescending(b => b.Year).ThenByDescending(b => b.Weeknumber)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Board>> GetRecentBoards(int take = 10)
    {
        take = Math.Clamp(take, 1, 100);
        return await ctx.Boards
            .Include(b => b.Drawnnumbers)
            .OrderByDescending(b => b.Year).ThenByDescending(b => b.Weeknumber)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<ActiveParticipantDto>> GetActiveParticipants()
    {
        var active = await GetActiveBoard();
        if (active == null) return new List<ActiveParticipantDto>();

        var drawn = await ctx.Drawnnumbers
            .Where(d => d.Boardid == active.Id)
            .Select(d => d.Drawnnumber1)
            .ToListAsync();

        var boards = await ctx.Playerboards
            .Include(pb => pb.Player)
            .Include(pb => pb.Playerboardnumbers)
            .Where(pb => pb.Boardid == active.Id && !pb.Player.Isdeleted)
            .ToListAsync();

        var list = new List<ActiveParticipantDto>();
        foreach (var pb in boards)
        {
            var numbers = pb.Playerboardnumbers.Select(n => n.Selectednumber).OrderBy(n => n).ToList();
            var matches = drawn.Count == 0 ? 0 : numbers.Count(n => drawn.Contains(n));
            list.Add(new ActiveParticipantDto
            {
                PlayerId = pb.Playerid,
                Name = pb.Player.Name,
                Email = pb.Player.Email,
                Numbers = numbers,
                Matches = matches
            });
        }

        return list
            .OrderByDescending(p => p.Matches)
            .ThenBy(p => p.Name)
            .ToList();
    }

    public async Task<List<GameHistoryDto>> GetGameHistory(int take = 10)
    {
        var boards = await GetRecentBoards(take);
        var result = new List<GameHistoryDto>();
        foreach (var b in boards)
        {
            var numbers = b.Drawnnumbers.Select(d => d.Drawnnumber1).OrderBy(n => n).ToList();
            var participantCount = await ctx.Playerboards.CountAsync(pb => pb.Boardid == b.Id);

            int winners = 0;
            if (numbers.Count > 0)
            {
                winners = await ctx.Playerboards
                    .Where(pb => pb.Boardid == b.Id)
                    .Select(pb => pb.Playerboardnumbers.Select(n => n.Selectednumber))
                    .CountAsync(nums => numbers.All(n => nums.Contains(n)));
            }

            result.Add(new GameHistoryDto
            {
                BoardId = b.Id,
                Week = b.Weeknumber,
                Year = b.Year,
                StartDate = b.Startdate,
                EndDate = b.Enddate,
                Numbers = numbers,
                Participants = participantCount,
                Winners = winners
            });
        }

        return result;
    }

    public async Task DrawWinningNumbersAndAdvance(string adminId, IReadOnlyCollection<int> numbers)
    {
        if (string.IsNullOrWhiteSpace(adminId)) throw new ValidationException("Admin required");
        if (numbers is null || numbers.Count != 3) throw new ValidationException("Exactly 3 numbers are required");
        var distinct = numbers.Distinct().ToList();
        if (distinct.Count != 3) throw new ValidationException("Numbers must be unique");
        if (distinct.Any(n => n < 1 || n > 16)) throw new ValidationException("Numbers must be between 1 and 16");

        var admin = await ctx.Admins.FirstOrDefaultAsync(a => a.Id == adminId && !a.Isdeleted)
                    ?? throw new ValidationException("Admin not found");

        var active = await ctx.Boards.OrderByDescending(b => b.Year).ThenByDescending(b => b.Weeknumber)
            .FirstOrDefaultAsync(b => b.Isactive);
        if (active == null) throw new ValidationException("No active board found");

        // Transactional: save numbers, deactivate current, activate next
        using var tx = await ctx.Database.BeginTransactionAsync();
        try
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            foreach (var n in distinct)
            {
                ctx.Drawnnumbers.Add(new Drawnnumber
                {
                    Id = Guid.NewGuid().ToString(),
                    Boardid = active.Id,
                    Drawnnumber1 = n,
                    Drawnat = now,
                    Drawnby = admin.Id
                });
            }

            active.Isactive = false;

            // Find next board by iso week/year ordering
            var next = await ctx.Boards
                .Where(b => b.Year > active.Year || (b.Year == active.Year && b.Weeknumber > active.Weeknumber))
                .OrderBy(b => b.Year).ThenBy(b => b.Weeknumber)
                .FirstOrDefaultAsync();

            if (next != null)
            {
                next.Isactive = true;
            }

            await ctx.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<Board> ActivateBoard(string boardId)
    {
        if (string.IsNullOrWhiteSpace(boardId)) throw new ValidationException("Board id required");
        var board = await ctx.Boards.FirstOrDefaultAsync(b => b.Id == boardId)
                    ?? throw new ValidationException("Board not found");

        // Ensure only one active board at a time
        var currentlyActive = await ctx.Boards.Where(b => b.Isactive && b.Id != board.Id).ToListAsync();
        foreach (var b in currentlyActive) b.Isactive = false;

        board.Isactive = true;
        await ctx.SaveChangesAsync();
        return board;
    }

    public async Task<Board> DeactivateBoard(string boardId)
    {
        if (string.IsNullOrWhiteSpace(boardId)) throw new ValidationException("Board id required");
        var board = await ctx.Boards.FirstOrDefaultAsync(b => b.Id == boardId)
                    ?? throw new ValidationException("Board not found");
        if (board.Isactive)
        {
            board.Isactive = false;
            await ctx.SaveChangesAsync();
        }
        return board;
    }

    // Fund Requests
    public async Task<Fundrequest> CreateFundRequest(string playerId, decimal amount, string transactionNumber)
    {
        if (string.IsNullOrWhiteSpace(playerId)) throw new ValidationException("Player required");
        if (amount <= 0) throw new ValidationException("Amount must be greater than zero");
        if (string.IsNullOrWhiteSpace(transactionNumber)) throw new ValidationException("Transaction number is required");

        var player = await ctx.Players.FirstOrDefaultAsync(p => p.Id == playerId && !p.Isdeleted)
                     ?? throw new ValidationException("Player not found");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var req = new Fundrequest
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Amount = amount,
            Transactionnumber = transactionNumber.Trim(),
            Status = "pending",
            Createdat = now,
            Processedat = null,
            Processedbyadminid = null
        };

        ctx.Fundrequests.Add(req);
        await ctx.SaveChangesAsync();
        return req;
    }

    public async Task<List<Fundrequest>> GetFundRequests(string? status = null)
    {
        var query = ctx.Fundrequests.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            status = status.Trim().ToLowerInvariant();
            if (status is not ("pending" or "approved" or "denied"))
                throw new ValidationException("Invalid status");
            query = query.Where(r => r.Status == status);
        }

        // Default: oldest first as requested
        return await query
            .OrderBy(r => r.Createdat)
            .ToListAsync();
    }

    public async Task<Fundrequest> ApproveFundRequest(string requestId, string adminId)
    {
        if (string.IsNullOrWhiteSpace(requestId)) throw new ValidationException("Request id required");
        if (string.IsNullOrWhiteSpace(adminId)) throw new ValidationException("Admin id required");

        var admin = await ctx.Admins.FirstOrDefaultAsync(a => a.Id == adminId && !a.Isdeleted)
                    ?? throw new ValidationException("Admin not found");

        var req = await ctx.Fundrequests.FirstOrDefaultAsync(r => r.Id == requestId)
                  ?? throw new ValidationException("Fund request not found");
        if (req.Status != "pending") throw new ValidationException("Only pending requests can be approved");

        var player = await ctx.Players.FirstOrDefaultAsync(p => p.Id == req.Playerid)
                     ?? throw new ValidationException("Player not found");

        req.Status = "approved";
        req.Processedat = timeProvider.GetUtcNow().UtcDateTime;
        req.Processedbyadminid = admin.Id;

        player.Funds += req.Amount;

        await ctx.SaveChangesAsync();
        return req;
    }

    public async Task<Fundrequest> DenyFundRequest(string requestId, string adminId)
    {
        if (string.IsNullOrWhiteSpace(requestId)) throw new ValidationException("Request id required");
        if (string.IsNullOrWhiteSpace(adminId)) throw new ValidationException("Admin id required");

        var admin = await ctx.Admins.FirstOrDefaultAsync(a => a.Id == adminId && !a.Isdeleted)
                    ?? throw new ValidationException("Admin not found");

        var req = await ctx.Fundrequests.FirstOrDefaultAsync(r => r.Id == requestId)
                  ?? throw new ValidationException("Fund request not found");
        if (req.Status != "pending") throw new ValidationException("Only pending requests can be denied");

        req.Status = "denied";
        req.Processedat = timeProvider.GetUtcNow().UtcDateTime;
        req.Processedbyadminid = admin.Id;

        await ctx.SaveChangesAsync();
        return req;
    }

    private static string ComputeSha512(string input)
    {
        var bytes = SHA512.HashData(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
