using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace JerneIF.Tests;

public class AuthorizationAttributesTests
{
    private static TAttr? GetAttr<TAttr>(MethodInfo mi) where TAttr : Attribute
        => mi.GetCustomAttributes(typeof(TAttr), inherit: true).Cast<TAttr>().FirstOrDefault();

    private static AuthorizeAttribute? GetAuthorize(MethodInfo mi)
        => mi.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>().FirstOrDefault();

    [Fact]
    public void PlayersController_Authorization_Is_Configured()
    {
        var t = Type.GetType("api.Controllers.PlayersController, api")!;
        Assert.NotNull(t);

        var getAll = t.GetMethod("GetPlayers")!;
        Assert.NotNull(GetAttr<AllowAnonymousAttribute>(getAll));

        var getById = t.GetMethod("GetPlayerById")!;
        Assert.NotNull(GetAttr<AllowAnonymousAttribute>(getById));

        var create = t.GetMethod("CreatePlayer")!;
        var aCreate = GetAuthorize(create);
        Assert.NotNull(aCreate);
        Assert.Equal("Admin", aCreate!.Roles);

        var createBoards = t.GetMethod("CreateBoards")!;
        Assert.NotNull(GetAuthorize(createBoards));

        var update = t.GetMethod("UpdatePlayer")!;
        Assert.NotNull(GetAuthorize(update));

        var changePwd = t.GetMethod("ChangePassword")!;
        var aChange = GetAuthorize(changePwd);
        Assert.NotNull(aChange);
        Assert.Equal("User", aChange!.Roles);

        var soft = t.GetMethod("SoftDelete")!;
        var aSoft = GetAuthorize(soft);
        Assert.NotNull(aSoft);
        Assert.Equal("Admin", aSoft!.Roles);

        var restore = t.GetMethod("Restore")!;
        var aRest = GetAuthorize(restore);
        Assert.NotNull(aRest);
        Assert.Equal("Admin", aRest!.Roles);
    }

    [Fact]
    public void FundRequestsController_Authorization_Is_Configured()
    {
        var t = Type.GetType("api.Controllers.FundRequestsController, api")!;
        Assert.NotNull(t);

        var create = t.GetMethod("Create")!;
        var aCreate = GetAuthorize(create);
        Assert.NotNull(aCreate);
        Assert.Equal("User", aCreate!.Roles);

        var list = t.GetMethod("List")!;
        var aList = GetAuthorize(list);
        Assert.NotNull(aList);
        Assert.Equal("Admin", aList!.Roles);

        var approve = t.GetMethod("Approve")!;
        Assert.Equal("Admin", GetAuthorize(approve)!.Roles);

        var deny = t.GetMethod("Deny")!;
        Assert.Equal("Admin", GetAuthorize(deny)!.Roles);
    }

    [Fact]
    public void BoardsController_Authorization_Is_Configured()
    {
        var t = Type.GetType("api.Controllers.BoardsController, api")!;
        Assert.NotNull(t);

        var draw = t.GetMethod("Draw")!;
        Assert.Equal("Admin", GetAuthorize(draw)!.Roles);

        var activate = t.GetMethod("Activate")!;
        Assert.Equal("Admin", GetAuthorize(activate)!.Roles);

        var deactivate = t.GetMethod("Deactivate")!;
        Assert.Equal("Admin", GetAuthorize(deactivate)!.Roles);
    }

    [Fact]
    public void AdminsController_Authorization_Is_Configured()
    {
        var t = Type.GetType("api.Controllers.AdminsController, api")!;
        Assert.NotNull(t);

        var soft = t.GetMethod("SoftDelete")!;
        Assert.Equal("Admin", GetAuthorize(soft)!.Roles);

        var restore = t.GetMethod("Restore")!;
        Assert.Equal("Admin", GetAuthorize(restore)!.Roles);
    }
}
