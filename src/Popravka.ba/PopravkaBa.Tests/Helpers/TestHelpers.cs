using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Popravka.ba.Data;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.Helpers;

public static class TestHelpers
{
    // Instancira novu praznu baza za svaki test (zato su testovi nezavisni)
    public static ApplicationDbContext NovaBaza() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    public static Mock<UserManager<ApplicationUser>> MockUserManager() =>
        new(Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

    public static void PrijaviKorisnika(Controller kontroler, string id, params string[] uloge)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, id), new(ClaimTypes.Name, id) };
        claims.AddRange(uloge.Select(u => new Claim(ClaimTypes.Role, u)));
        Postavi(kontroler, new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")));
    }

    public static void NeprijavljenKorisnik(Controller kontroler) =>
        Postavi(kontroler, new ClaimsPrincipal(new ClaimsIdentity()));

    private static void Postavi(Controller kontroler, ClaimsPrincipal korisnik)
    {
        var http = new DefaultHttpContext { User = korisnik };
        kontroler.ControllerContext = new ControllerContext { HttpContext = http };
        kontroler.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        kontroler.Url = Mock.Of<IUrlHelper>();
    }
}