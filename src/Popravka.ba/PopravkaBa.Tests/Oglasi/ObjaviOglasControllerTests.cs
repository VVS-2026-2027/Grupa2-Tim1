using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Interface;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using PopravkaBa.Web.Controllers;
using static PopravkaBa.Tests.Oglasi.ObjaviOglasControllerTestHelper;

namespace PopravkaBa.Tests.Oglasi;

[TestClass]
public class ObjaviOglasControllerTests
{
    private const string VlasnikId = "k1";
    private const string SlikaUrl = "https://example.test/oglas.png";
    private const string GreskaObjave = "Greška pri objavi oglasa.";

    private Mock<IOglasUslugeFacade> _usluge = null!;
    private Mock<IOglasMajstoraFacade> _majstori = null!;
    private Mock<IOglasRadnoMjestoFacade> _radnaMjesta = null!;
    private Mock<IFileStorage> _storage = null!;
    private ObjaviOglasController _kontroler = null!;
    private Kategorija[] _kategorije = null!;
    private Mjesto[] _mjesta = null!;

    [TestInitialize]
    public void PripremiTest()
    {
        _usluge = new Mock<IOglasUslugeFacade>();
        _majstori = new Mock<IOglasMajstoraFacade>();
        _radnaMjesta = new Mock<IOglasRadnoMjestoFacade>();
        _storage = new Mock<IFileStorage>();
        var mjestoService = new Mock<IMjestoService>();
        var userManager = TestHelpers.MockUserManager();

        _kategorije = new[] { new Kategorija { ID = 1, Naziv = "Vodoinstalacije" } };
        _mjesta = new[] { new Mjesto { MjestoID = 1, Naziv = "Sarajevo" } };

        _usluge.Setup(s => s.DajSveKategorije()).ReturnsAsync(_kategorije);
        mjestoService.Setup(s => s.DajSvaMjestaAsync()).ReturnsAsync(_mjesta);
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns((ClaimsPrincipal korisnik) =>
                korisnik.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        _kontroler = new ObjaviOglasController(
            _usluge.Object,
            _majstori.Object,
            _radnaMjesta.Object,
            mjestoService.Object,
            _storage.Object,
            userManager.Object,
            NullLogger<ObjaviOglasController>.Instance);

        TestHelpers.PrijaviKorisnika(_kontroler, VlasnikId, "Klijent");
    }

    [TestMethod]
    [DataRow("Klijent", null, "usluge")]
    [DataRow("Firma", null, "majstora")]
    [DataRow("Majstor", null, "majstora")]
    [DataRow("Administrator", null, "usluge")]
    [DataRow("Klijent", "radnomjesto", "radnomjesto")]
    public async Task Index_UlogaITab_VracaPogledSKategorijamaIMjestima(
        string uloga, string? tab, string ocekivaniTab)
    {
        // Arrange
        TestHelpers.PrijaviKorisnika(_kontroler, VlasnikId, uloga);

        // Act
        var rezultat = await _kontroler.Index(tab);

        // Assert
        Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.AreEqual(ocekivaniTab, _kontroler.ViewData["AktivniTab"]);
        ProvjeriListe(_kontroler, _kategorije, _mjesta);
    }

    [TestMethod]
    public async Task ObjaviOglasUsluge_ValidnaSlika_SpremaSlikuIObjavljujeOglas()
    {
        // Arrange
        var dto = new ObjaviOglasUslugeDto();
        using var sadrzaj = new MemoryStream(new byte[] { 1, 2, 3 });
        var slika = new FormFile(sadrzaj, 0, sadrzaj.Length, "Slika", "oglas.PNG")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        using var otkazivanje = new CancellationTokenSource();
        var token = otkazivanje.Token;
        _storage.Setup(s => s.SpremiPublic(It.IsAny<Stream>(), "image/png", token))
            .ReturnsAsync(SlikaUrl);
        _usluge.Setup(s => s.ObjaviOglas(dto, VlasnikId)).ReturnsAsync(OglasId);

        // Act
        var rezultat = await _kontroler.ObjaviOglasUsluge(dto, slika, token);

        // Assert
        ProvjeriDetalje(rezultat, "OglasUsluge");
        Assert.AreEqual(SlikaUrl, dto.Slika);
        Assert.AreEqual("Oglas usluge je uspješno objavljen.",
            _kontroler.TempData["Success"]);
        _storage.Verify(s => s.SpremiPublic(It.IsAny<Stream>(), "image/png", token),
            Times.Once());
        _usluge.Verify(s => s.ObjaviOglas(dto, VlasnikId), Times.Once());
    }

    [TestMethod]
    [DataRow("oglas.gif", 1L, "Nepodržan format slike (.jpg, .jpeg, .png, .webp).")]
    [DataRow("oglas.png", 5242881L, "Slika ne smije biti veća od 5MB.")]
    public async Task ObjaviOglasUsluge_NeispravnaSlika_VracaObrazacBezObjave(
        string naziv, long velicina, string ocekivanaGreska)
    {
        // Arrange
        var dto = new ObjaviOglasUslugeDto();
        var slika = new Mock<IFormFile>();
        slika.SetupGet(s => s.FileName).Returns(naziv);
        slika.SetupGet(s => s.Length).Returns(velicina);

        // Act
        var rezultat = await _kontroler.ObjaviOglasUsluge(dto, slika.Object);

        // Assert
        ProvjeriObrazac(_kontroler, _kategorije, _mjesta, rezultat, dto, "usluge");
        Assert.AreEqual(ocekivanaGreska,
            _kontroler.ModelState["Slika"]!.Errors.Single().ErrorMessage);
        _storage.Verify(s => s.SpremiPublic(
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never());
        _usluge.Verify(s => s.ObjaviOglas(
            It.IsAny<ObjaviOglasUslugeDto>(), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    public async Task ObjaviOglasUsluge_IzuzetakFasade_VracaObrazacSPorukom()
    {
        // Arrange
        var dto = new ObjaviOglasUslugeDto();
        _usluge.Setup(s => s.ObjaviOglas(dto, VlasnikId))
            .ThrowsAsync(new Exception("Objava nije uspjela."));

        // Act
        var rezultat = await _kontroler.ObjaviOglasUsluge(dto, null);

        // Assert
        ProvjeriObrazac(_kontroler, _kategorije, _mjesta, rezultat, dto, "usluge");
        Assert.AreEqual(GreskaObjave, _kontroler.TempData["Error"]);
    }

    [TestMethod]
    public async Task ObjaviOglasMajstora_IspravanUnos_ObjavljujeIPreusmjerava()
    {
        // Arrange
        TestHelpers.PrijaviKorisnika(_kontroler, VlasnikId, "Majstor");
        var dto = new ObjaviOglasMajstoraDto();
        _majstori.Setup(s => s.ObjaviOglas(dto, VlasnikId)).ReturnsAsync(OglasId);

        // Act
        var rezultat = await _kontroler.ObjaviOglasMajstora(dto, null);

        // Assert
        ProvjeriDetalje(rezultat, "OglasMajstora");
        Assert.IsNull(dto.Slika);
        Assert.AreEqual("Oglas majstora je uspješno objavljen.",
            _kontroler.TempData["Success"]);
        _majstori.Verify(s => s.ObjaviOglas(dto, VlasnikId), Times.Once());
    }

    [TestMethod]
    public async Task ObjaviOglasMajstora_NeispravanModel_VracaObrazacBezObjave()
    {
        // Arrange
        var dto = new ObjaviOglasMajstoraDto();
        _kontroler.ModelState.AddModelError("Naslov", "Naslov je obavezan.");

        // Act
        var rezultat = await _kontroler.ObjaviOglasMajstora(dto, null);

        // Assert
        ProvjeriObrazac(_kontroler, _kategorije, _mjesta, rezultat, dto, "majstora");
        _majstori.Verify(s => s.ObjaviOglas(
            It.IsAny<ObjaviOglasMajstoraDto>(), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    [DataRow(true, "Poslovno pravilo nije ispunjeno.")]
    [DataRow(false, GreskaObjave)]
    public async Task ObjaviOglasMajstora_IzuzetakFasade_VracaOdgovarajucuPoruku(
        bool poslovnaGreska, string ocekivanaPoruka)
    {
        // Arrange
        var dto = new ObjaviOglasMajstoraDto();
        Exception greska = poslovnaGreska
            ? new InvalidOperationException("Poslovno pravilo nije ispunjeno.")
            : new Exception("Objava nije uspjela.");
        _majstori.Setup(s => s.ObjaviOglas(dto, VlasnikId)).ThrowsAsync(greska);

        // Act
        var rezultat = await _kontroler.ObjaviOglasMajstora(dto, null);

        // Assert
        ProvjeriObrazac(_kontroler, _kategorije, _mjesta, rezultat, dto, "majstora");
        Assert.AreEqual(ocekivanaPoruka, _kontroler.TempData["Error"]);
    }

    [TestMethod]
    public async Task ObjaviOglasRadnoMjesto_UvjetiSaPraznimStavkama_UklanjaPrazneUvjete()
    {
        // Arrange
        TestHelpers.PrijaviKorisnika(_kontroler, VlasnikId, "Firma");
        var dto = new ObjaviOglasRadnoMjestoDto { Uvjeti = new List<string> { "", "Iskustvo", " " } };
        _radnaMjesta.Setup(s => s.ObjaviOglas(dto, VlasnikId)).ReturnsAsync(OglasId);

        // Act
        await _kontroler.ObjaviOglasRadnoMjesto(dto, null);

        // Assert
        CollectionAssert.AreEqual(new[] { "Iskustvo" }, dto.Uvjeti.ToArray());
    }

    [TestMethod]
    public async Task ObjaviOglasRadnoMjesto_UvjetiNisuPoslani_PostavljaPraznuListuUvjeta()
    {
        // Arrange
        TestHelpers.PrijaviKorisnika(_kontroler, VlasnikId, "Firma");
        var dto = new ObjaviOglasRadnoMjestoDto { Uvjeti = null! };
        _radnaMjesta.Setup(s => s.ObjaviOglas(dto, VlasnikId)).ReturnsAsync(OglasId);

        // Act
        await _kontroler.ObjaviOglasRadnoMjesto(dto, null);

        // Assert
        CollectionAssert.AreEqual(Array.Empty<string>(), dto.Uvjeti.ToArray());
    }

    [TestMethod]
    public async Task ObjaviOglasRadnoMjesto_IspravanModel_ObjavljujeOglasIPrikazujeDetalje()
    {
        // Arrange
        TestHelpers.PrijaviKorisnika(_kontroler, VlasnikId, "Firma");
        var dto = new ObjaviOglasRadnoMjestoDto { Uvjeti = new List<string> { "Iskustvo" } };
        _radnaMjesta.Setup(s => s.ObjaviOglas(dto, VlasnikId)).ReturnsAsync(OglasId);

        // Act
        var rezultat = await _kontroler.ObjaviOglasRadnoMjesto(dto, null);

        // Assert: oglas je objavljen i korisnik ide na njegove detalje sa porukom o uspjehu
        _radnaMjesta.Verify(s => s.ObjaviOglas(dto, VlasnikId), Times.Once());
        ProvjeriDetalje(rezultat, "OglasRadnoMjesto");
        Assert.AreEqual("Oglas radnog mjesta je uspješno objavljen.", _kontroler.TempData["Success"]);
    }

    [TestMethod]
    public async Task ObjaviOglasRadnoMjesto_NeispravanModel_VracaObrazacBezObjave()
    {
        // Arrange
        var dto = new ObjaviOglasRadnoMjestoDto();
        _kontroler.ModelState.AddModelError("Naslov", "Naslov je obavezan.");

        // Act
        var rezultat = await _kontroler.ObjaviOglasRadnoMjesto(dto, null);

        // Assert
        ProvjeriObrazac(_kontroler, _kategorije, _mjesta, rezultat, dto, "radnomjesto");
        _radnaMjesta.Verify(s => s.ObjaviOglas(
            It.IsAny<ObjaviOglasRadnoMjestoDto>(), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    public async Task ObjaviOglasRadnoMjesto_IzuzetakFasade_VracaObrazacSPorukom()
    {
        // Arrange
        var dto = new ObjaviOglasRadnoMjestoDto();
        _radnaMjesta.Setup(s => s.ObjaviOglas(dto, VlasnikId))
            .ThrowsAsync(new Exception("Objava nije uspjela."));

        // Act
        var rezultat = await _kontroler.ObjaviOglasRadnoMjesto(dto, null);

        // Assert
        ProvjeriObrazac(_kontroler, _kategorije, _mjesta, rezultat, dto, "radnomjesto");
        Assert.AreEqual(GreskaObjave, _kontroler.TempData["Error"]);
    }
}