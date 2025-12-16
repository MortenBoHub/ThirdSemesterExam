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
    private static MyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new MyDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private static GameService CreateGameService(MyDbContext ctx, FakeTimeProvider? ftp = null)
    {
        var time = (TimeProvider)(ftp ?? new FakeTimeProvider(DateTimeOffset.UtcNow));
        var hasher = new Api.Security.BcryptPasswordHasher();
        return new GameService(ctx, time, hasher);
    }

    [Fact]
    public async Task CreatePlayer_HashesPassword_And_EnforcesUniqueEmail()
    {
        await using var ctx = CreateDbContext();
        var svc = CreateGameService(ctx);

        var p1 = await svc.CreatePlayer(new CreatePlayerRequestDto
        {
            Name = "A",
            Email = "a@example.com",
            PhoneNumber = "12345",
            Password = "Password123!"
        });
        Assert.False(string.IsNullOrWhiteSpace(p1.Passwordhash));
        Assert.NotEqual("Password123!", p1.Passwordhash);

        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(async () =>
        {
            await svc.CreatePlayer(new CreatePlayerRequestDto
            {
                Name = "B",
                Email = "a@example.com",
                PhoneNumber = "12345",
                Password = "AnotherPass!"
            });
        });
    }

    [Fact]
    public async Task CreatePlayerBoards_Validates_And_CreatesForFutureWeeks()
    {
        await using var ctx = CreateDbContext();
        var ftp = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var svc = CreateGameService(ctx, ftp);

        // Seed player
        var player = new Player
        {
            Id = Guid.NewGuid().ToString(),
            Name = "P",
            Email = "p@example.com",
            Phonenumber = "11111",
            Passwordhash = "x",
            Createdat = ftp.GetUtcNow().UtcDateTime,
            Funds = 0m,
            Isdeleted = false
        };
        ctx.Players.Add(player);

        // Seed two future boards relative to now
        var now = ftp.GetUtcNow().UtcDateTime;
        var year = System.Globalization.ISOWeek.GetYear(now);
        var week = System.Globalization.ISOWeek.GetWeekOfYear(now);
        var b1 = new Board { Id = Guid.NewGuid().ToString(), Year = year, Weeknumber = week, Isactive = true, Createdat = now, Startdate = now, Enddate = now.AddDays(6) };
        var b2 = new Board { Id = Guid.NewGuid().ToString(), Year = year, Weeknumber = week + 1, Isactive = false, Createdat = now.AddDays(7), Startdate = now.AddDays(7), Enddate = now.AddDays(13) };
        ctx.Boards.AddRange(b1, b2);
        await ctx.SaveChangesAsync();

        // Invalid numbers
        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(async () =>
            await svc.CreatePlayerBoards(player.Id, new CreatePlayerBoardsRequestDto { SelectedNumbers = new() { 1, 2, 3, 4 }, RepeatWeeks = 1 }));

        // Valid
        var created = await svc.CreatePlayerBoards(player.Id, new CreatePlayerBoardsRequestDto
        {
            SelectedNumbers = new() { 1, 2, 3, 4, 5 },
            RepeatWeeks = 2
        });
        Assert.Equal(2, created.Count);
        var numbersForFirst = await ctx.Playerboardnumbers.Where(n => n.Playerboardid == created[0].Id).Select(n => n.Selectednumber).OrderBy(n => n).ToListAsync();
        Assert.True(numbersForFirst.SequenceEqual(new[] { 1, 2, 3, 4, 5 }));
    }

    [Fact]
    public async Task FundRequests_Create_List_Approve_Deny()
    {
        await using var ctx = CreateDbContext();
        var ftp = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var svc = CreateGameService(ctx, ftp);

        var player = new Player
        {
            Id = Guid.NewGuid().ToString(),
            Email = "p@example.com",
            Name = "P",
            Phonenumber = "11111",
            Passwordhash = "x",
            Createdat = ftp.GetUtcNow().UtcDateTime,
            Funds = 0m,
            Isdeleted = false
        };
        var admin = new Admin
        {
            Id = Guid.NewGuid().ToString(),
            Email = "a@example.com",
            Name = "A",
            Phonenumber = "22222",
            Passwordhash = "y",
            Createdat = ftp.GetUtcNow().UtcDateTime,
            Isdeleted = false
        };
        ctx.Players.Add(player);
        ctx.Admins.Add(admin);
        await ctx.SaveChangesAsync();

        var r1 = await svc.CreateFundRequest(player.Id, 100m, "tx-1");
        ftp.Advance(TimeSpan.FromMinutes(1));
        var r2 = await svc.CreateFundRequest(player.Id, 50m, "tx-2");

        var listed = await svc.GetFundRequests();
        Assert.Equal(new[] { r1.Id, r2.Id }, listed.Select(r => r.Id).ToArray());

        var approved = await svc.ApproveFundRequest(r1.Id, admin.Id);
        Assert.Equal("approved", approved.Status);
        var reloadedPlayer = await ctx.Players.FirstAsync(p => p.Id == player.Id);
        Assert.Equal(100m, reloadedPlayer.Funds);

        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(async () => await svc.ApproveFundRequest(r1.Id, admin.Id));

        var denied = await svc.DenyFundRequest(r2.Id, admin.Id);
        Assert.Equal("denied", denied.Status);
    }
}
