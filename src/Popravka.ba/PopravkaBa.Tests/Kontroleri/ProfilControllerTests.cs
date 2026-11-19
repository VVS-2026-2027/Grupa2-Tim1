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

[TestClass]
public class ProfilControllerTests
{
    private const string UrlSlike = "https://cdn.popravka.ba/slike/nova.png";

    private Mock<UserManager<ApplicationUser>> _userManager = null!;
    private Mock<SignInManager<ApplicationUser>> _signInManager = null!;
    private Mock<IOglasMajstoraFacade> _oglasMajstoraFacade = null!;
    private Mock<IOglasUslugeFacade> _oglasUslugeFacade = null!;
    private Mock<IIzvrsilacUslugeService> _izvrsilacService = null!;
    private Mock<IFileStorage> _storage = null!;
    private Mock<ILogger<ProfilController>> _logger = null!;
    private Mock<IMjestoService> _mjestoService = null!;
    private Mock<IKategorijaService> _kategorijaService = null!;
    private Mock<IRecenzijaService> _recenzijaService = null!;
    private ProfilController _kontroler = null!;

    [TestInitialize]
    public void Postavi()
    {
        _userManager = TestHelpers.MockUserManager();
        _signInManager = new Mock<SignInManager<ApplicationUser>>(
            _userManager.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            null!, null!, null!, null!);
        _oglasMajstoraFacade = new Mock<IOglasMajstoraFacade>();
        _oglasUslugeFacade = new Mock<IOglasUslugeFacade>();
        _izvrsilacService = new Mock<IIzvrsilacUslugeService>();
        _storage = new Mock<IFileStorage>();
        _logger = new Mock<ILogger<ProfilController>>();
        _mjestoService = new Mock<IMjestoService>();
        _kategorijaService = new Mock<IKategorijaService>();
        _recenzijaService = new Mock<IRecenzijaService>();

        _oglasMajstoraFacade.Setup(f => f.DajSveOglase()).ReturnsAsync(new List<OglasMajstora>());
        _oglasUslugeFacade.Setup(f => f.DajSveOglase()).ReturnsAsync(new List<OglasUsluge>());
        _mjestoService.Setup(m => m.DajSvaMjestaAsync()).ReturnsAsync(new List<Mjesto> { new() { MjestoID = 1, Naziv = "Sarajevo" } });
        _kategorijaService.Setup(k => k.DajSveKategorije()).ReturnsAsync(new List<Kategorija> { new() { ID = 1, Naziv = "Vodoinstalater" } });
        _userManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.SetUserNameAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        _storage.Setup(s => s.SpremiPublic(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UrlSlike);

        _kontroler = new ProfilController(
            _userManager.Object,
            _signInManager.Object,
            _oglasMajstoraFacade.Object,
            _oglasUslugeFacade.Object,
            _izvrsilacService.Object,
            _storage.Object,
            _logger.Object,
            _mjestoService.Object,
            _kategorijaService.Object,
            _recenzijaService.Object);
        TestHelpers.NeprijavljenKorisnik(_kontroler);
    }

    // ---------- Pomoćne metode (priprema podataka) ----------

    private void Prijavi(string id, params string[] uloge)
    {
        TestHelpers.PrijaviKorisnika(_kontroler, id, uloge);
        _userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(id);
    }

    private void PostaviUloge(ApplicationUser korisnik, params string[] uloge)
    {
        _userManager.Setup(m => m.GetRolesAsync(korisnik)).ReturnsAsync(uloge.ToList());
    }

    private void PostaviProfilPoUsernameu(ApplicationUser korisnik, params string[] uloge)
    {
        _userManager.Setup(m => m.FindByNameAsync(korisnik.UserName!)).ReturnsAsync(korisnik);
        PostaviUloge(korisnik, uloge);
    }

    // Prijavljeni korisnik uređuje vlastiti profil
    private void PripremiVlastitiProfil(ApplicationUser korisnik, string uloga)
    {
        Prijavi(korisnik.Id, uloga);
        _userManager.Setup(m => m.FindByIdAsync(korisnik.Id)).ReturnsAsync(korisnik);
        PostaviUloge(korisnik, uloga);
    }

    private static Klijent NoviKlijent() => new()
    {
        Id = "k1", UserName = "haso", Ime = "Haso", Prezime = "Hasic",
        Email = "haso@popravka.ba", EmailConfirmed = true
    };

    private static Majstor NoviMajstor() => new()
    {
        Id = "m1", UserName = "majstor1", Ime = "Mujo", Prezime = "Mujic",
        Email = "mujo@popravka.ba", EmailConfirmed = true
    };

    private static Firma NovaFirma() => new()
    {
        Id = "f1", UserName = "elektro", Email = "firma@popravka.ba", EmailConfirmed = true,
        AdminVerificirao = true, NazivFirme = "Elektro Servis", VelicinaFirme = VelicinaFirme.Mala,
        OtvorenoOd = TimeSpan.FromHours(8), OtvorenoDo = TimeSpan.FromHours(16)
    };

    private static ProfilEditViewModel NoviVm(string userId, string username, string email) => new()
    {
        UserId = userId, Username = username, Ime = "Novo", Prezime = "Prezime", Email = email,
        SelectedMjestaId = new List<int> { 1 }, SelectedKategorijeId = new List<int> { 1 }
    };

    private static IFormFile Fajl(long velicina = 100)
    {
        var fajl = new Mock<IFormFile>();
        fajl.Setup(f => f.Length).Returns(velicina);
        fajl.Setup(f => f.ContentType).Returns("image/png");
        fajl.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream());
        return fajl.Object;
    }

    private static List<IFormFile> Slike(int broj) =>
        Enumerable.Range(0, broj).Select(_ => Fajl()).ToList();

    private static RadnoVrijemeDto Radno(int? od, int? doSati) => new()
    {
        OtvorenoOd = od is null ? null : TimeSpan.FromHours(od.Value),
        OtvorenoDo = doSati is null ? null : TimeSpan.FromHours(doSati.Value)
    };

    private static IdentityResult Greske(params string[] opisi) =>
        IdentityResult.Failed(opisi.Select(o => new IdentityError { Description = o }).ToArray());

    private List<string> PorukeGresaka() =>
        _kontroler.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

    private static OglasUsluge NoviOglasUsluge(int id, string vlasnikId, Status status, DateTime datum) => new()
    {
        OglasID = id, Naslov = "Oglas " + id, VlasnikOglasaID = vlasnikId,
        StatusOglasa = status, DatumObjave = datum
    };

    private static OglasMajstora NoviOglasMajstora(int id, string vlasnikId, Status status) => new()
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

    private static Majstor MajstorSaPunimProfilom()
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

    // ---------- Index ----------

    public static IEnumerable<object[]> NevalidniUsernamei =>
        DataDrivenInputParser.CitajXml<string>("TestData/xml/ProfilController_nevalidniUsername.xml");

    [TestMethod]
    [DynamicData(nameof(NevalidniUsernamei))]
    public async Task Index_PrazanUsernameIzXml_VracaNotFoundBezTrazenjaKorisnika(string? username)
    {
        // Act
        var rezultat = await _kontroler.Index(username!);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        _userManager.Verify(m => m.FindByNameAsync(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Index_NepostojeciKorisnik_VracaNotFound()
    {
        // Arrange
        _userManager.Setup(m => m.FindByNameAsync("nepoznat")).ReturnsAsync((ApplicationUser?)null);

        // Act
        var rezultat = await _kontroler.Index("nepoznat");

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task Index_Administrator_VracaNotFoundJerAdminNemaJavniProfil()
    {
        // Arrange
        var admin = new Administrator { Id = "a1", UserName = "admin" };
        PostaviProfilPoUsernameu(admin, "Administrator");

        // Act
        var rezultat = await _kontroler.Index("admin");

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task Index_KorisnikBezUloge_VracaViewSaUlogomKorisnik()
    {
        // Arrange
        var klijent = NoviKlijent();
        PostaviProfilPoUsernameu(klijent);

        // Act
        var rezultat = await _kontroler.Index("haso");

        // Assert
        var view = Assert.IsInstanceOfType<ViewResult>(rezultat);
        var model = Assert.IsInstanceOfType<ProfilViewModel>(view.Model);
        Assert.AreEqual("Korisnik", model.Uloga);
        Assert.AreEqual(0, model.OglasiUsluge.Count);
    }

    [TestMethod]
    public async Task Index_Klijent_VracaOsnovnePodatkeProfila()
    {
        // Arrange
        var klijent = NoviKlijent();
        klijent.Slika = "https://cdn.popravka.ba/slike/haso.png";
        PostaviProfilPoUsernameu(klijent, "Klijent");

        // Act
        var rezultat = await _kontroler.Index("haso");

        // Assert
        var model = Assert.IsInstanceOfType<ProfilViewModel>(Assert.IsInstanceOfType<ViewResult>(rezultat).Model);
        Assert.AreEqual("k1", model.UserId);
        Assert.AreEqual("haso", model.Username);
        Assert.AreEqual("Haso Hasic", model.DisplayName);
        Assert.AreEqual("Klijent", model.Uloga);
        Assert.AreEqual("haso@popravka.ba", model.Email);
        Assert.AreEqual("https://cdn.popravka.ba/slike/haso.png", model.Slika);
        Assert.IsTrue(model.JeVerificiranEmail);
        Assert.IsTrue(model.JeVerificiran);
        Assert.IsFalse(model.JeVlasnik);
    }

    [TestMethod]
    public async Task Index_Klijent_VracaSamoOglaseVlasnikaOdNajnovijeg()
    {
        // Arrange
        var klijent = NoviKlijent();
        PostaviProfilPoUsernameu(klijent, "Klijent");
        _oglasUslugeFacade.Setup(f => f.DajSveOglase()).ReturnsAsync(new List<OglasUsluge>
        {
            NoviOglasUsluge(1, "k1", Status.Aktivan, new DateTime(2026, 1, 1)),
            NoviOglasUsluge(2, "k1", Status.Neaktivan, new DateTime(2026, 3, 1)),
            NoviOglasUsluge(3, "k2", Status.Aktivan, new DateTime(2026, 2, 1))
        });

        // Act
        var rezultat = await _kontroler.Index("haso");

        // Assert
        var model = Assert.IsInstanceOfType<ProfilViewModel>(Assert.IsInstanceOfType<ViewResult>(rezultat).Model);
        Assert.AreEqual(2, model.OglasiUsluge.Count);
        Assert.AreEqual(2, model.OglasiUsluge[0].OglasId);
        Assert.AreEqual(1, model.OglasiUsluge[1].OglasId);
        Assert.AreEqual(1, model.BrojAktivnihOglasa);
        Assert.AreEqual(1, model.BrojZatvorenihOglasa);
    }

    [TestMethod]
    public async Task Index_KlijentSaPrihvacenomPonudom_OznacavaIzvrsiocaIPonude()
    {
        // Arrange
        var klijent = NoviKlijent();
        PostaviProfilPoUsernameu(klijent, "Klijent");
        var oglas = NoviOglasUsluge(1, "k1", Status.Aktivan, new DateTime(2026, 1, 1));
        oglas.Mjesto = new Mjesto { Naziv = "Sarajevo" };
        oglas.Kategorije = new List<OglasKategorija>
        {
            new() { Kategorija = new Kategorija { Naziv = "Vodoinstalater" } },
            new() { Kategorija = null! }
        };
        oglas.Ponude = new List<PonudaUsluge>
        {
            new() { IzvrsilacID = "m1", Izvrsilac = NoviMajstor(), StatusPonude = Status.Prihvaceno },
            new() { IzvrsilacID = "m2", Izvrsilac = new Majstor { Id = "m2", UserName = "majstor2" }, StatusPonude = Status.NaCekanju }
        };
        _oglasUslugeFacade.Setup(f => f.DajSveOglase()).ReturnsAsync(new List<OglasUsluge> { oglas });

        // Act
        var rezultat = await _kontroler.Index("haso");

        // Assert
        var model = Assert.IsInstanceOfType<ProfilViewModel>(Assert.IsInstanceOfType<ViewResult>(rezultat).Model);
        var stavka = model.OglasiUsluge.Single();
        Assert.IsTrue(stavka.ImaPrihvacenuPonudu);
        Assert.AreEqual("majstor1", stavka.IzvrsilacUsername);
        Assert.AreEqual(2, stavka.BrojPonuda);
        Assert.AreEqual("Sarajevo", stavka.Lokacija);
        CollectionAssert.AreEqual(new[] { "Vodoinstalater" }, stavka.Kategorije);
    }

    [TestMethod]
    public async Task Index_KlijentBezPonudaIKategorija_VracaPraznuListuKategorijaIBezIzvrsioca()
    {
        // Arrange
        var klijent = NoviKlijent();
        PostaviProfilPoUsernameu(klijent, "Klijent");
        _oglasUslugeFacade.Setup(f => f.DajSveOglase()).ReturnsAsync(new List<OglasUsluge>
        {
            NoviOglasUsluge(1, "k1", Status.Aktivan, new DateTime(2026, 1, 1))
        });

        // Act
        var rezultat = await _kontroler.Index("haso");

        // Assert
        var model = Assert.IsInstanceOfType<ProfilViewModel>(Assert.IsInstanceOfType<ViewResult>(rezultat).Model);
        var stavka = model.OglasiUsluge.Single();
        Assert.IsFalse(stavka.ImaPrihvacenuPonudu);
        Assert.IsNull(stavka.IzvrsilacUsername);
        Assert.AreEqual(0, stavka.Kategorije.Count);
    }

    public static IEnumerable<object[]> OcjenjivanjeIzvrsioca =>
        DataDrivenInputParser.CitajXml<bool, bool>("TestData/xml/ProfilController_ocjenjivanjeIzvrsioca.xml");

    [TestMethod]
    [DynamicData(nameof(OcjenjivanjeIzvrsioca))]
    public async Task Index_KlijentVlasnikSaZavrsenimPoslomIzXml_ProvjeravaDaLiJeIzvrsilacVecOcijenjen(
        bool mozeOstavitiRecenziju, bool vecOcijenjen)
    {
        // Arrange
        var klijent = NoviKlijent();
        Prijavi("k1", "Klijent");
        PostaviProfilPoUsernameu(klijent, "Klijent");
        var oglas = NoviOglasUsluge(1, "k1", Status.Isporuceno, new DateTime(2026, 1, 1));
        oglas.Ponude = new List<PonudaUsluge>
        {
            new() { IzvrsilacID = "m1", Izvrsilac = NoviMajstor(), StatusPonude = Status.Isporuceno }
        };
        _oglasUslugeFacade.Setup(f => f.DajSveOglase()).ReturnsAsync(new List<OglasUsluge> { oglas });
        _recenzijaService.Setup(r => r.MozeOstavitiRecenziju("k1", "m1")).ReturnsAsync(mozeOstavitiRecenziju);

        // Act
        var rezultat = await _kontroler.Index("haso");

        // Assert
        var model = Assert.IsInstanceOfType<ProfilViewModel>(Assert.IsInstanceOfType<ViewResult>(rezultat).Model);
        Assert.IsTrue(model.JeVlasnik);
        var stavka = model.OglasiUsluge.Single();
        Assert.IsFalse(stavka.ImaPrihvacenuPonudu);
        Assert.AreEqual(vecOcijenjen, stavka.VecOcijenjen);
        _recenzijaService.Verify(r => r.MozeOstavitiRecenziju("k1", "m1"), Times.Once);
    }

    [TestMethod]
    public async Task Index_KlijentNijeVlasnikProfila_NeProvjeravaRecenzije()
    {
        // Arrange
        var klijent = NoviKlijent();
        Prijavi("k9", "Klijent");
        PostaviProfilPoUsernameu(klijent, "Klijent");
        var oglas = NoviOglasUsluge(1, "k1", Status.Isporuceno, new DateTime(2026, 1, 1));
        oglas.Ponude = new List<PonudaUsluge>
        {
            new() { IzvrsilacID = "m1", Izvrsilac = NoviMajstor(), StatusPonude = Status.Isporuceno }
        };
        _oglasUslugeFacade.Setup(f => f.DajSveOglase()).ReturnsAsync(new List<OglasUsluge> { oglas });

        // Act
        var rezultat = await _kontroler.Index("haso");

        // Assert
        var model = Assert.IsInstanceOfType<ProfilViewModel>(Assert.IsInstanceOfType<ViewResult>(rezultat).Model);
        Assert.IsFalse(model.JeVlasnik);
        Assert.IsFalse(model.OglasiUsluge.Single().VecOcijenjen);
        _recenzijaService.Verify(r => r.MozeOstavitiRecenziju(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Index_MajstorSaProfilom_PopunjavaPodatkeIzvrsioca()
    {
        // Arrange
        var majstor = MajstorSaPunimProfilom();
        PostaviProfilPoUsernameu(majstor, "Majstor");
        _izvrsilacService.Setup(s => s.DajProfilPoIdAsync("m1")).ReturnsAsync(majstor);

        // Act
        var rezultat = await _kontroler.Index("majstor1");

        // Assert
        var model = Assert.IsInstanceOfType<ProfilViewModel>(Assert.IsInstanceOfType<ViewResult>(rezultat).Model);
        Assert.AreEqual("Majstor", model.Uloga);
        Assert.AreEqual("Iskusan majstor", model.Opis);
        Assert.AreEqual(4.5m, model.ProsjecnaOcjena);
        Assert.AreEqual(12, model.BrojZavrsenihPoslova);
        CollectionAssert.AreEqual(new[] { "Vodoinstalater", "Električar" }, model.Vjestine);
        Assert.AreEqual("Vodoinstalater", model.Zanimanje);
        Assert.AreEqual("Sarajevo, Tuzla", model.Lokacija);
        Assert.AreEqual(1, model.PortfolioSlike.Count);
        Assert.AreEqual("Kupatilo", model.PortfolioSlike[0].Opis);
    }

    [TestMethod]
    public async Task Index_MajstorSaRecenzijama_SortiraOdNajnovijeIPostavljaBrojRecenzija()
    {
        // Arrange
        var majstor = MajstorSaPunimProfilom();
        PostaviProfilPoUsernameu(majstor, "Majstor");
        _izvrsilacService.Setup(s => s.DajProfilPoIdAsync("m1")).ReturnsAsync(majstor);

        // Act
        var rezultat = await _kontroler.Index("majstor1");

        // Assert
        var model = Assert.IsInstanceOfType<ProfilViewModel>(Assert.IsInstanceOfType<ViewResult>(rezultat).Model);
        Assert.AreEqual(2, model.BrojRecenzija);
        Assert.AreEqual(2, model.Recenzije[0].RecenzijaId);
        Assert.AreEqual("Klijent", model.Recenzije[0].KlijentIme);
        Assert.IsTrue(model.Recenzije[0].Prijavljena);
        Assert.AreEqual(1, model.Recenzije[1].RecenzijaId);
        Assert.AreEqual("Haso Hasic", model.Recenzije[1].KlijentIme);
    }

    [TestMethod]
    public async Task Index_Majstor_VracaSamoAktivneOglaseMajstora()
    {
        // Arrange
        var majstor = MajstorSaPunimProfilom();
        PostaviProfilPoUsernameu(majstor, "Majstor");
        _izvrsilacService.Setup(s => s.DajProfilPoIdAsync("m1")).ReturnsAsync(majstor);
        _oglasMajstoraFacade.Setup(f => f.DajSveOglase()).ReturnsAsync(new List<OglasMajstora>
        {
            NoviOglasMajstora(1, "m1", Status.Aktivan),
            NoviOglasMajstora(2, "m1", Status.Neaktivan),
            NoviOglasMajstora(3, "m2", Status.Aktivan)
        });

        // Act
        var rezultat = await _kontroler.Index("majstor1");

        // Assert
        var model = Assert.IsInstanceOfType<ProfilViewModel>(Assert.IsInstanceOfType<ViewResult>(rezultat).Model);
        var oglas = model.OglasiMajstora.Single();
        Assert.AreEqual(1, oglas.OglasId);
        Assert.AreEqual("Tuzla", oglas.Lokacija);
        Assert.AreEqual(50d, oglas.MinCijena);
        Assert.AreEqual(TipIsplate.PoSatu, oglas.TipIsplate);
        CollectionAssert.AreEqual(new[] { "Vodoinstalater" }, oglas.Kategorije);
    }

    [TestMethod]
    public async Task Index_MajstorBezProfilaIzvrsioca_VracaPraznePodatkeAliOglase()
    {
        // Arrange
        var majstor = NoviMajstor();
        PostaviProfilPoUsernameu(majstor, "Majstor");
        _izvrsilacService.Setup(s => s.DajProfilPoIdAsync("m1")).ReturnsAsync((IzvrsilacUsluge?)null);
        _oglasMajstoraFacade.Setup(f => f.DajSveOglase()).ReturnsAsync(new List<OglasMajstora>
        {
            NoviOglasMajstora(1, "m1", Status.Aktivan)
        });

        // Act
        var rezultat = await _kontroler.Index("majstor1");

        // Assert
        var model = Assert.IsInstanceOfType<ProfilViewModel>(Assert.IsInstanceOfType<ViewResult>(rezultat).Model);
        Assert.IsNull(model.Opis);
        Assert.AreEqual(0, model.Vjestine.Count);
        Assert.AreEqual(0, model.Recenzije.Count);
        Assert.AreEqual(1, model.OglasiMajstora.Count);
    }

    [TestMethod]
    public async Task Index_MajstorBezKolekcija_VracaPrazneListe()
    {
        // Arrange
        var majstor = NoviMajstor();
        PostaviProfilPoUsernameu(majstor, "Majstor");
        _izvrsilacService.Setup(s => s.DajProfilPoIdAsync("m1")).ReturnsAsync(majstor);

        // Act
        var rezultat = await _kontroler.Index("majstor1");

        // Assert
        var model = Assert.IsInstanceOfType<ProfilViewModel>(Assert.IsInstanceOfType<ViewResult>(rezultat).Model);
        Assert.AreEqual(0, model.Vjestine.Count);
        Assert.IsNull(model.Zanimanje);
        Assert.IsNull(model.Lokacija);
        Assert.AreEqual(0, model.PortfolioSlike.Count);
        Assert.AreEqual(0, model.Recenzije.Count);
    }

    [TestMethod]
    public async Task Index_Firma_PopunjavaPodatkeFirme()
    {
        // Arrange
        var firma = NovaFirma();
        PostaviProfilPoUsernameu(firma, "Firma");
        _izvrsilacService.Setup(s => s.DajProfilPoIdAsync("f1")).ReturnsAsync(firma);

        // Act
        var rezultat = await _kontroler.Index("elektro");

        // Assert
        var model = Assert.IsInstanceOfType<ProfilViewModel>(Assert.IsInstanceOfType<ViewResult>(rezultat).Model);
        Assert.AreEqual("Firma", model.Uloga);
        Assert.AreEqual("Elektro Servis", model.NazivFirme);
        Assert.AreEqual("Elektro Servis", model.DisplayName);
        Assert.AreEqual(10, model.MinZaposlenih);
        Assert.AreEqual(50, model.MaxZaposlenih);
        Assert.AreEqual(TimeSpan.FromHours(8), model.RadnoVrijeme!.OtvorenoOd);
        Assert.AreEqual(TimeSpan.FromHours(16), model.RadnoVrijeme.OtvorenoDo);
        Assert.IsTrue(model.JeVerificiran);
    }

    [TestMethod]
    public async Task Index_FirmaBezAdminVerifikacije_NijeVerificirana()
    {
        // Arrange
        var firma = NovaFirma();
        firma.AdminVerificirao = false;
        PostaviProfilPoUsernameu(firma, "Firma");

        // Act
        var rezultat = await _kontroler.Index("elektro");

        // Assert
        var model = Assert.IsInstanceOfType<ProfilViewModel>(Assert.IsInstanceOfType<ViewResult>(rezultat).Model);
        Assert.IsFalse(model.JeVerificiran);
        Assert.IsTrue(model.JeVerificiranEmail);
    }

    public static IEnumerable<object[]> MogucnostRecenzije =>
        DataDrivenInputParser.CitajXml<string, string, bool>("TestData/xml/ProfilController_mogucnostRecenzije.xml");

    [TestMethod]
    [DynamicData(nameof(MogucnostRecenzije))]
    public async Task Index_MajstorGledanOdPrijavljenogIzXml_MozeOstavitiRecenzijuSamoTudjiKlijent(
        string prijavljeniId, string uloga, bool ocekivano)
    {
        // Arrange
        var majstor = NoviMajstor();
        Prijavi(prijavljeniId, uloga);
        PostaviProfilPoUsernameu(majstor, "Majstor");
        _recenzijaService.Setup(r => r.MozeOstavitiRecenziju(It.IsAny<string>(), "m1")).ReturnsAsync(true);

        // Act
        var rezultat = await _kontroler.Index("majstor1");

        // Assert
        var model = Assert.IsInstanceOfType<ProfilViewModel>(Assert.IsInstanceOfType<ViewResult>(rezultat).Model);
        Assert.AreEqual(ocekivano, model.MozeOstavitiRecenziju);
    }

    [TestMethod]
    public async Task Index_MajstorGledanOdNeprijavljenog_NeProvjeravaRecenzije()
    {
        // Arrange
        var majstor = NoviMajstor();
        PostaviProfilPoUsernameu(majstor, "Majstor");

        // Act
        var rezultat = await _kontroler.Index("majstor1");

        // Assert
        var model = Assert.IsInstanceOfType<ProfilViewModel>(Assert.IsInstanceOfType<ViewResult>(rezultat).Model);
        Assert.IsFalse(model.MozeOstavitiRecenziju);
        _recenzijaService.Verify(r => r.MozeOstavitiRecenziju(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ---------- Edit (GET) ----------

    [TestMethod]
    public async Task Edit_Get_VlastitiProfilKlijenta_VracaViewSaPodacimaKorisnika()
    {
        // Arrange
        var klijent = NoviKlijent();
        klijent.Slika = "https://cdn.popravka.ba/slike/haso.png";
        PripremiVlastitiProfil(klijent, "Klijent");

        // Act
        var rezultat = await _kontroler.Edit(username: null);

        // Assert
        var model = Assert.IsInstanceOfType<ProfilEditViewModel>(Assert.IsInstanceOfType<ViewResult>(rezultat).Model);
        Assert.AreEqual("k1", model.UserId);
        Assert.AreEqual("haso", model.Username);
        Assert.AreEqual("Haso", model.Ime);
        Assert.AreEqual("Hasic", model.Prezime);
        Assert.AreEqual("haso@popravka.ba", model.Email);
        Assert.AreEqual("Klijent", model.Uloga);
        Assert.AreEqual("https://cdn.popravka.ba/slike/haso.png", model.TrenutnaSlika);
        Assert.AreEqual(1, model.Mjesta.Count());
        Assert.AreEqual(1, model.Kategorije.Count());
        _izvrsilacService.Verify(s => s.DajProfilPoIdAsync(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Edit_Get_NepostojeciKorisnik_VracaNotFound()
    {
        // Arrange
        Prijavi("k1", "Klijent");
        _userManager.Setup(m => m.FindByIdAsync("k1")).ReturnsAsync((ApplicationUser?)null);

        // Act
        var rezultat = await _kontroler.Edit(username: null);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task Edit_Get_AdministratorSaUsernameom_UcitavaTudjiProfil()
    {
        // Arrange
        var klijent = NoviKlijent();
        Prijavi("a1", "Administrator");
        PostaviProfilPoUsernameu(klijent, "Klijent");

        // Act
        var rezultat = await _kontroler.Edit("haso");

        // Assert
        var model = Assert.IsInstanceOfType<ProfilEditViewModel>(Assert.IsInstanceOfType<ViewResult>(rezultat).Model);
        Assert.AreEqual("k1", model.UserId);
        _userManager.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Edit_Get_ObicanKorisnikSaTudjimUsernameom_UcitavaVlastitiProfil()
    {
        // Arrange
        var klijent = NoviKlijent();
        PripremiVlastitiProfil(klijent, "Klijent");

        // Act
        var rezultat = await _kontroler.Edit("neki.drugi");

        // Assert
        var model = Assert.IsInstanceOfType<ProfilEditViewModel>(Assert.IsInstanceOfType<ViewResult>(rezultat).Model);
        Assert.AreEqual("k1", model.UserId);
        _userManager.Verify(m => m.FindByNameAsync(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Edit_Get_Majstor_VracaOdabraneKategorijeMjestaIPortfolio()
    {
        // Arrange
        var majstor = MajstorSaPunimProfilom();
        PripremiVlastitiProfil(majstor, "Majstor");
        _izvrsilacService.Setup(s => s.DajProfilPoIdAsync("m1")).ReturnsAsync(majstor);

        // Act
        var rezultat = await _kontroler.Edit(username: null);

        // Assert
        var model = Assert.IsInstanceOfType<ProfilEditViewModel>(Assert.IsInstanceOfType<ViewResult>(rezultat).Model);
        Assert.AreEqual("Majstor", model.Uloga);
        Assert.AreEqual("Iskusan majstor", model.Opis);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, model.SelectedKategorijeId);
        CollectionAssert.AreEqual(new[] { 4, 5 }, model.SelectedMjestaId);
        Assert.AreEqual(1, model.PortfolioSlike.Count);
        Assert.AreEqual(10, model.PortfolioSlike[0].Id);
    }

    [TestMethod]
    public async Task Edit_Get_MajstorBezProfilaIzvrsioca_VracaPraznaPolja()
    {
        // Arrange
        var majstor = NoviMajstor();
        PripremiVlastitiProfil(majstor, "Majstor");
        _izvrsilacService.Setup(s => s.DajProfilPoIdAsync("m1")).ReturnsAsync((IzvrsilacUsluge?)null);

        // Act
        var rezultat = await _kontroler.Edit(username: null);

        // Assert
        var model = Assert.IsInstanceOfType<ProfilEditViewModel>(Assert.IsInstanceOfType<ViewResult>(rezultat).Model);
        Assert.IsNull(model.Opis);
        Assert.AreEqual(0, model.SelectedKategorijeId.Count);
        Assert.AreEqual(0, model.SelectedMjestaId.Count);
    }

    [TestMethod]
    public async Task Edit_Get_Firma_VracaPodatkeFirmeIRadnoVrijeme()
    {
        // Arrange
        var firma = NovaFirma();
        PripremiVlastitiProfil(firma, "Firma");
        _izvrsilacService.Setup(s => s.DajProfilPoIdAsync("f1")).ReturnsAsync(firma);

        // Act
        var rezultat = await _kontroler.Edit(username: null);

        // Assert
        var model = Assert.IsInstanceOfType<ProfilEditViewModel>(Assert.IsInstanceOfType<ViewResult>(rezultat).Model);
        Assert.AreEqual("Elektro Servis", model.NazivFirme);
        Assert.AreEqual(VelicinaFirme.Mala, model.VelicinaFirme);
        Assert.AreEqual(TimeSpan.FromHours(8), model.RadnoVrijeme!.OtvorenoOd);
        Assert.AreEqual(TimeSpan.FromHours(16), model.RadnoVrijeme.OtvorenoDo);
        Assert.AreEqual(0, model.SelectedKategorijeId.Count);
        Assert.AreEqual(0, model.PortfolioSlike.Count);
    }

    // ---------- Edit (POST) ----------

    [TestMethod]
    public async Task Edit_Post_NepostojeciKorisnik_VracaNotFound()
    {
        // Arrange
        Prijavi("k1", "Klijent");
        _userManager.Setup(m => m.FindByIdAsync("k1")).ReturnsAsync((ApplicationUser?)null);

        // Act
        var rezultat = await _kontroler.Edit(NoviVm("k1", "haso", "haso@popravka.ba"), null, null, CancellationToken.None);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        _userManager.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [TestMethod]
    public async Task Edit_Post_KlijentValidanUnos_SnimaIPreusmjeravaNaProfil()
    {
        // Arrange
        var klijent = NoviKlijent();
        PripremiVlastitiProfil(klijent, "Klijent");
        var vm = NoviVm("k1", "haso", "haso@popravka.ba");
        vm.Ime = "Novo";
        vm.Prezime = "Prezime";

        // Act
        var rezultat = await _kontroler.Edit(vm, null, null, CancellationToken.None);

        // Assert
        var preusmjerenje = Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        Assert.AreEqual("Index", preusmjerenje.ActionName);
        Assert.AreEqual("haso", preusmjerenje.RouteValues!["username"]);
        Assert.AreEqual("Profil je uspješno ažuriran.", _kontroler.TempData["Success"]);
        Assert.AreEqual("Novo", klijent.Ime);
        Assert.AreEqual("Prezime", klijent.Prezime);
        _userManager.Verify(m => m.UpdateAsync(klijent), Times.Once);
    }

    [TestMethod]
    public async Task Edit_Post_VlastitiProfil_OsvjezavaPrijavuKorisnika()
    {
        // Arrange
        var klijent = NoviKlijent();
        PripremiVlastitiProfil(klijent, "Klijent");

        // Act
        await _kontroler.Edit(NoviVm("k1", "haso", "haso@popravka.ba"), null, null, CancellationToken.None);

        // Assert
        _signInManager.Verify(s => s.RefreshSignInAsync(klijent), Times.Once);
    }

    [TestMethod]
    public async Task Edit_Post_AdministratorUredjujeTudjiProfil_NeOsvjezavaPrijavu()
    {
        // Arrange
        var klijent = NoviKlijent();
        Prijavi("a1", "Administrator");
        _userManager.Setup(m => m.FindByIdAsync("k1")).ReturnsAsync(klijent);
        PostaviUloge(klijent, "Klijent");

        // Act
        var rezultat = await _kontroler.Edit(NoviVm("k1", "haso", "haso@popravka.ba"), null, null, CancellationToken.None);

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        _userManager.Verify(m => m.UpdateAsync(klijent), Times.Once);
        _signInManager.Verify(s => s.RefreshSignInAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    public static IEnumerable<object[]> PromjeneEmaila =>
        DataDrivenInputParser.CitajXml<string, string, bool>("TestData/xml/ProfilController_promjenaEmaila.xml");

    [TestMethod]
    [DynamicData(nameof(PromjeneEmaila))]
    public async Task Edit_Post_PromjenaEmailaIzXml_EmailPostajeNepotvrdjenSamoAkoJeStvarnoPromijenjen(
        string stariEmail, string noviEmail, bool ocekivano)
    {
        // Arrange
        var klijent = NoviKlijent();
        klijent.Email = stariEmail;
        PripremiVlastitiProfil(klijent, "Klijent");

        // Act
        await _kontroler.Edit(NoviVm("k1", "haso", noviEmail), null, null, CancellationToken.None);

        // Assert
        Assert.AreEqual(ocekivano, klijent.EmailConfirmed);
    }

    [TestMethod]
    public async Task Edit_Post_PromijenjenEmail_NalogPostajeNaCekanju()
    {
        // Arrange
        var klijent = NoviKlijent();
        klijent.OsvjeziStatusAktivnosti();
        PripremiVlastitiProfil(klijent, "Klijent");

        // Act
        await _kontroler.Edit(NoviVm("k1", "haso", "novi@popravka.ba"), null, null, CancellationToken.None);

        // Assert
        Assert.AreEqual("novi@popravka.ba", klijent.Email);
        Assert.AreEqual(Status.NaCekanju, klijent.StatusVerifikacije);
    }

    [TestMethod]
    public async Task Edit_Post_NevalidanModel_VracaViewSaIstimModelomBezSnimanja()
    {
        // Arrange
        var klijent = NoviKlijent();
        PripremiVlastitiProfil(klijent, "Klijent");
        var vm = NoviVm("k1", "haso", "haso@popravka.ba");
        _kontroler.ModelState.AddModelError(nameof(vm.Ime), "Ime je obavezno");

        // Act
        var rezultat = await _kontroler.Edit(vm, null, null, CancellationToken.None);

        // Assert
        var view = Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.AreSame(vm, view.Model);
        Assert.AreEqual("Klijent", vm.Uloga);
        Assert.AreEqual(1, vm.Mjesta.Count());
        _userManager.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [TestMethod]
    public async Task Edit_Post_ZauzetoKorisnickoIme_VracaViewSaGreskomIBezSnimanja()
    {
        // Arrange
        var klijent = NoviKlijent();
        PripremiVlastitiProfil(klijent, "Klijent");
        _userManager.Setup(m => m.FindByNameAsync("zauzeto")).ReturnsAsync(new Klijent { Id = "k2", UserName = "zauzeto" });

        // Act
        var rezultat = await _kontroler.Edit(NoviVm("k1", "zauzeto", "haso@popravka.ba"), null, null, CancellationToken.None);

        // Assert
        Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.IsTrue(_kontroler.ModelState.ContainsKey(nameof(ProfilEditViewModel.Username)));
        Assert.IsTrue(PorukeGresaka().Contains("Korisničko ime je već zauzeto."));
        _userManager.Verify(m => m.SetUserNameAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        _userManager.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [TestMethod]
    public async Task Edit_Post_SlobodnoNovoKorisnickoIme_PozivaSetUserName()
    {
        // Arrange
        var klijent = NoviKlijent();
        PripremiVlastitiProfil(klijent, "Klijent");
        _userManager.Setup(m => m.FindByNameAsync("novo.ime")).ReturnsAsync((ApplicationUser?)null);

        // Act
        var rezultat = await _kontroler.Edit(NoviVm("k1", "novo.ime", "haso@popravka.ba"), null, null, CancellationToken.None);

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        _userManager.Verify(m => m.SetUserNameAsync(klijent, "novo.ime"), Times.Once);
    }

    [TestMethod]
    public async Task Edit_Post_PromjenaKorisnickogImenaNeuspjesna_VracaViewSaGreskomIdentityja()
    {
        // Arrange
        var klijent = NoviKlijent();
        PripremiVlastitiProfil(klijent, "Klijent");
        _userManager.Setup(m => m.FindByNameAsync("novo.ime")).ReturnsAsync((ApplicationUser?)null);
        _userManager.Setup(m => m.SetUserNameAsync(klijent, "novo.ime")).ReturnsAsync(Greske("Neispravno korisničko ime."));

        // Act
        var rezultat = await _kontroler.Edit(NoviVm("k1", "novo.ime", "haso@popravka.ba"), null, null, CancellationToken.None);

        // Assert
        Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.IsTrue(PorukeGresaka().Contains("Neispravno korisničko ime."));
        _userManager.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [TestMethod]
    public async Task Edit_Post_NovaProfilnaSlika_SpremaSlikuUStorageIPostavljaUrl()
    {
        // Arrange
        var klijent = NoviKlijent();
        PripremiVlastitiProfil(klijent, "Klijent");

        // Act
        await _kontroler.Edit(NoviVm("k1", "haso", "haso@popravka.ba"), Fajl(), null, CancellationToken.None);

        // Assert
        Assert.AreEqual(UrlSlike, klijent.Slika);
        _storage.Verify(s => s.SpremiPublic(It.IsAny<Stream>(), "image/png", It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Edit_Post_BezNoveSlike_NeKoristiStorage()
    {
        // Arrange
        var klijent = NoviKlijent();
        PripremiVlastitiProfil(klijent, "Klijent");

        // Act
        await _kontroler.Edit(NoviVm("k1", "haso", "haso@popravka.ba"), null, null, CancellationToken.None);

        // Assert
        _storage.Verify(s => s.SpremiPublic(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Edit_Post_UpdateNeuspjesan_VracaViewSaGreskamaBezOsvjezavanjaPrijave()
    {
        // Arrange
        var klijent = NoviKlijent();
        PripremiVlastitiProfil(klijent, "Klijent");
        _userManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(Greske("Greška A.", "Greška B."));

        // Act
        var rezultat = await _kontroler.Edit(NoviVm("k1", "haso", "haso@popravka.ba"), null, null, CancellationToken.None);

        // Assert
        Assert.IsInstanceOfType<ViewResult>(rezultat);
        var poruke = PorukeGresaka();
        Assert.IsTrue(poruke.Contains("Greška A."));
        Assert.IsTrue(poruke.Contains("Greška B."));
        _signInManager.Verify(s => s.RefreshSignInAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [TestMethod]
    public async Task Edit_Post_IzuzetakPriSnimanju_VracaViewSaPorukomILogujeGresku()
    {
        // Arrange
        var klijent = NoviKlijent();
        PripremiVlastitiProfil(klijent, "Klijent");
        _userManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ThrowsAsync(new InvalidOperationException("Baza nije dostupna"));

        // Act
        var rezultat = await _kontroler.Edit(NoviVm("k1", "haso", "haso@popravka.ba"), null, null, CancellationToken.None);

        // Assert
        Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.IsTrue(PorukeGresaka().Contains("Došlo je do greške pri ažuriranju profila."));
        _logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    // ---------- Edit (POST): majstor ----------

    [TestMethod]
    public async Task Edit_Post_MajstorBezMjestaIKategorija_VracaViewSaDvijeGreskeBezSnimanja()
    {
        // Arrange
        var majstor = NoviMajstor();
        PripremiVlastitiProfil(majstor, "Majstor");
        var vm = NoviVm("m1", "majstor1", "mujo@popravka.ba");
        vm.SelectedMjestaId = new List<int>();
        vm.SelectedKategorijeId = new List<int>();

        // Act
        var rezultat = await _kontroler.Edit(vm, null, null, CancellationToken.None);

        // Assert
        Assert.IsInstanceOfType<ViewResult>(rezultat);
        var poruke = PorukeGresaka();
        Assert.IsTrue(poruke.Contains("Trebate odabrati najmanje 1 lokaciju."));
        Assert.IsTrue(poruke.Contains("Trebate odabrati najmanje 1 kategoriju."));
        _userManager.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [TestMethod]
    public async Task Edit_Post_MajstorBezOdabira_KoristiPostojeceMjestaIKategorijeIzProfila()
    {
        // Arrange
        var majstor = MajstorSaPunimProfilom();
        PripremiVlastitiProfil(majstor, "Majstor");
        _izvrsilacService.Setup(s => s.DajProfilPoIdAsync("m1")).ReturnsAsync(majstor);
        var vm = NoviVm("m1", "majstor1", "mujo@popravka.ba");
        vm.SelectedMjestaId = new List<int>();
        vm.SelectedKategorijeId = new List<int>();

        // Act
        var rezultat = await _kontroler.Edit(vm, null, null, CancellationToken.None);

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        _mjestoService.Verify(m => m.AzurirajMjestaKorisnika("m1", It.Is<List<int>>(l => l.SequenceEqual(new[] { 4, 5 }))), Times.Once);
        _kategorijaService.Verify(k => k.AzurirajKategorijeIzvrsioca("m1", It.Is<List<int>>(l => l.SequenceEqual(new[] { 1, 2, 3 }))), Times.Once);
    }

    [TestMethod]
    public async Task Edit_Post_MajstorSaOdabirom_AzuriraMjestaKategorijeIOpis()
    {
        // Arrange
        var majstor = NoviMajstor();
        PripremiVlastitiProfil(majstor, "Majstor");
        _izvrsilacService.Setup(s => s.DajProfilPoIdAsync("m1")).ReturnsAsync(majstor);
        var vm = NoviVm("m1", "majstor1", "mujo@popravka.ba");
        vm.SelectedMjestaId = new List<int> { 2, 3 };
        vm.SelectedKategorijeId = new List<int> { 4 };
        vm.Opis = "Novi opis";

        // Act
        await _kontroler.Edit(vm, null, null, CancellationToken.None);

        // Assert
        Assert.AreEqual("Novi opis", majstor.Opis);
        _mjestoService.Verify(m => m.AzurirajMjestaKorisnika("m1", vm.SelectedMjestaId), Times.Once);
        _kategorijaService.Verify(k => k.AzurirajKategorijeIzvrsioca("m1", vm.SelectedKategorijeId), Times.Once);
        _izvrsilacService.Verify(s => s.UrediAsync(majstor), Times.Once);
    }

    [TestMethod]
    public async Task Edit_Post_MajstorBezProfilaIzvrsioca_NeUredjujeIzvrsioca()
    {
        // Arrange
        var majstor = NoviMajstor();
        PripremiVlastitiProfil(majstor, "Majstor");
        _izvrsilacService.Setup(s => s.DajProfilPoIdAsync("m1")).ReturnsAsync((IzvrsilacUsluge?)null);

        // Act
        var rezultat = await _kontroler.Edit(NoviVm("m1", "majstor1", "mujo@popravka.ba"), null, null, CancellationToken.None);

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        _izvrsilacService.Verify(s => s.UrediAsync(It.IsAny<IzvrsilacUsluge>()), Times.Never);
    }

    public static IEnumerable<object[]> BrojeviNovihSlika =>
        DataDrivenInputParser.CitajXml<int, int>("TestData/xml/ProfilController_noveSlike.xml");

    [TestMethod]
    [DynamicData(nameof(BrojeviNovihSlika))]
    public async Task Edit_Post_NoveSlikeIzXml_SpremaNajviseDesetSlikaUPortfolio(int brojSlika, int ocekivano)
    {
        // Arrange
        var majstor = NoviMajstor();
        PripremiVlastitiProfil(majstor, "Majstor");
        _izvrsilacService.Setup(s => s.DajProfilPoIdAsync("m1")).ReturnsAsync(majstor);

        // Act
        await _kontroler.Edit(NoviVm("m1", "majstor1", "mujo@popravka.ba"), null, Slike(brojSlika), CancellationToken.None);

        // Assert
        Assert.AreEqual(ocekivano, majstor.SlikePortfolija!.Count);
        _storage.Verify(s => s.SpremiPublic(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(ocekivano));
    }

    [TestMethod]
    public async Task Edit_Post_PrazneDatotekeIzForme_SePreskacu()
    {
        // Arrange
        var majstor = NoviMajstor();
        PripremiVlastitiProfil(majstor, "Majstor");
        _izvrsilacService.Setup(s => s.DajProfilPoIdAsync("m1")).ReturnsAsync(majstor);
        var datoteke = new List<IFormFile> { Fajl(0), Fajl(100) };

        // Act
        await _kontroler.Edit(NoviVm("m1", "majstor1", "mujo@popravka.ba"), null, datoteke, CancellationToken.None);

        // Assert
        Assert.AreEqual(1, majstor.SlikePortfolija!.Count);
        Assert.AreEqual(UrlSlike, majstor.SlikePortfolija.Single().URL);
        _storage.Verify(s => s.SpremiPublic(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Edit_Post_StorageNeVratiUrl_SlikaSeNeDodajeUPortfolio()
    {
        // Arrange
        var majstor = NoviMajstor();
        PripremiVlastitiProfil(majstor, "Majstor");
        _izvrsilacService.Setup(s => s.DajProfilPoIdAsync("m1")).ReturnsAsync(majstor);
        _storage.Setup(s => s.SpremiPublic(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string)null!);

        // Act
        await _kontroler.Edit(NoviVm("m1", "majstor1", "mujo@popravka.ba"), null, Slike(2), CancellationToken.None);

        // Assert
        Assert.AreEqual(0, majstor.SlikePortfolija!.Count);
    }

    [TestMethod]
    public async Task Edit_Post_SlikeZaBrisanje_UklanjaSamoOdabranePortfolioSlike()
    {
        // Arrange
        var majstor = NoviMajstor();
        majstor.SlikePortfolija = new List<PortfolioSlika>
        {
            new() { PortfolioSlikaID = 1, URL = "https://cdn.popravka.ba/1.png" },
            new() { PortfolioSlikaID = 2, URL = "https://cdn.popravka.ba/2.png" },
            new() { PortfolioSlikaID = 3, URL = "https://cdn.popravka.ba/3.png" }
        };
        PripremiVlastitiProfil(majstor, "Majstor");
        _izvrsilacService.Setup(s => s.DajProfilPoIdAsync("m1")).ReturnsAsync(majstor);
        var vm = NoviVm("m1", "majstor1", "mujo@popravka.ba");
        vm.SlikeZaBrisanje = new List<int> { 1, 3 };

        // Act
        await _kontroler.Edit(vm, null, null, CancellationToken.None);

        // Assert
        Assert.AreEqual(1, majstor.SlikePortfolija.Count);
        Assert.AreEqual(2, majstor.SlikePortfolija.Single().PortfolioSlikaID);
        _izvrsilacService.Verify(s => s.UrediAsync(majstor), Times.Once);
    }

    // ---------- Edit (POST): firma ----------

    [TestMethod]
    public async Task Edit_Post_FirmaValidanUnos_AzuriraNazivVelicinuIRadnoVrijeme()
    {
        // Arrange
        var firma = NovaFirma();
        PripremiVlastitiProfil(firma, "Firma");
        _izvrsilacService.Setup(s => s.DajProfilPoIdAsync("f1")).ReturnsAsync(firma);
        var vm = NoviVm("f1", "elektro", "firma@popravka.ba");
        vm.NazivFirme = "Novi Servis";
        vm.VelicinaFirme = VelicinaFirme.Srednja;
        vm.RadnoVrijeme = Radno(9, 17);

        // Act
        var rezultat = await _kontroler.Edit(vm, null, null, CancellationToken.None);

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        Assert.AreEqual("Novi Servis", firma.NazivFirme);
        Assert.AreEqual(VelicinaFirme.Srednja, firma.VelicinaFirme);
        Assert.AreEqual(51, firma.MinZaposlenih);
        Assert.AreEqual(100, firma.MaxZaposlenih);
        Assert.AreEqual(TimeSpan.FromHours(9), firma.OtvorenoOd);
        Assert.AreEqual(TimeSpan.FromHours(17), firma.OtvorenoDo);
        Assert.AreEqual("09:00 - 17:00", firma.RadnoVrijeme);
    }

    [TestMethod]
    public async Task Edit_Post_Firma_IgnorisImeIPrezime()
    {
        // Arrange
        var firma = NovaFirma();
        PripremiVlastitiProfil(firma, "Firma");
        var vm = NoviVm("f1", "elektro", "firma@popravka.ba");
        vm.NazivFirme = "Elektro Servis";
        vm.Ime = "Ime";
        vm.Prezime = "Prezime";
        _kontroler.ModelState.AddModelError(nameof(vm.Ime), "Ime je obavezno");
        _kontroler.ModelState.AddModelError(nameof(vm.Prezime), "Prezime je obavezno");

        // Act
        var rezultat = await _kontroler.Edit(vm, null, null, CancellationToken.None);

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        Assert.IsNull(firma.Ime);
        Assert.IsNull(firma.Prezime);
    }

    [TestMethod]
    public async Task Edit_Post_FirmaBezRadnogVremena_BriseRadnoVrijemeAZadrzavaVelicinu()
    {
        // Arrange
        var firma = NovaFirma();
        firma.RadnoVrijeme = "08:00 - 16:00";
        PripremiVlastitiProfil(firma, "Firma");
        var vm = NoviVm("f1", "elektro", "firma@popravka.ba");
        vm.NazivFirme = "Elektro Servis";
        vm.VelicinaFirme = null;
        vm.RadnoVrijeme = null;

        // Act
        await _kontroler.Edit(vm, null, null, CancellationToken.None);

        // Assert
        Assert.IsNull(firma.OtvorenoOd);
        Assert.IsNull(firma.OtvorenoDo);
        Assert.IsNull(firma.RadnoVrijeme);
        Assert.AreEqual(VelicinaFirme.Mala, firma.VelicinaFirme);
    }

    [TestMethod]
    public async Task Edit_Post_FirmaBezNazivaFirme_VracaViewSaGreskomBezSnimanja()
    {
        // Arrange
        var firma = NovaFirma();
        PripremiVlastitiProfil(firma, "Firma");
        var vm = NoviVm("f1", "elektro", "firma@popravka.ba");
        vm.NazivFirme = "   ";

        // Act
        var rezultat = await _kontroler.Edit(vm, null, null, CancellationToken.None);

        // Assert
        Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.IsTrue(_kontroler.ModelState.ContainsKey(nameof(ProfilEditViewModel.NazivFirme)));
        Assert.IsTrue(PorukeGresaka().Contains("Naziv firme je obavezan."));
        _userManager.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    public static IEnumerable<object[]> NeispravnaRadnaVremena =>
        DataDrivenInputParser.CitajXml<int?, int?, string>("TestData/xml/ProfilController_radnoVrijeme.xml");

    [TestMethod]
    [DynamicData(nameof(NeispravnaRadnaVremena))]
    public async Task Edit_Post_FirmaNeispravnoRadnoVrijemeIzXml_VracaViewSaPorukom(int? otvorenoOd, int? otvorenoDo, string poruka)
    {
        // Arrange
        var firma = NovaFirma();
        PripremiVlastitiProfil(firma, "Firma");
        var vm = NoviVm("f1", "elektro", "firma@popravka.ba");
        vm.NazivFirme = "Elektro Servis";
        vm.RadnoVrijeme = Radno(otvorenoOd, otvorenoDo);

        // Act
        var rezultat = await _kontroler.Edit(vm, null, null, CancellationToken.None);

        // Assert
        Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.IsTrue(PorukeGresaka().Contains(poruka));
        _userManager.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }
}
