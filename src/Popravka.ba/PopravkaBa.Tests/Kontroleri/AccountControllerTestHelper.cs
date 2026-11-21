using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Interface;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using PopravkaBa.Web.Controllers;
using PopravkaBa.Web.Models.Enums;
using PopravkaBa.Web.Models.ViewModels;

namespace PopravkaBa.Tests.Account;

// Pomoćne metode koje samo pripremaju podatke za testove
internal static class AccountControllerTestHelper
{
    internal static RegistracijaViewModel PrijavaVm(string unos = "haso", string lozinka = "Lozinka1") =>
        new() { Login = new LoginDto { EmailUsername = unos, Lozinka = lozinka } };

    internal static RegistracijaKlijentaDto KlijentDto() => new()
    {
        KorisnickoIme = "haso", Ime = "Haso", Prezime = "Hasić", Email = "haso@popravka.ba",
        Lozinka = "Lozinka1", PotvrdaLozinke = "Lozinka1", MjestaID = new List<int> { 1 }
    };

    internal static RegistracijaMajstoraDto MajstorDto() => new()
    {
        KorisnickoIme = "mujo", Ime = "Mujo", Prezime = "Mujić", Email = "mujo@popravka.ba",
        Lozinka = "Lozinka1", PotvrdaLozinke = "Lozinka1",
        KategorijeID = new List<int> { 1 }, MjestaID = new List<int> { 1 }
    };

    internal static RegistracijaFirmeDto FirmaDto() => new()
    {
        KorisnickoIme = "elektro", NazivFirme = "Elektro Servis", Email = "firma@popravka.ba",
        Lozinka = "Lozinka1", PotvrdaLozinke = "Lozinka1", VelicinaFirme = VelicinaFirme.Mala,
        KategorijeID = new List<int> { 2 }, MjestaID = new List<int> { 3 }
    };

    internal static Mock<IFormFile> Logo(string imeFajla, long velicina)
    {
        var fajl = new Mock<IFormFile>();
        fajl.Setup(f => f.FileName).Returns(imeFajla);
        fajl.Setup(f => f.Length).Returns(velicina);
        fajl.Setup(f => f.ContentType).Returns("image/png");
        fajl.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream());
        return fajl;
    }

    internal static IdentityResult Greske(params string[] opisi) =>
        IdentityResult.Failed(opisi.Select(o => new IdentityError { Description = o }).ToArray());

    internal static (Status, VerifikacijskiToken?, ApplicationUser?) Validacija(
        Status status, VerifikacijskiToken? token = null, ApplicationUser? korisnik = null) => (status, token, korisnik);

    internal static ApplicationUser AktivanKorisnik()
    {
        var korisnik = new Klijent { Id = "k1", Email = "haso@popravka.ba", EmailConfirmed = true };
        korisnik.OsvjeziStatusAktivnosti();
        return korisnik;
    }

    internal static ResetLozinkeViewModel ResetVm() =>
        new() { Token = "tok", NovaLozinka = "NovaLozinka1", PotvrdaLozinke = "NovaLozinka1" };
}
