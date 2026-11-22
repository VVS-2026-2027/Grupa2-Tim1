using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.Oglasi;

// Zajedničke pomoćne metode za testove OglasMajstora, OglasRadnoMjesto i OglasUsluge kontrolera
internal static class OglasControllerTestHelper
{
    internal const string NovaSlikaUrl = "https://example.test/nova.png";

    // GetUserId(User) vraća id iz claima prijavljenog korisnika, pa test mijenja korisnika preko PrijaviKorisnika
    internal static void KorisnikIzClaimova(this Mock<UserManager<ApplicationUser>> userManager) =>
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns((ClaimsPrincipal korisnik) => korisnik.FindFirst(ClaimTypes.NameIdentifier)?.Value);

    internal static IFormFile Fajl(string ime = "slika.png", long velicina = 100)
    {
        var fajl = new Mock<IFormFile>();
        fajl.Setup(f => f.FileName).Returns(ime);
        fajl.Setup(f => f.Length).Returns(velicina);
        fajl.Setup(f => f.ContentType).Returns("image/png");
        fajl.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream());
        return fajl.Object;
    }

    internal static void ProvjeriPreusmjerenjeNaDetalje(IActionResult rezultat, int id)
    {
        var preusmjerenje = Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        Assert.AreEqual("Detalji", preusmjerenje.ActionName);
        Assert.AreEqual(id, preusmjerenje.RouteValues!["id"]);
    }

    internal static void ProvjeriPreusmjerenjeNaIndex(IActionResult rezultat)
    {
        var preusmjerenje = Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        Assert.AreEqual("Index", preusmjerenje.ActionName);
    }

    // Pogled sa istim modelom koji je poslan (forma se vraća korisniku sa greškama)
    internal static void ProvjeriPogledSaModelom(IActionResult rezultat, object model)
    {
        var pogled = Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.AreSame(model, pogled.Model);
    }
}
