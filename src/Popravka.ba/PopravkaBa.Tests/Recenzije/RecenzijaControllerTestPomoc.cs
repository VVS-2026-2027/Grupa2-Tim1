using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Interface;
using PopravkaBa.Domain.Models;
using PopravkaBa.Web.Controllers;

namespace PopravkaBa.Tests.Recenzije;

internal static class RecenzijaControllerTestPomoc
{
    internal const string IzvrsilacId = "m1";
    internal const string KlijentId = "k1";
    internal const string KorisnickoIme = "majstor1";
    internal const int RecenzijaId = 5;

    internal static Majstor PostaviIzvrsioca(Mock<UserManager<ApplicationUser>> userManager)
    {
        var izvrsilac = new Majstor
        {
            Id = IzvrsilacId,
            UserName = KorisnickoIme,
            Ime = "Amar",
            Prezime = "Kovač"
        };
        userManager.Setup(m => m.FindByIdAsync(IzvrsilacId)).ReturnsAsync(izvrsilac);
        return izvrsilac;
    }

    internal static Recenzija PostaviRecenziju(
        Mock<IRecenzijaService> servis, string? korisnickoIme = KorisnickoIme)
    {
        var recenzija = NovaRecenzija();
        recenzija.Izvrsilac = korisnickoIme is null
            ? null!
            : new Majstor { Id = IzvrsilacId, UserName = korisnickoIme };
        servis.Setup(s => s.DajRecenzijuPoId(RecenzijaId)).ReturnsAsync(recenzija);
        return recenzija;
    }

    internal static Recenzija NovaRecenzija() => new()
    {
        RecenzijaID = RecenzijaId,
        IzvrsilacID = IzvrsilacId,
        KlijentID = KlijentId
    };

    internal static KreirajRecenzijuDto NovaObjava() => new()
    {
        IzvrsilacID = IzvrsilacId,
        Ocjena = 5,
        Komentar = "Odlična usluga"
    };

    internal static PrijaviRecenzijuDto NovaPrijava() => new()
    {
        RecenzijaID = RecenzijaId,
        RazlogPrijave = "Neprimjeren sadržaj recenzije"
    };

    internal static Exception GreskaServisa(bool poslovnaGreska, string poruka) =>
        poslovnaGreska ? new InvalidOperationException(poruka) : new Exception("Neočekivana greška");

    internal static void ProvjeriPreusmjerenje(
        IActionResult rezultat, string kontroler, string? korisnickoIme)
    {
        var preusmjerenje = (RedirectToActionResult)rezultat;
        Assert.AreEqual("Index", preusmjerenje.ActionName);
        
        Assert.AreEqual(kontroler, preusmjerenje.ControllerName);
        var stvarnoKorisnickoIme = preusmjerenje.RouteValues?.GetValueOrDefault("username");
        Assert.AreEqual(korisnickoIme, stvarnoKorisnickoIme);
    }
}
