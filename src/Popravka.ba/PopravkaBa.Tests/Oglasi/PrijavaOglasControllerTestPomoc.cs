using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Interface;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.Oglasi;

internal static class PrijavaOglasControllerTestPomoc
{
    internal const int OglasId = 10;
    internal const int PrijavaId = 5;
    internal const string MajstorId = "m1";

    internal static PrijavaRadnoMjesto PostaviPrijavu(Mock<IPrijavaOglasService> servis)
    {
        var prijava = NovaPrijava();
        servis.Setup(s => s.DajPrijavuPoId(PrijavaId)).ReturnsAsync(prijava);
        return prijava;
    }

    internal static void PostaviNepostojecuPrijavu(Mock<IPrijavaOglasService> servis)
    {
        servis.Setup(s => s.DajPrijavuPoId(PrijavaId))
            .ReturnsAsync((PrijavaRadnoMjesto?)null);
    }

    internal static PrijavaRadnoMjesto NovaPrijava() => new()
    {
        ID = PrijavaId,
        OglasID = OglasId,
        MajstorID = MajstorId
    };

    internal static Task<IActionResult> PozoviGetAkciju(PrijavaOglasController kontroler, string akcija) => akcija switch
    {
        nameof(PrijavaOglasController.ObrisiPrijavu) => kontroler.ObrisiPrijavu(PrijavaId),
        nameof(PrijavaOglasController.PrihvatiPrijava) => kontroler.PrihvatiPrijava(PrijavaId),
        nameof(PrijavaOglasController.OdbijPrijavu) => kontroler.OdbijPrijavu(PrijavaId),
        _ => throw new ArgumentOutOfRangeException(nameof(akcija))
    };

    internal static Task<IActionResult> PozoviPrihvatanje(PrijavaOglasController kontroler, bool direktno) => direktno
        ? kontroler.PrihvacenaPrijavaDirektno(PrijavaId)
        : kontroler.PrihvacenaPrijava(PrijavaId);

    internal static void ProvjeriDetaljeOglasa(IActionResult rezultat, int ocekivaniId)
    {
        var preusmjerenje = (RedirectToActionResult)rezultat;
        Assert.AreEqual("Detalji", preusmjerenje.ActionName);
        Assert.AreEqual("OglasRadnoMjesto", preusmjerenje.ControllerName);
        Assert.IsNotNull(preusmjerenje.RouteValues);
        Assert.AreEqual(ocekivaniId, (int)preusmjerenje.RouteValues["id"]!);
    }
}
