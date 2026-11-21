using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Interface;
using PopravkaBa.Domain.Models;
using static PopravkaBa.Tests.PrijaveOglasRadnoMjesto.PrijavaOglasControllerTestHelper;

namespace PopravkaBa.Tests.PrijaveOglasRadnoMjesto;

[TestClass]
[TestCategory("Controller")]
public class PrijavaOglasControllerTests
{

    private Mock<IPrijavaOglasService> _servis = null!;
    private PrijavaOglasController _kontroler = null!;

    [TestInitialize]
    public void PripremiTest()
    {
        _servis = new Mock<IPrijavaOglasService>();
        var userManager = TestHelpers.MockUserManager();

        userManager
            .Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns((ClaimsPrincipal korisnik) =>
                korisnik.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        _kontroler = new PrijavaOglasController(
            _servis.Object,
            Mock.Of<IOglasRadnoMjestoService>(),
            userManager.Object,
            NullLogger<PrijavaOglasController>.Instance);

        TestHelpers.PrijaviKorisnika(_kontroler, MajstorId, "Majstor");
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task Index_ZadaniOglas_VracaPrijaveIOglasId()
    {
        // Arrange
        var prijave = new[] { NovaPrijava() };
        _servis.Setup(s => s.DajSvePrijave(OglasId)).ReturnsAsync(prijave);

        // Act
        var rezultat = await _kontroler.Index(OglasId);

        // Assert
        Assert.AreSame(prijave, ((ViewResult)rezultat).Model);
        Assert.AreEqual(OglasId, (int)_kontroler.ViewData["OglasId"]!);
        _servis.Verify(s => s.DajSvePrijave(OglasId), Times.Once());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task Posalji_PrijavljeniMajstor_SaljePrijavuIPreusmjerava()
    {
        // Arrange
        _servis.Setup(s => s.KreirajPrijavu(It.IsAny<KreirajPrijavaRadnoMjestoDto>()))
            .Returns(Task.CompletedTask);

        // Act
        var rezultat = await _kontroler.Posalji(OglasId);

        // Assert
        ProvjeriDetaljeOglasa(rezultat, OglasId);
        Assert.AreEqual("Prijava je uspješno poslana.", _kontroler.TempData["Success"]);
        _servis.Verify(s => s.KreirajPrijavu(It.Is<KreirajPrijavaRadnoMjestoDto>(dto =>
            dto.OglasID == OglasId && dto.MajstorID == MajstorId)), Times.Once());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    [DataRow(true, "Oglas više nije aktivan.")]
    [DataRow(false, "Došlo je do greške pri slanju prijave.")]
    public async Task Posalji_IzuzetakIzServisa_PrikazujeGreskuIPreusmjerava(
        bool poslovnaGreska, string ocekivanaPoruka)
    {
        // Arrange
        Exception greska = poslovnaGreska
            ? new InvalidOperationException("Oglas više nije aktivan.")
            : new Exception("Neočekivana greška.");
        _servis.Setup(s => s.KreirajPrijavu(It.IsAny<KreirajPrijavaRadnoMjestoDto>()))
            .ThrowsAsync(greska);

        // Act
        var rezultat = await _kontroler.Posalji(OglasId);

        // Assert
        ProvjeriDetaljeOglasa(rezultat, OglasId);
        Assert.AreEqual(ocekivanaPoruka, _kontroler.TempData["Error"]);
        Assert.IsNull(_kontroler.TempData["Success"]);
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    [DataRow(nameof(PrijavaOglasController.ObrisiPrijavu))]
    [DataRow(nameof(PrijavaOglasController.PrihvatiPrijava))]
    [DataRow(nameof(PrijavaOglasController.OdbijPrijavu))]
    public async Task GetAkcija_PostojecaPrijava_VracaPogledSPrijavom(string akcija)
    {
        // Arrange
        var prijava = PostaviPrijavu(_servis);

        // Act
        var rezultat = await PozoviGetAkciju(_kontroler, akcija);

        // Assert
        Assert.AreSame(prijava, ((ViewResult)rezultat).Model);
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    [DataRow(nameof(PrijavaOglasController.ObrisiPrijavu))]
    [DataRow(nameof(PrijavaOglasController.PrihvatiPrijava))]
    [DataRow(nameof(PrijavaOglasController.OdbijPrijavu))]
    public async Task GetAkcija_NepostojecaPrijava_VracaNotFound(string akcija)
    {
        // Arrange
        PostaviNepostojecuPrijavu(_servis);

        // Act
        var rezultat = await PozoviGetAkciju(_kontroler, akcija);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task ObrisanaPrijava_PostojecaPrijava_BriseIPreusmjerava()
    {
        // Arrange
        PostaviPrijavu(_servis);
        _servis.Setup(s => s.ObrisiPrijavu(PrijavaId)).Returns(Task.CompletedTask);

        // Act
        var rezultat = await _kontroler.ObrisanaPrijava(PrijavaId);

        // Assert
        ProvjeriDetaljeOglasa(rezultat, OglasId);
        Assert.AreEqual("Prijava je obrisana.", _kontroler.TempData["Success"]);
        _servis.Verify(s => s.ObrisiPrijavu(PrijavaId), Times.Once());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task ObrisanaPrijava_NepostojecaPrijava_VracaNotFoundBezBrisanja()
    {
        // Arrange
        PostaviNepostojecuPrijavu(_servis);

        // Act
        var rezultat = await _kontroler.ObrisanaPrijava(PrijavaId);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        _servis.Verify(s => s.ObrisiPrijavu(It.IsAny<int>()), Times.Never());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task ObrisanaPrijava_PrijavaNestanePriBrisanju_VracaNotFound()
    {
        // Arrange
        PostaviPrijavu(_servis);
        _servis.Setup(s => s.ObrisiPrijavu(PrijavaId))
            .ThrowsAsync(new KeyNotFoundException());

        // Act
        var rezultat = await _kontroler.ObrisanaPrijava(PrijavaId);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        _servis.Verify(s => s.ObrisiPrijavu(PrijavaId), Times.Once());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    [DataRow(false, false, "Prijava je prihvaćena.")]
    [DataRow(true, false, "Prijava je prihvaćena.")]
    [DataRow(false, true, "Prijava je prihvaćena. Popis je popunjen - sve preostale prijave su automatski odbijene.")]
    [DataRow(true, true, "Prijava je prihvaćena. Popis je popunjen - sve preostale prijave su automatski odbijene.")]
    public async Task PrihvacenaPrijava_PostojecaPrijava_PrikazujePorukuPremaPopunjenosti(
        bool direktno, bool oglasPopunjen, string ocekivanaPoruka)
    {
        // Arrange
        PostaviPrijavu(_servis);
        _servis.Setup(s => s.PrihvatiPonudu(PrijavaId)).ReturnsAsync(oglasPopunjen);

        // Act
        var rezultat = await PozoviPrihvatanje(_kontroler, direktno);

        // Assert
        ProvjeriDetaljeOglasa(rezultat, OglasId);
        Assert.AreEqual(ocekivanaPoruka, _kontroler.TempData["Success"]);
        _servis.Verify(s => s.PrihvatiPonudu(PrijavaId), Times.Once());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    [DataRow(false)]
    [DataRow(true)]
    public async Task PrihvacenaPrijava_NepostojecaPrijava_VracaNotFound(bool direktno)
    {
        // Arrange
        PostaviNepostojecuPrijavu(_servis);

        // Act
        var rezultat = await PozoviPrihvatanje(_kontroler, direktno);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        _servis.Verify(s => s.PrihvatiPonudu(It.IsAny<int>()), Times.Never());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    [DataRow(false)]
    [DataRow(true)]
    public async Task PrihvacenaPrijava_IzuzetakIzServisa_PrikazujeGreskuIPreusmjerava(
        bool direktno)
    {
        // Arrange
        PostaviPrijavu(_servis);
        _servis.Setup(s => s.PrihvatiPonudu(PrijavaId))
            .ThrowsAsync(new InvalidOperationException());

        // Act
        var rezultat = await PozoviPrihvatanje(_kontroler, direktno);

        // Assert
        ProvjeriDetaljeOglasa(rezultat, PrijavaId);
        Assert.AreEqual("Greška pri prihvatanju prijave.", _kontroler.TempData["Error"]);
        Assert.IsNull(_kontroler.TempData["Success"]);
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task OdbijenaPrijava_PostojecaPrijava_OdbijaIPreusmjerava()
    {
        // Arrange
        PostaviPrijavu(_servis);
        _servis.Setup(s => s.OdbijPrijavu(PrijavaId)).Returns(Task.CompletedTask);

        // Act
        var rezultat = await _kontroler.OdbijenaPrijava(PrijavaId);

        // Assert
        ProvjeriDetaljeOglasa(rezultat, OglasId);
        Assert.AreEqual("Prijava je odbijena.", _kontroler.TempData["Success"]);
        _servis.Verify(s => s.OdbijPrijavu(PrijavaId), Times.Once());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task OdbijenaPrijava_NepostojecaPrijava_VracaNotFound()
    {
        // Arrange
        PostaviNepostojecuPrijavu(_servis);

        // Act
        var rezultat = await _kontroler.OdbijenaPrijava(PrijavaId);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        _servis.Verify(s => s.OdbijPrijavu(It.IsAny<int>()), Times.Never());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task OdbijenaPrijava_IzuzetakIzServisa_PrikazujeGreskuIPreusmjerava()
    {
        // Arrange
        PostaviPrijavu(_servis);
        _servis.Setup(s => s.OdbijPrijavu(PrijavaId))
            .ThrowsAsync(new InvalidOperationException());

        // Act
        var rezultat = await _kontroler.OdbijenaPrijava(PrijavaId);

        // Assert
        ProvjeriDetaljeOglasa(rezultat, PrijavaId);
        Assert.AreEqual("Greška pri odbijanju prijave.", _kontroler.TempData["Error"]);
        Assert.IsNull(_kontroler.TempData["Success"]);
    }
}