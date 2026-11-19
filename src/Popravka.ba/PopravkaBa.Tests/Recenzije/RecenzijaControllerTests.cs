using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Interface;
using PopravkaBa.Domain.Models;
using PopravkaBa.Web.Controllers;
using static PopravkaBa.Tests.Recenzije.RecenzijaControllerTestPomoc;

namespace PopravkaBa.Tests.Recenzije;

[TestClass]
public class RecenzijaControllerTests
{

    private Mock<IRecenzijaService> _servis = null!;
    private Mock<UserManager<ApplicationUser>> _userManager = null!;
    private RecenzijaController _kontroler = null!;

    [TestInitialize]
    public void PripremiTest()
    {
        _servis = new Mock<IRecenzijaService>();
        _userManager = TestHelpers.MockUserManager();
        _kontroler = new RecenzijaController(
            _servis.Object,
            _userManager.Object,
            NullLogger<RecenzijaController>.Instance);

        TestHelpers.NeprijavljenKorisnik(_kontroler);
        _userManager
            .Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns((ClaimsPrincipal korisnik) =>
                korisnik.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    [TestMethod]
    public async Task Index_PrazanId_VracaNotFound()
    {
        // Arrange
        const string prazanId = " ";

        // Act
        var rezultat = await _kontroler.Index(prazanId);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        _userManager.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    public async Task Index_NepostojeciIzvrsilac_VracaNotFound()
    {
        // Arrange
        _userManager.Setup(m => m.FindByIdAsync(IzvrsilacId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var rezultat = await _kontroler.Index(IzvrsilacId);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        _servis.Verify(s => s.DajRecenzijePoId(It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    [DataRow(IzvrsilacId, true)]
    [DataRow(KlijentId, false)]
    public async Task Index_PostojeciIzvrsilac_VracaRecenzijeIOznakuVlasnika(
        string korisnikId, bool ocekivaniVlasnik)
    {
        // Arrange
        var izvrsilac = PostaviIzvrsioca(_userManager);
        var recenzije = new[] { NovaRecenzija() };
        _servis.Setup(s => s.DajRecenzijePoId(IzvrsilacId)).ReturnsAsync(recenzije);
        TestHelpers.PrijaviKorisnika(_kontroler, korisnikId);

        // Act
        var rezultat = await _kontroler.Index(IzvrsilacId);

        // Assert
        var pogled = (ViewResult)rezultat;
        Assert.AreSame(recenzije, pogled.Model);
        Assert.AreEqual(izvrsilac.DisplayName, _kontroler.ViewData["IzvrsilacIme"]);
        Assert.AreEqual(KorisnickoIme, _kontroler.ViewData["IzvrsilacUsername"]);
        Assert.AreEqual(ocekivaniVlasnik, _kontroler.ViewData["JeVlasnik"]);
    }

    [TestMethod]
    public async Task Objavi_NepostojeciIzvrsilac_VracaNotFound()
    {
        // Arrange
        var dto = NovaObjava();
        _userManager.Setup(m => m.FindByIdAsync(IzvrsilacId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var rezultat = await _kontroler.Objavi(dto);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        _servis.Verify(s => s.ObjaviRecenziju(dto, It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    public async Task Objavi_NeispravanModel_VracaNaProfilSPorukom()
    {
        // Arrange
        PostaviIzvrsioca(_userManager);
        var dto = NovaObjava();
        _kontroler.ModelState.AddModelError(nameof(dto.Ocjena), "Neispravna ocjena.");

        // Act
        var rezultat = await _kontroler.Objavi(dto);

        // Assert
        ProvjeriPreusmjerenje(rezultat, "Profil", KorisnickoIme);
        Assert.AreEqual("Ocjena (1-5) i komentar su obavezni.", _kontroler.TempData["Error"]);
        _servis.Verify(s => s.ObjaviRecenziju(dto, It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    public async Task Objavi_IspravanModel_ObjavljujeZaPrijavljenogKlijenta()
    {
        // Arrange
        PostaviIzvrsioca(_userManager);
        TestHelpers.PrijaviKorisnika(_kontroler, KlijentId, "Klijent");
        var dto = NovaObjava();
        _servis.Setup(s => s.ObjaviRecenziju(dto, KlijentId)).Returns(Task.CompletedTask);

        // Act
        var rezultat = await _kontroler.Objavi(dto);

        // Assert
        ProvjeriPreusmjerenje(rezultat, "Profil", KorisnickoIme);
        Assert.AreEqual("Recenzija je objavljena. Hvala vam!", _kontroler.TempData["Success"]);
        _servis.Verify(s => s.ObjaviRecenziju(dto, KlijentId), Times.Once());
    }

    [TestMethod]
    [DataRow(true, "Klijent ne može ostaviti recenziju.")]
    [DataRow(false, "Došlo je do greške pri objavi recenzije.")]
    public async Task Objavi_IzuzetakIzServisa_VracaNaProfilSPorukom(
        bool poslovnaGreska, string ocekivanaPoruka)
    {
        // Arrange
        PostaviIzvrsioca(_userManager);
        TestHelpers.PrijaviKorisnika(_kontroler, KlijentId, "Klijent");
        var dto = NovaObjava();
        _servis.Setup(s => s.ObjaviRecenziju(dto, KlijentId))
            .ThrowsAsync(GreskaServisa(poslovnaGreska, "Klijent ne može ostaviti recenziju."));

        // Act
        var rezultat = await _kontroler.Objavi(dto);

        // Assert
        ProvjeriPreusmjerenje(rezultat, "Profil", KorisnickoIme);
        Assert.AreEqual(ocekivanaPoruka, _kontroler.TempData["Error"]);
    }

    [TestMethod]
    public async Task PrijaviGet_NepostojecaRecenzija_VracaNotFound()
    {
        // Arrange
        _servis.Setup(s => s.DajRecenzijuPoId(RecenzijaId)).ReturnsAsync((Recenzija?)null);

        // Act
        var rezultat = await _kontroler.Prijavi(RecenzijaId);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task PrijaviGet_TudjaRecenzija_VracaForbid()
    {
        // Arrange
        PostaviRecenziju(_servis);
        TestHelpers.PrijaviKorisnika(_kontroler, "m2", "Majstor");

        // Act
        var rezultat = await _kontroler.Prijavi(RecenzijaId);

        // Assert
        Assert.IsInstanceOfType<ForbidResult>(rezultat);
    }

    [TestMethod]
    public async Task PrijaviGet_VlastitaRecenzija_VracaFormu()
    {
        // Arrange
        var recenzija = PostaviRecenziju(_servis);
        TestHelpers.PrijaviKorisnika(_kontroler, IzvrsilacId, "Majstor");

        // Act
        var rezultat = await _kontroler.Prijavi(RecenzijaId);

        // Assert
        Assert.AreSame(recenzija, ((ViewResult)rezultat).Model);
    }

    [TestMethod]
    public async Task PrijaviPost_NepostojecaRecenzija_VracaNotFound()
    {
        // Arrange
        var dto = NovaPrijava();
        _servis.Setup(s => s.DajRecenzijuPoId(RecenzijaId)).ReturnsAsync((Recenzija?)null);

        // Act
        var rezultat = await _kontroler.Prijavi(dto);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        _servis.Verify(s => s.PrijaviRecenziju(dto), Times.Never());
    }

    [TestMethod]
    public async Task PrijaviPost_TudjaRecenzija_VracaForbid()
    {
        // Arrange
        PostaviRecenziju(_servis);
        TestHelpers.PrijaviKorisnika(_kontroler, "m2", "Majstor");
        var dto = NovaPrijava();

        // Act
        var rezultat = await _kontroler.Prijavi(dto);

        // Assert
        Assert.IsInstanceOfType<ForbidResult>(rezultat);
        _servis.Verify(s => s.PrijaviRecenziju(dto), Times.Never());
    }

    [TestMethod]
    [DataRow("Razlog je prekratak.", "Razlog je prekratak.")]
    [DataRow("", "Razlog prijave nije validan.")]
    public async Task PrijaviPost_NeispravanModel_VracaFormuSUnosomIPorukom(
        string validacijskaPoruka, string ocekivanaPoruka)
    {
        // Arrange
        var recenzija = PostaviRecenziju(_servis);
        TestHelpers.PrijaviKorisnika(_kontroler, IzvrsilacId, "Majstor");
        var dto = NovaPrijava();
        _kontroler.ModelState.AddModelError(nameof(dto.RazlogPrijave), validacijskaPoruka);

        // Act
        var rezultat = await _kontroler.Prijavi(dto);

        // Assert
        Assert.AreSame(recenzija, ((ViewResult)rezultat).Model);
        Assert.AreEqual(dto.RazlogPrijave, _kontroler.ViewData["RazlogPrijave"]);
        Assert.AreEqual(ocekivanaPoruka, _kontroler.TempData["Error"]);
        _servis.Verify(s => s.PrijaviRecenziju(dto), Times.Never());
    }

    [TestMethod]
    [DataRow(KorisnickoIme, "Profil")]
    [DataRow(null, "Home")]
    public async Task PrijaviPost_IspravanModel_PrijavljujeIPreusmjerava(
        string? korisnickoIme, string ocekivaniKontroler)
    {
        // Arrange
        PostaviRecenziju(_servis, korisnickoIme);
        TestHelpers.PrijaviKorisnika(_kontroler, IzvrsilacId, "Majstor");
        var dto = NovaPrijava();
        _servis.Setup(s => s.PrijaviRecenziju(dto)).Returns(Task.CompletedTask);

        // Act
        var rezultat = await _kontroler.Prijavi(dto);

        // Assert
        ProvjeriPreusmjerenje(rezultat, ocekivaniKontroler, korisnickoIme);
        Assert.AreEqual(
            "Recenzija je prijavljena. Administrator će pregledati prijavu.",
            _kontroler.TempData["Success"]);
        _servis.Verify(s => s.PrijaviRecenziju(dto), Times.Once());
    }

    [TestMethod]
    [DataRow(true, "Recenzija je već prijavljena.")]
    [DataRow(false, "Došlo je do greške pri prijavi recenzije.")]
    public async Task PrijaviPost_IzuzetakIzServisa_VracaNaProfilSPorukom(
        bool poslovnaGreska, string ocekivanaPoruka)
    {
        // Arrange
        PostaviRecenziju(_servis);
        TestHelpers.PrijaviKorisnika(_kontroler, IzvrsilacId, "Majstor");
        var dto = NovaPrijava();
        _servis.Setup(s => s.PrijaviRecenziju(dto))
            .ThrowsAsync(GreskaServisa(poslovnaGreska, "Recenzija je već prijavljena."));

        // Act
        var rezultat = await _kontroler.Prijavi(dto);

        // Assert
        ProvjeriPreusmjerenje(rezultat, "Profil", KorisnickoIme);
        Assert.AreEqual(ocekivanaPoruka, _kontroler.TempData["Error"]);
    }
}