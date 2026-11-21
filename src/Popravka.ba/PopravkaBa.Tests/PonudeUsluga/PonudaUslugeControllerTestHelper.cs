using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Interface;
using PopravkaBa.Domain.Models;
using PopravkaBa.Web.Controllers;

namespace PopravkaBa.Tests.PonudeUsluga;

internal static class PonudaUslugeControllerTestHelper
{
    internal const int OglasId = 10;

    internal static KreirajPonudaUslugeDto NovaPonuda(int? pomakDana = null)
    {
        var pocetak = new DateTime(2026, 11, 22);
        return new KreirajPonudaUslugeDto
        {
            OglasUslugeID = OglasId,
            DatumPocetkaUsluge = pocetak,
            DatumOcekivanogZavrsetka = pomakDana.HasValue
                ? pocetak.AddDays(pomakDana.Value)
                : null,
            Cijena = 100,
            Poruka = "Mogu preuzeti posao."
        };
    }

    internal static Task IshodServisa(bool uspjeh) =>
        uspjeh
            ? Task.CompletedTask
            : Task.FromException(new InvalidOperationException("Obrada nije uspjela."));

    internal static void ProvjeriDetaljeOglasa(IActionResult rezultat)
    {
        var preusmjerenje = (RedirectToActionResult)rezultat;
        Assert.AreEqual("Detalji", preusmjerenje.ActionName);
        Assert.AreEqual("OglasUsluge", preusmjerenje.ControllerName);
        Assert.IsNotNull(preusmjerenje.RouteValues);
        Assert.AreEqual(OglasId, (int)preusmjerenje.RouteValues["id"]!);
    }
}
