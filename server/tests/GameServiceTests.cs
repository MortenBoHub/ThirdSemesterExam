using System;
using System.Linq;
using System.Threading.Tasks;
using api.Models.Requests;
using api.Services;
using dataccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace JerneIF.Tests;

public class GameServiceTests
{
    private readonly IGameService _gameService;
    private readonly MyDbContext _context;
    private readonly FakeTimeProvider _fakeTimeProvider;

    public GameServiceTests(IGameService gameService, MyDbContext context, FakeTimeProvider fakeTimeProvider)
    {
        _gameService = gameService;
        _context = context;
        _fakeTimeProvider = fakeTimeProvider;
        
        // Ensure isolation by cleaning up boards and players before each test
        _context.Playerboardnumbers.RemoveRange(_context.Playerboardnumbers);
        _context.Playerboards.RemoveRange(_context.Playerboards);
        _context.Boards.RemoveRange(_context.Boards);
        _context.Players.RemoveRange(_context.Players);
        _context.Admins.RemoveRange(_context.Admins);
        _context.Fundrequests.RemoveRange(_context.Fundrequests);
        _context.Drawnnumbers.RemoveRange(_context.Drawnnumbers);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreatePlayerBoards_DeductsFunds_BasedOnNumberCount()
    {
        // Seed player with some funds
        var player = new Player
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Rich Player",
            Email = $"rich{Guid.NewGuid()}@example.com",
            Phonenumber = "123",
            Passwordhash = "x",
            Createdat = _fakeTimeProvider.GetUtcNow().UtcDateTime,
            Funds = 1000m,
            Isdeleted = false
        };
        _context.Players.Add(player);

        var now = _fakeTimeProvider.GetUtcNow().UtcDateTime;
        var year = System.Globalization.ISOWeek.GetYear(now);
        var week = System.Globalization.ISOWeek.GetWeekOfYear(now);
        var board = new Board { Id = Guid.NewGuid().ToString(), Year = year, Weeknumber = week, Isactive = true, Createdat = now };
        _context.Boards.Add(board);
        await _context.SaveChangesAsync();

        // 5 numbers -> 20kr
        await _gameService.CreatePlayerBoards(player.Id, new CreatePlayerBoardsRequestDto { SelectedNumbers = new() { 1, 2, 3, 4, 5 }, RepeatWeeks = 1 });
        var reloaded = await _context.Players.FirstAsync(p => p.Id == player.Id);
        Assert.Equal(980m, reloaded.Funds);

        // 8 numbers -> 160kr
        await _gameService.CreatePlayerBoards(player.Id, new CreatePlayerBoardsRequestDto { SelectedNumbers = new() { 1, 2, 3, 4, 5, 6, 7, 8 }, RepeatWeeks = 1 });
        reloaded = await _context.Players.FirstAsync(p => p.Id == player.Id);
        Assert.Equal(820m, reloaded.Funds);
    }

    [Fact]
    public async Task CreatePlayerBoards_InsufficientFunds_Throws()
    {
        var player = new Player
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Poor Player",
            Email = $"poor{Guid.NewGuid()}@example.com",
            Phonenumber = "123",
            Passwordhash = "x",
            Createdat = _fakeTimeProvider.GetUtcNow().UtcDateTime,
            Funds = 10m,
            Isdeleted = false
        };
        _context.Players.Add(player);

        var now = _fakeTimeProvider.GetUtcNow().UtcDateTime;
        var year = System.Globalization.ISOWeek.GetYear(now);
        var week = System.Globalization.ISOWeek.GetWeekOfYear(now);
        var board = new Board { Id = Guid.NewGuid().ToString(), Year = year, Weeknumber = week, Isactive = true, Createdat = now };
        _context.Boards.Add(board);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(async () =>
            await _gameService.CreatePlayerBoards(player.Id, new CreatePlayerBoardsRequestDto { SelectedNumbers = new() { 1, 2, 3, 4, 5 }, RepeatWeeks = 1 }));
    }

    [Fact]
    public async Task GetActiveBoard_Handles_NullDates()
    {
        var now = _fakeTimeProvider.GetUtcNow().UtcDateTime;
        var year = System.Globalization.ISOWeek.GetYear(now);
        var week = System.Globalization.ISOWeek.GetWeekOfYear(now);
        
        // Board with null dates
        var board = new Board 
        { 
            Id = Guid.NewGuid().ToString(), 
            Year = year, 
            Weeknumber = week, 
            Isactive = true, 
            Createdat = now,
            Startdate = null,
            Enddate = null
        };
        _context.Boards.Add(board);
        await _context.SaveChangesAsync();

        var active = await _gameService.GetActiveBoard();
        Assert.NotNull(active);
        Assert.Null(active.Startdate);
        Assert.Null(active.Enddate);
    }

    [Fact]
    public async Task CreatePlayer_HashesPassword_And_EnforcesUniqueEmail()
    {
        var p1 = await _gameService.CreatePlayer(new CreatePlayerRequestDto
        {
            Name = "A",
            Email = $"a{Guid.NewGuid()}@example.com",
            PhoneNumber = "12345",
            Password = "Password123!"
        });
        Assert.False(string.IsNullOrWhiteSpace(p1.Passwordhash));
        Assert.NotEqual("Password123!", p1.Passwordhash);

        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(async () =>
        {
            await _gameService.CreatePlayer(new CreatePlayerRequestDto
            {
                Name = "B",
                Email = p1.Email,
                PhoneNumber = "12345",
                Password = "AnotherPass!"
            });
        });
    }

    [Fact]
    public async Task CreatePlayerBoards_Validates_And_CreatesForFutureWeeks()
    {
        // Seed player
        var player = new Player
        {
            Id = Guid.NewGuid().ToString(),
            Name = "P",
            Email = $"p{Guid.NewGuid()}@example.com",
            Phonenumber = "11111",
            Passwordhash = "x",
            Createdat = _fakeTimeProvider.GetUtcNow().UtcDateTime,
            Funds = 1000m,
            Isdeleted = false
        };
        _context.Players.Add(player);

        // Seed two future boards relative to now
        var now = _fakeTimeProvider.GetUtcNow().UtcDateTime;
        var year = System.Globalization.ISOWeek.GetYear(now);
        var week = System.Globalization.ISOWeek.GetWeekOfYear(now);
        var b1 = new Board { Id = Guid.NewGuid().ToString(), Year = year, Weeknumber = week, Isactive = true, Createdat = now, Startdate = now, Enddate = now.AddDays(6) };
        var b2 = new Board { Id = Guid.NewGuid().ToString(), Year = year, Weeknumber = week + 1, Isactive = false, Createdat = now.AddDays(7), Startdate = now.AddDays(7), Enddate = now.AddDays(13) };
        _context.Boards.AddRange(b1, b2);
        await _context.SaveChangesAsync();

        // Invalid numbers
        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(async () =>
            await _gameService.CreatePlayerBoards(player.Id, new CreatePlayerBoardsRequestDto { SelectedNumbers = new() { 1, 2, 3, 4 }, RepeatWeeks = 1 }));

        // Valid
        var created = await _gameService.CreatePlayerBoards(player.Id, new CreatePlayerBoardsRequestDto
        {
            SelectedNumbers = new() { 1, 2, 3, 4, 5 },
            RepeatWeeks = 2
        });
        Assert.Equal(2, created.Count);
        var numbersForFirst = await _context.Playerboardnumbers.Where(n => n.Playerboardid == created[0].Id).Select(n => n.Selectednumber).OrderBy(n => n).ToListAsync();
        Assert.True(numbersForFirst.SequenceEqual(new[] { 1, 2, 3, 4, 5 }));
    }

    [Fact]
    public async Task FundRequests_Create_List_Approve_Deny()
    {
        var player = new Player
        {
            Id = Guid.NewGuid().ToString(),
            Email = $"p{Guid.NewGuid()}@example.com",
            Name = "P",
            Phonenumber = "11111",
            Passwordhash = "x",
            Createdat = _fakeTimeProvider.GetUtcNow().UtcDateTime,
            Funds = 0m,
            Isdeleted = false
        };
        var admin = new Admin
        {
            Id = Guid.NewGuid().ToString(),
            Email = $"a{Guid.NewGuid()}@example.com",
            Name = "A",
            Phonenumber = "22222",
            Passwordhash = "y",
            Createdat = _fakeTimeProvider.GetUtcNow().UtcDateTime,
            Isdeleted = false
        };
        _context.Players.Add(player);
        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        var r1 = await _gameService.CreateFundRequest(player.Id, 100m, "tx-1");
        _fakeTimeProvider.Advance(TimeSpan.FromMinutes(1));
        var r2 = await _gameService.CreateFundRequest(player.Id, 50m, "tx-2");

        var listed = await _gameService.GetFundRequests();
        Assert.Equal(new[] { r1.Id, r2.Id }, listed.Select(r => r.Id).ToArray());

        var approved = await _gameService.ApproveFundRequest(r1.Id, admin.Id);
        Assert.Equal("approved", approved.Status);
        var reloadedPlayer = await _context.Players.FirstAsync(p => p.Id == player.Id);
        Assert.Equal(100m, reloadedPlayer.Funds);

        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(async () => await _gameService.ApproveFundRequest(r1.Id, admin.Id));

        var denied = await _gameService.DenyFundRequest(r2.Id, admin.Id);
        Assert.Equal("denied", denied.Status);
    }

    [Fact]
    public async Task Player_Management_Get_Update_Delete_Restore()
    {
        var p = await _gameService.CreatePlayer(new CreatePlayerRequestDto
        {
            Name = "Original",
            Email = $"m{Guid.NewGuid()}@example.com",
            PhoneNumber = "12345678",
            Password = "Password123!"
        });

        // GetById
        var fetched = await _gameService.GetPlayerById(p.Id);
        Assert.Equal("Original", fetched!.Name);

        // Update
        await _gameService.UpdatePlayer(p.Id, new UpdatePlayerRequestDto
        {
            Name = "Updated",
            Email = p.Email,
            Phonenumber = "222"
        });
        var updated = await _gameService.GetPlayerById(p.Id);
        Assert.Equal("Updated", updated!.Name);
        Assert.Equal("222", updated.Phonenumber);

        // GetPlayers
        var all = await _gameService.GetPlayers();
        Assert.Contains(all, x => x.Id == p.Id);

        // SoftDelete
        await _gameService.SoftDeletePlayer(p.Id);
        var deleted = await _gameService.GetPlayerById(p.Id);
        Assert.Null(deleted); // GetPlayerById filters out deleted

        var allAfterDelete = await _gameService.GetPlayers();
        Assert.DoesNotContain(allAfterDelete, x => x.Id == p.Id);

        // Restore
        await _gameService.RestorePlayer(p.Id);
        var restored = await _gameService.GetPlayerById(p.Id);
        Assert.NotNull(restored);
    }

    [Fact]
    public async Task ChangePassword_Succeeds_And_FailsOnWrongCurrent()
    {
        var pass = "OldPass123!";
        var p = await _gameService.CreatePlayer(new CreatePlayerRequestDto
        {
            Name = "PassTest",
            Email = $"p{Guid.NewGuid()}@example.com",
            PhoneNumber = "12345678",
            Password = pass
        });

        await _gameService.ChangePassword(p.Id, pass, "NewPass123!");
        
        // Try login with new pass to verify (I'll just check if it doesn't throw if I were using AuthService, 
        // but here I check that old one fails if I try to change again or if I check hash)
        // Actually I can just try to change it again using the NEW password as current.
        await _gameService.ChangePassword(p.Id, "NewPass123!", "NewestPass123!");

        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(async () =>
            await _gameService.ChangePassword(p.Id, "WrongPass", "EvenNewer"));
    }

    [Fact]
    public async Task Admin_Management_Delete_Restore()
    {
        var admin = new Admin
        {
            Id = Guid.NewGuid().ToString(),
            Email = $"adm{Guid.NewGuid()}@example.com",
            Name = "Admin",
            Phonenumber = "000",
            Passwordhash = "hash",
            Createdat = DateTime.UtcNow,
            Isdeleted = false
        };
        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        await _gameService.SoftDeleteAdmin(admin.Id);
        var dbAdmin = await _context.Admins.FirstAsync(a => a.Id == admin.Id);
        Assert.True(dbAdmin.Isdeleted);

        await _gameService.RestoreAdmin(admin.Id);
        dbAdmin = await _context.Admins.FirstAsync(a => a.Id == admin.Id);
        Assert.False(dbAdmin.Isdeleted);
    }

    [Fact]
    public async Task Board_Management_Activate_Deactivate_GetActive_GetRecent()
    {
        var admin = new Admin { Id = Guid.NewGuid().ToString(), Email = "a@a.dk", Name = "A", Passwordhash = "x", Phonenumber = "123456" };
        _context.Admins.Add(admin);
        var now = _fakeTimeProvider.GetUtcNow().UtcDateTime;
        var b1 = new Board { Id = Guid.NewGuid().ToString(), Year = 2025, Weeknumber = 1, Isactive = false, Createdat = now, Startdate = now, Enddate = now.AddDays(6) };
        var b2 = new Board { Id = Guid.NewGuid().ToString(), Year = 2025, Weeknumber = 2, Isactive = false, Createdat = now, Startdate = now.AddDays(7), Enddate = now.AddDays(13) };
        _context.Boards.AddRange(b1, b2);
        await _context.SaveChangesAsync();

        await _gameService.ActivateBoard(b1.Id);
        var active = await _gameService.GetActiveBoard();
        Assert.Equal(b1.Id, active!.Id);

        await _gameService.DeactivateBoard(b1.Id);
        active = await _gameService.GetActiveBoard();
        Assert.Null(active);

        // Add drawn numbers so they show up in GetRecentBoards (which is now filtered for history)
        _context.Drawnnumbers.Add(new Drawnnumber { Id = Guid.NewGuid().ToString(), Boardid = b1.Id, Drawnnumber1 = 1, Drawnat = now, Drawnby = admin.Id });
        _context.Drawnnumbers.Add(new Drawnnumber { Id = Guid.NewGuid().ToString(), Boardid = b2.Id, Drawnnumber1 = 2, Drawnat = now, Drawnby = admin.Id });
        await _context.SaveChangesAsync();

        var recent = await _gameService.GetRecentBoards(10);
        Assert.Contains(recent, b => b.Id == b1.Id);
        Assert.Contains(recent, b => b.Id == b2.Id);
    }

    [Fact]
    public async Task DrawWinningNumbersAndAdvance_Logic_Test()
    {
        // Setup admin
        var admin = new Admin { Id = Guid.NewGuid().ToString(), Email = "a@a.dk", Name = "A", Passwordhash = "x", Phonenumber = "1" };
        _context.Admins.Add(admin);

        // Setup Player with enough funds
        var p1 = await _gameService.CreatePlayer(new CreatePlayerRequestDto { Name = "P1", Email = "p1@a.dk", PhoneNumber = "12345678", Password = "Password123!" });
        p1.Funds = 100m;
        
        // Setup Player with enough funds
        var p2 = await _gameService.CreatePlayer(new CreatePlayerRequestDto { Name = "P2", Email = "p2@a.dk", PhoneNumber = "87654321", Password = "Password123!" });
        p2.Funds = 100m;
        await _context.SaveChangesAsync();

        // Setup Board
        var now = _fakeTimeProvider.GetUtcNow().UtcDateTime;
        var b1 = new Board { Id = Guid.NewGuid().ToString(), Year = 2025, Weeknumber = 1, Isactive = true, Createdat = now, Startdate = now, Enddate = now.AddDays(6) };
        var b2 = new Board { Id = Guid.NewGuid().ToString(), Year = 2025, Weeknumber = 2, Isactive = false, Createdat = now.AddDays(7), Startdate = now.AddDays(7), Enddate = now.AddDays(13) };
        _context.Boards.AddRange(b1, b2);
        await _context.SaveChangesAsync();

        // Create PlayerBoards for both
        await _gameService.CreatePlayerBoards(p1.Id, new CreatePlayerBoardsRequestDto { SelectedNumbers = new() { 1, 2, 3, 4, 5 }, RepeatWeeks = 1 });
        await _gameService.CreatePlayerBoards(p2.Id, new CreatePlayerBoardsRequestDto { SelectedNumbers = new() { 1, 2, 3, 4, 5 }, RepeatWeeks = 1 });

        // Get active participants
        var participants = await _gameService.GetActiveParticipants();
        // Both have boards for current active board
        Assert.Equal(2, participants.Count);

        // Draw numbers (exactly 3 required)
        await _gameService.DrawWinningNumbersAndAdvance(admin.Id, new[] { 1, 2, 3 });

        // Reload board
        var oldBoard = await _context.Boards.Include(b => b.Drawnnumbers).FirstAsync(b => b.Id == b1.Id);
        Assert.False(oldBoard.Isactive);
        Assert.Equal(3, oldBoard.Drawnnumbers.Count);
        Assert.NotNull(oldBoard.Enddate);

        // Verify next board is active and has start date
        var nextBoard = await _context.Boards.FirstAsync(b => b.Id == b2.Id);
        Assert.True(nextBoard.Isactive);
        Assert.NotNull(nextBoard.Startdate);

        // Check winners in history (since it's calculated there)
        var history = await _gameService.GetGameHistory();
        Assert.NotEmpty(history);
        var historyEntry = history.First(h => h.BoardId == b1.Id);
        Assert.Equal(2, historyEntry.Winners);

        // NEW: Check if Iswinner is persisted in DB
        var persistedWinner = await _context.Playerboards.FirstAsync(pb => pb.Playerid == p1.Id && pb.Boardid == b1.Id);
        Assert.True(persistedWinner.Iswinner);
    }

    [Fact]
    public async Task GetGameHistory_Filtering_And_Personalization_Test()
    {
        var admin = new Admin { Id = Guid.NewGuid().ToString(), Email = "a@a.dk", Name = "A", Passwordhash = "x", Phonenumber = "123456" };
        _context.Admins.Add(admin);

        var player = await _gameService.CreatePlayer(new CreatePlayerRequestDto { Name = "P", Email = "p@a.dk", PhoneNumber = "123456", Password = "Password123!" });
        player.Funds = 1000m;
        await _context.SaveChangesAsync();

        var now = _fakeTimeProvider.GetUtcNow().UtcDateTime;
        // B1: Completed (has drawn numbers)
        var b1 = new Board { Id = "B1", Year = 2025, Weeknumber = 1, Isactive = true, Createdat = now };
        // B2: Future
        var b2 = new Board { Id = "B2", Year = 2025, Weeknumber = 2, Isactive = false, Createdat = now };
        _context.Boards.AddRange(b1, b2);
        await _context.SaveChangesAsync();

        await _gameService.CreatePlayerBoards(player.Id, new CreatePlayerBoardsRequestDto { SelectedNumbers = new() { 1, 2, 3, 4, 5 }, RepeatWeeks = 2 });

        // Draw for B1
        await _gameService.DrawWinningNumbersAndAdvance(admin.Id, new[] { 1, 2, 3 });

        // Get history without player
        var historyGeneral = await _gameService.GetGameHistory();
        Assert.Single(historyGeneral); // Only B1 should show up
        Assert.Equal("B1", historyGeneral[0].BoardId);
        Assert.Empty(historyGeneral[0].PlayerBoards);

        // Get history with player
        var historyPersonal = await _gameService.GetGameHistory(10, player.Id);
        Assert.Single(historyPersonal);
        Assert.Single(historyPersonal[0].PlayerBoards);
        Assert.True(historyPersonal[0].PlayerBoards[0].IsWinner);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, historyPersonal[0].PlayerBoards[0].SelectedNumbers.ToArray());
    }

    [Fact]
    public async Task GetGameHistory_Admin_Sees_WinnerDetails()
    {
        var admin = new Admin { Id = Guid.NewGuid().ToString(), Email = "a@a.dk", Name = "A", Passwordhash = "x", Phonenumber = "123456" };
        _context.Admins.Add(admin);

        var player = await _gameService.CreatePlayer(new CreatePlayerRequestDto { Name = "Winner Player", Email = "winner@a.dk", PhoneNumber = "99999999", Password = "Password123!" });
        player.Funds = 1000m;
        await _context.SaveChangesAsync();

        var now = _fakeTimeProvider.GetUtcNow().UtcDateTime;
        var b1 = new Board { Id = "B1", Year = 2025, Weeknumber = 1, Isactive = true, Createdat = now };
        _context.Boards.Add(b1);
        await _context.SaveChangesAsync();

        await _gameService.CreatePlayerBoards(player.Id, new CreatePlayerBoardsRequestDto { SelectedNumbers = new() { 1, 2, 3, 4, 5 }, RepeatWeeks = 1 });

        // Draw for B1
        await _gameService.DrawWinningNumbersAndAdvance(admin.Id, new[] { 1, 2, 3 });

        // Get history as admin
        var historyAdmin = await _gameService.GetGameHistory(10, null, true);
        Assert.Single(historyAdmin);
        Assert.Single(historyAdmin[0].WinnerDetails);
        Assert.Equal("Winner Player", historyAdmin[0].WinnerDetails[0].Name);
        Assert.Equal("99999999", historyAdmin[0].WinnerDetails[0].Phonenumber);

        // Get history NOT as admin
        var historyUser = await _gameService.GetGameHistory(10, null, false);
        Assert.Empty(historyUser[0].WinnerDetails);
    }

    [Fact]
    public async Task DrawWinningNumbersAndAdvance_YearTransition_Test()
    {
        var admin = new Admin { Id = Guid.NewGuid().ToString(), Email = "a@a.dk", Name = "A", Passwordhash = "x", Phonenumber = "1" };
        _context.Admins.Add(admin);

        var now = _fakeTimeProvider.GetUtcNow().UtcDateTime;
        var b1 = new Board { Id = "2025-52", Year = 2025, Weeknumber = 52, Isactive = true, Createdat = now };
        var b2 = new Board { Id = "2026-01", Year = 2026, Weeknumber = 1, Isactive = false, Createdat = now };
        _context.Boards.AddRange(b1, b2);
        await _context.SaveChangesAsync();

        await _gameService.DrawWinningNumbersAndAdvance(admin.Id, new[] { 1, 2, 3 });

        var board52 = await _context.Boards.FirstAsync(b => b.Id == "2025-52");
        Assert.False(board52.Isactive);

        var board01 = await _context.Boards.FirstAsync(b => b.Id == "2026-01");
        Assert.True(board01.Isactive);
    }

    [Fact]
    public async Task GetGameHistory_BackwardCompatibility_Calculates_Winners()
    {
        var admin = new Admin { Id = Guid.NewGuid().ToString(), Email = "a@a.dk", Name = "A", Passwordhash = "x", Phonenumber = "123456" };
        _context.Admins.Add(admin);

        var player = await _gameService.CreatePlayer(new CreatePlayerRequestDto { Name = "Old Winner", Email = "old@a.dk", PhoneNumber = "123456", Password = "Password123!" });
        player.Funds = 1000m;
        await _context.SaveChangesAsync();

        var now = _fakeTimeProvider.GetUtcNow().UtcDateTime;
        // Board with NO endDate and NO isWinner set, but HAS drawn numbers
        var b1 = new Board { Id = "OldBoard", Year = 2025, Weeknumber = 1, Isactive = false, Createdat = now };
        _context.Boards.Add(b1);
        await _context.SaveChangesAsync();

        // Create player board but do NOT mark as winner
        var pb = new Playerboard { Id = "PB1", Boardid = b1.Id, Playerid = player.Id, Createdat = now, Iswinner = false };
        _context.Playerboards.Add(pb);
        _context.Playerboardnumbers.AddRange(
            new Playerboardnumber { Id = "N1", Playerboardid = "PB1", Selectednumber = 1 },
            new Playerboardnumber { Id = "N2", Playerboardid = "PB1", Selectednumber = 2 },
            new Playerboardnumber { Id = "N3", Playerboardid = "PB1", Selectednumber = 3 },
            new Playerboardnumber { Id = "N4", Playerboardid = "PB1", Selectednumber = 4 },
            new Playerboardnumber { Id = "N5", Playerboardid = "PB1", Selectednumber = 5 }
        );

        // Add drawn numbers manually to simulate old data
        _context.Drawnnumbers.AddRange(
            new Drawnnumber { Id = "D1", Boardid = b1.Id, Drawnnumber1 = 1, Drawnat = now, Drawnby = admin.Id },
            new Drawnnumber { Id = "D2", Boardid = b1.Id, Drawnnumber1 = 2, Drawnat = now, Drawnby = admin.Id },
            new Drawnnumber { Id = "D3", Boardid = b1.Id, Drawnnumber1 = 3, Drawnat = now, Drawnby = admin.Id }
        );
        await _context.SaveChangesAsync();

        // Get history as admin
        var history = await _gameService.GetGameHistory(10, null, true);
        
        Assert.Single(history);
        Assert.Equal(1, history[0].Winners); // Should be calculated!
        Assert.Single(history[0].WinnerDetails);
        Assert.Equal("Old Winner", history[0].WinnerDetails[0].Name);

        // Verify it was persisted back to DB
        var updatedPb = await _context.Playerboards.FirstAsync(x => x.Id == "PB1");
        Assert.True(updatedPb.Iswinner);
    }

    [Fact]
    public async Task Unhappy_Paths_Tests()
    {
        // Player not found
        var fetched = await _gameService.GetPlayerById("nonexistent");
        Assert.Null(fetched);

        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(async () =>
            await _gameService.UpdatePlayer("nonexistent", new UpdatePlayerRequestDto { Name = "X" }));

        // Admin not found
        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(async () =>
            await _gameService.DrawWinningNumbersAndAdvance("nonexistent", new[] { 1, 2, 3 }));

        // No active board
        var admin = new Admin { Id = Guid.NewGuid().ToString(), Email = "a@a.dk", Name = "A", Passwordhash = "x", Phonenumber = "123456" };
        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();
        
        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(async () =>
            await _gameService.DrawWinningNumbersAndAdvance(admin.Id, new[] { 1, 2, 3 }));
    }
}
