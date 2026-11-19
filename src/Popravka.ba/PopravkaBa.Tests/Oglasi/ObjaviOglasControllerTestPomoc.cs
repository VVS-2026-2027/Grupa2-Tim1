using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Interface;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using PopravkaBa.Web.Controllers;

namespace PopravkaBa.Tests.Oglasi;

internal static class ObjaviOglasControllerTestPomoc
{
    internal const int OglasId = 42;

    internal static void ProvjeriListe(ObjaviOglasController kontroler, Kategorija[] kategorije, Mjesto[] mjesta)
    {
        Assert.AreSame(kategorije, kontroler.ViewData["Kategorije"]);
        Assert.AreSame(mjesta, kontroler.ViewData["Mjesta"]);
    }

    internal static void ProvjeriObrazac(
        ObjaviOglasController kontroler, Kategorija[] kategorije, Mjesto[] mjesta, IActionResult rezultat, object model, string tab)
    {
        var pogled = (ViewResult)rezultat;
        Assert.AreEqual("Index", pogled.ViewName);
        Assert.AreSame(model, pogled.Model);
        Assert.AreEqual(tab, kontroler.ViewData["AktivniTab"]);
        ProvjeriListe(kontroler, kategorije, mjesta);
    }

    internal static void ProvjeriDetalje(IActionResult rezultat, string kontroler)
    {
        var preusmjerenje = (RedirectToActionResult)rezultat;
        Assert.AreEqual("Detalji", preusmjerenje.ActionName);
        Assert.AreEqual(kontroler, preusmjerenje.ControllerName);
        Assert.IsNotNull(preusmjerenje.RouteValues);
        Assert.AreEqual(OglasId, (int)preusmjerenje.RouteValues["id"]!);
    }
}
