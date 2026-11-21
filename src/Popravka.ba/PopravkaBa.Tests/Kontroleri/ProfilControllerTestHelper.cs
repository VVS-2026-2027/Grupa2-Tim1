using System.Security.Claims;
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
using PopravkaBa.Web.Models.ViewModels;

namespace PopravkaBa.Tests.Profil;

// Pomoćne metode koje samo pripremaju podatke za testove
internal static class ProfilControllerTestHelper
{
    internal static Klijent NoviKlijent() => new()
    {
        Id = "k1", UserName = "haso", Ime = "Haso", Prezime = "Hasic",
        Email = "haso@popravka.ba", EmailConfirmed = true
    };

    internal static Majstor NoviMajstor() => new()
    {
        Id = "m1", UserName = "majstor1", Ime = "Mujo", Prezime = "Mujic",
        Email = "mujo@popravka.ba", EmailConfirmed = true
    };

    internal static Firma NovaFirma() => new()
    {
        Id = "f1", UserName = "elektro", Email = "firma@popravka.ba", EmailConfirmed = true,
        AdminVerificirao = true, NazivFirme = "Elektro Servis", VelicinaFirme = VelicinaFirme.Mala,
        OtvorenoOd = TimeSpan.FromHours(8), OtvorenoDo = TimeSpan.FromHours(16)
    };

    internal static ProfilEditViewModel NoviVm(string userId, string username, string email) => new()
    {
        UserId = userId, Username = username, Ime = "Novo", Prezime = "Prezime", Email = email,
        SelectedMjestaId = new List<int> { 1 }, SelectedKategorijeId = new List<int> { 1 }
    };

    internal static IFormFile Fajl(long velicina = 100)
    {
        var fajl = new Mock<IFormFile>();
        fajl.Setup(f => f.Length).Returns(velicina);
        fajl.Setup(f => f.ContentType).Returns("image/png");
        fajl.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream());
        return fajl.Object;
    }

    internal static List<IFormFile> Slike(int broj) =>
        Enumerable.Range(0, broj).Select(_ => Fajl()).ToList();

    internal static RadnoVrijemeDto Radno(int? od, int? doSati) => new()
    {
        OtvorenoOd = od is null ? null : TimeSpan.FromHours(od.Value),
        OtvorenoDo = doSati is null ? null : TimeSpan.FromHours(doSati.Value)
    };

    internal static IdentityResult Greske(params string[] opisi) =>
        IdentityResult.Failed(opisi.Select(o => new IdentityError { Description = o }).ToArray());

    internal static OglasUsluge NoviOglasUsluge(int id, string vlasnikId, Status status, DateTime datum) => new()
    {
        OglasID = id, Naslov = "Oglas " + id, VlasnikOglasaID = vlasnikId,
        StatusOglasa = status, DatumObjave = datum
    };

    internal static OglasMajstora NoviOglasMajstora(int id, string vlasnikId, Status status) => new()
    {
        OglasID = id, Naslov = "Majstor oglas " + id, Opis = "Opis oglasa", VlasnikOglasaID = vlasnikId,
        StatusOglasa = status, MinCijena = 50, TipIsplate = TipIsplate.PoSatu,
        Mjesto = new Mjesto { Naziv = "Tuzla" },
        Kategorije = new List<OglasKategorija>
        {
            new() { Kategorija = new Kategorija { Naziv = "Vodoinstalater" } },
            new() { Kategorija = null! }
        }
    };

    internal static Majstor MajstorSaPunimProfilom()
    {
        var majstor = NoviMajstor();
        majstor.Slika = "https://cdn.popravka.ba/slike/mujo.png";
        majstor.Opis = "Iskusan majstor";
        majstor.ProsjecnaOcjena = 4.5m;
        majstor.BrojZavrsenihPoslova = 12;
        majstor.Kategorije = new List<IzvrsilacKategorija>
        {
            new() { KategorijaID = 1, Kategorija = new Kategorija { ID = 1, Naziv = "Vodoinstalater" } },
            new() { KategorijaID = 2, Kategorija = new Kategorija { ID = 2, Naziv = "Električar" } },
            new() { KategorijaID = 3 }
        };
        majstor.Mjesta = new List<KorisnikMjesto>
        {
            new() { MjestoID = 4, Mjesto = new Mjesto { MjestoID = 4, Naziv = "Sarajevo" } },
            new() { MjestoID = 5, Mjesto = new Mjesto { MjestoID = 5, Naziv = "Tuzla" } }
        };
        majstor.SlikePortfolija = new List<PortfolioSlika>
        {
            new() { PortfolioSlikaID = 10, URL = "https://cdn.popravka.ba/slike/1.png", Opis = "Kupatilo" }
        };
        majstor.Recenzije = new List<Recenzija>
        {
            new()
            {
                RecenzijaID = 1, Ocjena = 5, Komentar = "Odlično", DatumRecenzije = new DateTime(2026, 1, 1),
                Klijent = new Klijent { Ime = "Haso", Prezime = "Hasic" }
            },
            new() { RecenzijaID = 2, Ocjena = 4, Komentar = "Dobro", DatumRecenzije = new DateTime(2026, 2, 1), Prijavljena = true }
        };
        return majstor;
    }

    // Prijavljuje korisnika na kontroleru i postavlja GetUserId na njegov id
    internal static void Prijavi(ProfilController kontroler, Mock<UserManager<ApplicationUser>> userManager, string id, params string[] uloge)
    {
        TestHelpers.PrijaviKorisnika(kontroler, id, uloge);
        userManager.PrijavljenId(id);
    }

    // Profil se otvara po korisničkom imenu i korisnik ima date uloge
    internal static void PostaviProfilPoUsernameu(this Mock<UserManager<ApplicationUser>> userManager, ApplicationUser korisnik, params string[] uloge)
    {
        userManager.PronadjiPoUsername(korisnik);
        userManager.VratiUloge(korisnik, uloge);
    }

    // Prijavljeni korisnik uređuje vlastiti profil
    internal static void PripremiVlastitiProfil(ProfilController kontroler, Mock<UserManager<ApplicationUser>> userManager, ApplicationUser korisnik, string uloga)
    {
        Prijavi(kontroler, userManager, korisnik.Id, uloga);
        userManager.PronadjiPoId(korisnik, korisnik.Id);
        userManager.VratiUloge(korisnik, uloga);
    }
}
