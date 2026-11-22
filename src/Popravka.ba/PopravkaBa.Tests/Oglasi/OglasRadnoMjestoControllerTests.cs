using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Interface;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using PopravkaBa.Web.Controllers;
using PopravkaBa.Web.Models.ViewModels;
using static PopravkaBa.Tests.Oglasi.OglasControllerTestHelper;
using static PopravkaBa.Tests.Oglasi.OglasiTestHelper;

namespace PopravkaBa.Tests.Oglasi;

[TestClass]
[TestCategory("Controller")]
public class OglasRadnoMjestoControllerTests
{
    private Mock<IOglasRadnoMjestoFacade> _fasada = null!;
    private Mock<IMjestoService> _mjesta = null!;
    private Mock<UserManager<ApplicationUser>> _userManager = null!;
    private Mock<IFileStorage> _storage = null!;
    private OglasRadnoMjestoController _kontroler = null!;
    private Kategorija[] _kategorije = null!;

    [TestInitialize]
    public void Postavi()
    {
        _fasada = new Mock<IOglasRadnoMjestoFacade>();
        _mjesta = new Mock<IMjestoService>();
        _userManager = TestHelpers.MockUserManager();
        _storage = new Mock<IFileStorage>();
        _kategorije = new[] { new Kategorija { ID = 1, Naziv = "Elektroinstalacije" } };

        _fasada.Setup(f => f.DajSveKategorije()).ReturnsAsync(_kategorije);
        _userManager.KorisnikIzClaimova();
        _storage.Setup(s => s.SpremiPublic(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NovaSlikaUrl);

        _kontroler = new OglasRadnoMjestoController(_fasada.Object, _mjesta.Object, _storage.Object,
            _userManager.Object, NullLogger<OglasRadnoMjestoController>.Instance, Mock.Of<IPrijavaOglasService>());
        TestHelpers.PrijaviKorisnika(_kontroler, VlasnikId, "Firma");
    }

    private void PostojiOglas(OglasRadnoMjesto oglas) =>
        _fasada.Setup(f => f.DajOglasPoId(oglas.OglasID)).ReturnsAsync(oglas);

    private async Task<OglasRadnoMjestoDetaljiViewModel> Detalji(int id) =>
        (OglasRadnoMjestoDetaljiViewModel)((ViewResult)await _kontroler.Detalji(id)).Model!;

    private static PrijavaRadnoMjesto Prijava(int id, string majstorId, Majstor? majstor = null) => new()
    {
        ID = id, OglasID = 1, MajstorID = majstorId, Majstor = majstor!, StatusPrijave = Status.NaCekanju,
        VrijemePrijave = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc)
    };

    // ---------- Index ----------

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_BezPretrage_PrikazujeSveOglase()
    {
        // Arrange
        var oglasi = new[] { OglasRadnoMjesto(1) };
        _fasada.Setup(f => f.DajSveOglase()).ReturnsAsync(oglasi);

        // Act
        var rezultat = await _kontroler.Index(null, null);

        // Assert
        ProvjeriPogledSaModelom(rezultat, oglasi);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_TekstPretrage_PrikazujeRezultatePretrage()
    {
        // Arrange
        var oglasi = new[] { OglasRadnoMjesto(2) };
        _fasada.Setup(f => f.PronadjiOglase("električar", 1)).ReturnsAsync(oglasi);

        // Act
        var rezultat = await _kontroler.Index("električar", 1);

        // Assert
        ProvjeriPogledSaModelom(rezultat, oglasi);
        Assert.AreEqual("električar", _kontroler.ViewData["Search"]);
    }

    // ---------- Detalji ----------

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Detalji_NepostojeciOglas_VracaNotFound()
    {
        // Act
        var rezultat = await _kontroler.Detalji(99);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Detalji_OglasSaUvjetimaIDozvolama_MapiraPodatkeOglasa()
    {
        // Arrange
        var oglas = OglasRadnoMjesto(1);
        oglas.Uvjeti = new List<UvjetOglasa> { new() { TekstUvjeta = "Iskustvo 2 godine" } };
        oglas.VozackeDozvole = new List<OglasVozackaDozvola> { new() { VozackaDozvola = VozackaDozvola.B } };
        PostojiOglas(oglas);

        // Act
        var vm = await Detalji(1);

        // Assert
        Assert.AreEqual(oglas.Naslov, vm.Naslov);
        Assert.AreEqual(oglas.MaxPrihod, vm.MaxPrihod);
        CollectionAssert.AreEqual(new[] { "Iskustvo 2 godine" }, vm.Uvjeti);
        CollectionAssert.AreEqual(new[] { VozackaDozvola.B }, vm.VozackeDozvole);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Detalji_OglasSaPrijavom_MapiraPodatkeMajstora()
    {
        // Arrange
        var majstor = new Majstor { Id = "m1", Ime = "Mujo", Prezime = "Mujić", ProsjecnaOcjena = 4.5m, BrojRecenzija = 3 };
        var oglas = OglasRadnoMjesto(1);
        oglas.Prijave = new List<PrijavaRadnoMjesto> { Prijava(7, "m1", majstor) };
        PostojiOglas(oglas);
        _userManager.Setup(m => m.FindByIdAsync("m1")).ReturnsAsync(new Majstor { UserName = "mujo" });

        // Act
        var prijava = (await Detalji(1)).Prijave.Single();

        // Assert
        Assert.AreEqual(7, prijava.PrijavaId);
        Assert.AreEqual("mujo", prijava.MajstorUsername);
        Assert.AreEqual("Mujo Mujić", prijava.MajstorIme);
        Assert.AreEqual(4.5m, prijava.ProsjecnaOcjena);
        Assert.AreEqual(3, prijava.BrojRecenzija);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Detalji_PrijavaBezMajstora_KoristiZamjenskeVrijednosti()
    {
        // Arrange
        var oglas = OglasRadnoMjesto(1);
        oglas.Prijave = new List<PrijavaRadnoMjesto> { Prijava(7, "m1") };
        PostojiOglas(oglas);

        // Act
        var prijava = (await Detalji(1)).Prijave.Single();

        // Assert
        Assert.AreEqual("m1", prijava.MajstorUsername);
        Assert.AreEqual("—", prijava.MajstorIme);
        Assert.AreEqual(0, prijava.ProsjecnaOcjena);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Detalji_OglasBezListiPrijavaIUvjeta_VracaPrazneListe()
    {
        // Arrange
        var oglas = OglasRadnoMjesto(1);
        oglas.Prijave = null!;
        oglas.Uvjeti = null!;
        oglas.VozackeDozvole = null!;
        PostojiOglas(oglas);

        // Act
        var vm = await Detalji(1);

        // Assert
        Assert.AreEqual(0, vm.Prijave.Count + vm.Uvjeti.Count + vm.VozackeDozvole.Count);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    [DataRow("m1", "Majstor", true)]
    [DataRow(VlasnikId, "Majstor", false)]
    [DataRow("k1", "Klijent", false)]
    public async Task Detalji_UlogaKorisnika_OdredjujeMozeLiSePrijaviti(string korisnikId, string uloga, bool moze)
    {
        // Arrange
        PostojiOglas(OglasRadnoMjesto(1));
        TestHelpers.PrijaviKorisnika(_kontroler, korisnikId, uloga);

        // Act
        var vm = await Detalji(1);

        // Assert
        Assert.AreEqual(moze, vm.MozeApplicirati);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Detalji_MajstorSePrijavio_OznacavaDaJeVecPrijavljen()
    {
        // Arrange
        var oglas = OglasRadnoMjesto(1);
        oglas.Prijave = new List<PrijavaRadnoMjesto> { Prijava(7, "m1") };
        PostojiOglas(oglas);
        TestHelpers.PrijaviKorisnika(_kontroler, "m1", "Majstor");

        // Act
        var vm = await Detalji(1);

        // Assert
        Assert.IsTrue(vm.VecApplicirao);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    [DataRow(VlasnikId, "Firma", true)]
    [DataRow("admin", "Administrator", true)]
    [DataRow(DrugiKorisnikId, "Firma", false)]
    public async Task Detalji_KorisnikIUloga_OdredjujeDaLiJeVlasnik(string korisnikId, string uloga, bool jeVlasnik)
    {
        // Arrange
        PostojiOglas(OglasRadnoMjesto(1));
        TestHelpers.PrijaviKorisnika(_kontroler, korisnikId, uloga);

        // Act
        var vm = await Detalji(1);

        // Assert
        Assert.AreEqual(jeVlasnik, vm.JeVlasnik);
    }

    // ---------- ObjaviOglas ----------

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_Get_PrikazujeFormuSaKategorijama()
    {
        // Act
        await _kontroler.ObjaviOglas();

        // Assert
        Assert.AreSame(_kategorije, _kontroler.ViewData["Kategorije"]);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_Post_NevalidanModel_VracaFormuBezObjave()
    {
        // Arrange
        var dto = ObjaviRadnoMjestoDto();
        _kontroler.ModelState.AddModelError("Naslov", "Naslov je obavezan.");

        // Act
        var rezultat = await _kontroler.ObjaviOglas(dto);

        // Assert
        ProvjeriPogledSaModelom(rezultat, dto);
        _fasada.Verify(f => f.ObjaviOglas(It.IsAny<ObjaviOglasRadnoMjestoDto>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_Post_IspravanModel_ObjavljujeIPreusmjeravaNaListu()
    {
        // Arrange
        var dto = ObjaviRadnoMjestoDto();

        // Act
        var rezultat = await _kontroler.ObjaviOglas(dto);

        // Assert
        _fasada.Verify(f => f.ObjaviOglas(dto, VlasnikId), Times.Once);
        ProvjeriPreusmjerenjeNaIndex(rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_Post_ServisBaciIzuzetak_VracaFormuSaGreskom()
    {
        // Arrange
        var dto = ObjaviRadnoMjestoDto();
        _fasada.Setup(f => f.ObjaviOglas(dto, VlasnikId)).ThrowsAsync(new Exception("Baza nije dostupna."));

        // Act
        var rezultat = await _kontroler.ObjaviOglas(dto);

        // Assert
        ProvjeriPogledSaModelom(rezultat, dto);
        Assert.IsFalse(_kontroler.ModelState.IsValid);
    }

    // ---------- UrediOglas (GET) ----------

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Get_NepostojeciOglas_VracaNotFound()
    {
        // Act
        var rezultat = await _kontroler.UrediOglas(99);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Get_TudjiOglas_VracaNaDetaljeSaGreskom()
    {
        // Arrange
        PostojiOglas(OglasRadnoMjesto(1, vlasnikId: DrugiKorisnikId));

        // Act
        var rezultat = await _kontroler.UrediOglas(1);

        // Assert
        ProvjeriPreusmjerenjeNaDetalje(rezultat, 1);
        Assert.AreEqual("Nemate dozvolu za uređivanje ovog oglasa.", _kontroler.TempData["Error"]);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Get_NeaktivanOglas_VracaNaDetaljeSaGreskom()
    {
        // Arrange
        PostojiOglas(OglasRadnoMjesto(1, status: Status.Neaktivan));

        // Act
        var rezultat = await _kontroler.UrediOglas(1);

        // Assert
        ProvjeriPreusmjerenjeNaDetalje(rezultat, 1);
        Assert.AreEqual("Samo aktivni oglasi se mogu uređivati.", _kontroler.TempData["Error"]);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Get_VlastitiOglas_PopunjavaFormuPodacimaOglasa()
    {
        // Arrange
        var oglas = OglasRadnoMjesto(1);
        oglas.Uvjeti = new List<UvjetOglasa> { new() { TekstUvjeta = "Vozačka B" } };
        oglas.Kategorije = new[] { new OglasKategorija { KategorijaID = 2 } };
        PostojiOglas(oglas);

        // Act
        var dto = (UrediOglasRadnoMjestoDto)((ViewResult)await _kontroler.UrediOglas(1)).Model!;

        // Assert
        Assert.AreEqual(oglas.Naslov, dto.Naslov);
        Assert.AreEqual(oglas.MinPrihod, dto.MinPrihod);
        CollectionAssert.AreEqual(new[] { "Vozačka B" }, dto.Uvjeti);
        CollectionAssert.AreEqual(new[] { 2 }, dto.KategorijeID);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Get_OglasBezListi_FormaImaPrazneListe()
    {
        // Arrange
        var oglas = OglasRadnoMjesto(1);
        oglas.Uvjeti = null!;
        oglas.VozackeDozvole = null!;
        PostojiOglas(oglas);

        // Act
        var dto = (UrediOglasRadnoMjestoDto)((ViewResult)await _kontroler.UrediOglas(1)).Model!;

        // Assert
        Assert.AreEqual(0, dto.Uvjeti.Count + dto.VozackeDozvole!.Count + dto.KategorijeID.Count);
    }

    // ---------- UrediOglas (POST) ----------

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_IdSeNePoklapa_VracaBadRequest()
    {
        // Act
        var rezultat = await _kontroler.UrediOglas(2, UrediRadnoMjestoDto(1), null);

        // Assert
        Assert.IsInstanceOfType<BadRequestResult>(rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_NepostojeciOglas_VracaNotFound()
    {
        // Act
        var rezultat = await _kontroler.UrediOglas(99, UrediRadnoMjestoDto(99), null);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_TudjiOglas_NeUredjujeOglas()
    {
        // Arrange
        PostojiOglas(OglasRadnoMjesto(1, vlasnikId: DrugiKorisnikId));

        // Act
        var rezultat = await _kontroler.UrediOglas(1, UrediRadnoMjestoDto(1), null);

        // Assert
        ProvjeriPreusmjerenjeNaDetalje(rezultat, 1);
        _fasada.Verify(f => f.UrediOglas(It.IsAny<UrediOglasRadnoMjestoDto>()), Times.Never);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_PrazniUvjeti_UklanjaPrazneUvjete()
    {
        // Arrange
        PostojiOglas(OglasRadnoMjesto(1));
        var dto = UrediRadnoMjestoDto(1);
        dto.Uvjeti = new List<string> { "Vozačka B", "", "   " };

        // Act
        await _kontroler.UrediOglas(1, dto, null);

        // Assert
        CollectionAssert.AreEqual(new[] { "Vozačka B" }, dto.Uvjeti);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_UvjetiNisuPoslani_PostavljaPraznuListu()
    {
        // Arrange
        PostojiOglas(OglasRadnoMjesto(1));
        var dto = UrediRadnoMjestoDto(1);
        dto.Uvjeti = null!;

        // Act
        await _kontroler.UrediOglas(1, dto, null);

        // Assert
        Assert.AreEqual(0, dto.Uvjeti.Count);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    [DataRow("slika.png")]
    [DataRow("SLIKA.JPG")]
    [DataRow("slika.webp")]
    public async Task UrediOglas_Post_DozvoljenFormatSlike_SpremaNovuSliku(string ime)
    {
        // Arrange
        PostojiOglas(OglasRadnoMjesto(1));
        var dto = UrediRadnoMjestoDto(1);

        // Act
        await _kontroler.UrediOglas(1, dto, Fajl(ime));

        // Assert
        Assert.AreEqual(NovaSlikaUrl, dto.Slika);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_NedozvoljenFormatSlike_VracaFormuSaGreskomZaSliku()
    {
        // Arrange
        PostojiOglas(OglasRadnoMjesto(1));
        var dto = UrediRadnoMjestoDto(1);

        // Act
        var rezultat = await _kontroler.UrediOglas(1, dto, Fajl("dokument.pdf"));

        // Assert
        ProvjeriPogledSaModelom(rezultat, dto);
        Assert.IsTrue(_kontroler.ModelState.ContainsKey("Slika"));
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_SlikaVecaOd5MB_VracaFormuSaGreskomZaSliku()
    {
        // Arrange
        PostojiOglas(OglasRadnoMjesto(1));
        var dto = UrediRadnoMjestoDto(1);

        // Act
        var rezultat = await _kontroler.UrediOglas(1, dto, Fajl("slika.png", 5 * 1024 * 1024 + 1));

        // Assert
        ProvjeriPogledSaModelom(rezultat, dto);
        Assert.IsTrue(_kontroler.ModelState.ContainsKey("Slika"));
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_PraznaSlika_ZadrzavaStaruSlikuBezSpremanja()
    {
        // Arrange
        PostojiOglas(OglasRadnoMjesto(1));
        var dto = UrediRadnoMjestoDto(1);

        // Act
        await _kontroler.UrediOglas(1, dto, Fajl("slika.png", 0));

        // Assert
        Assert.AreEqual("stara.png", dto.Slika);
        _storage.Verify(s => s.SpremiPublic(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_IspravanModel_UredjujeIPreusmjeravaNaDetalje()
    {
        // Arrange
        PostojiOglas(OglasRadnoMjesto(1));
        var dto = UrediRadnoMjestoDto(1);

        // Act
        var rezultat = await _kontroler.UrediOglas(1, dto, null);

        // Assert
        _fasada.Verify(f => f.UrediOglas(dto), Times.Once);
        ProvjeriPreusmjerenjeNaDetalje(rezultat, 1);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_Post_ServisBaciIzuzetak_VracaFormuSaGreskom()
    {
        // Arrange
        PostojiOglas(OglasRadnoMjesto(1));
        var dto = UrediRadnoMjestoDto(1);
        _fasada.Setup(f => f.UrediOglas(dto)).ThrowsAsync(new Exception("Baza nije dostupna."));

        // Act
        var rezultat = await _kontroler.UrediOglas(1, dto, null);

        // Assert
        ProvjeriPogledSaModelom(rezultat, dto);
        Assert.IsFalse(_kontroler.ModelState.IsValid);
    }

    // ---------- ObrisiOglas ----------

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObrisiOglas_Get_NepostojeciOglas_VracaNotFound()
    {
        // Act
        var rezultat = await _kontroler.ObrisiOglas(99);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObrisiOglas_Get_PostojeciOglas_PrikazujePotvrdu()
    {
        // Arrange
        var oglas = OglasRadnoMjesto(1);
        PostojiOglas(oglas);

        // Act
        var rezultat = await _kontroler.ObrisiOglas(1);

        // Assert
        ProvjeriPogledSaModelom(rezultat, oglas);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task PotvrdaBrisanjaOglasa_NepostojeciOglas_VracaNotFound()
    {
        // Act
        var rezultat = await _kontroler.PotvrdaBrisanjaOglasa(99);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task PotvrdaBrisanjaOglasa_TudjiOglas_NeBriseOglas()
    {
        // Arrange
        PostojiOglas(OglasRadnoMjesto(1, vlasnikId: DrugiKorisnikId));

        // Act
        await _kontroler.PotvrdaBrisanjaOglasa(1);

        // Assert
        _fasada.Verify(f => f.ObrisiOglas(It.IsAny<int>()), Times.Never);
        Assert.AreEqual("Nemate dozvolu za brisanje ovog oglasa.", _kontroler.TempData["Error"]);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task PotvrdaBrisanjaOglasa_VlastitiOglas_DeaktiviraOglas()
    {
        // Arrange
        PostojiOglas(OglasRadnoMjesto(1));

        // Act
        var rezultat = await _kontroler.PotvrdaBrisanjaOglasa(1);

        // Assert
        _fasada.Verify(f => f.ObrisiOglas(1), Times.Once);
        ProvjeriPreusmjerenjeNaDetalje(rezultat, 1);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task PotvrdaBrisanjaOglasa_OglasNestaoPriBrisanju_VracaNotFound()
    {
        // Arrange
        PostojiOglas(OglasRadnoMjesto(1));
        _fasada.Setup(f => f.ObrisiOglas(1)).ThrowsAsync(new KeyNotFoundException());

        // Act
        var rezultat = await _kontroler.PotvrdaBrisanjaOglasa(1);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }
}
