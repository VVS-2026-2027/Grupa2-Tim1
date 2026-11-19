using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Interface;
using PopravkaBa.Domain.Models;
using PopravkaBa.Web.Controllers;
using static PopravkaBa.Tests.PonudeUsluga.PonudaUslugeControllerTestPomoc;

namespace PopravkaBa.Tests.PonudeUsluga;

[TestClass]
public class PonudaUslugeControllerTests
{
    private const int PonudaId = 5;
    private const string IzvrsilacId = "m1";

    private Mock<IPonudaUslugeService> _servis = null!;
    private PonudaUslugeController _kontroler = null!;

    [TestInitialize]
    public void PripremiTest()
    {
        _servis = new Mock<IPonudaUslugeService>();
        var userManager = TestHelpers.MockUserManager();

        userManager
            .Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns((ClaimsPrincipal korisnik) =>
                korisnik.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        _kontroler = new PonudaUslugeController(
            _servis.Object,
            Mock.Of<IOglasUslugeService>(),
            userManager.Object,
            NullLogger<PonudaUslugeController>.Instance);

        TestHelpers.NeprijavljenKorisnik(_kontroler);
    }

    [TestMethod]
    public async Task Index_ZadaniOglas_VracaPonudeIOglasId()
    {
        // Arrange
        var ponude = new[] { new PonudaUsluge { ID = PonudaId } };
        _servis.Setup(s => s.DajSvePonudeOglasa(OglasId)).ReturnsAsync(ponude);

        // Act
        var rezultat = await _kontroler.Index(OglasId);

        // Assert
        Assert.AreSame(ponude, ((ViewResult)rezultat).Model);
        Assert.AreEqual(OglasId, (int)_kontroler.ViewData["Search"]!);
        _servis.Verify(s => s.DajSvePonudeOglasa(OglasId), Times.Once());
    }

    [TestMethod]
    public async Task AplicirajGet_ZadaniOglas_VracaObrazacSOglasId()
    {
        // Arrange
        const int oglasId = OglasId;

        // Act
        var rezultat = await _kontroler.Apliciraj(oglasId);

        // Assert
        Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.AreEqual(oglasId, (int)_kontroler.ViewData["OglasId"]!);
    }

    [TestMethod]
    public async Task AplicirajPost_NeispravanModel_VracaUneseniModel()
    {
        // Arrange
        var dto = NovaPonuda();
        _kontroler.ModelState.AddModelError(nameof(dto.Cijena), "Neispravna cijena.");

        // Act
        var rezultat = await _kontroler.Apliciraj(dto);

        // Assert
        Assert.AreSame(dto, ((ViewResult)rezultat).Model);
    }

    [TestMethod]
    public async Task AplicirajPost_IspravanModel_VracaPogledBezModela()
    {
        // Arrange
        var dto = NovaPonuda();

        // Act
        var rezultat = await _kontroler.Apliciraj(dto);

        // Assert
        Assert.IsNull(((ViewResult)rezultat).Model);
    }

    [TestMethod]
    public async Task PosaljiPonudu_NeispravanModel_PrikazujeGreskuIPreusmjerava()
    {
        // Arrange
        var dto = NovaPonuda();
        _kontroler.ModelState.AddModelError(nameof(dto.Cijena), "Neispravna cijena.");

        // Act
        var rezultat = await _kontroler.PosaljiPonudu(dto);

        // Assert
        ProvjeriDetaljeOglasa(rezultat);
        Assert.AreEqual("Molimo popunite sva obavezna polja.",
            _kontroler.TempData["Error"]);
        _servis.Verify(s => s.PosaljiPonudu(
            It.IsAny<KreirajPonudaUslugeDto>(), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    public async Task PosaljiPonudu_ZavrsetakPrijePocetka_PrikazujeGreskuIPreusmjerava()
    {
        // Arrange
        var dto = NovaPonuda(-1);

        // Act
        var rezultat = await _kontroler.PosaljiPonudu(dto);

        // Assert
        ProvjeriDetaljeOglasa(rezultat);
        Assert.AreEqual("Datum završetka mora biti nakon datuma početka.",
            _kontroler.TempData["Error"]);
        _servis.Verify(s => s.PosaljiPonudu(
            It.IsAny<KreirajPonudaUslugeDto>(), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(0)]
    [DataRow(1)]
    public async Task PosaljiPonudu_DozvoljenDatum_SaljePonuduZaPrijavljenogIzvrsioca(
        int? pomakDana)
    {
        // Arrange
        var dto = NovaPonuda(pomakDana);
        TestHelpers.PrijaviKorisnika(_kontroler, IzvrsilacId, "Majstor");
        _servis.Setup(s => s.PosaljiPonudu(dto, IzvrsilacId))
            .Returns(Task.CompletedTask);

        // Act
        var rezultat = await _kontroler.PosaljiPonudu(dto);

        // Assert
        ProvjeriDetaljeOglasa(rezultat);
        Assert.AreEqual("Ponuda je uspješno poslana.", _kontroler.TempData["Success"]);
        _servis.Verify(s => s.PosaljiPonudu(dto, IzvrsilacId), Times.Once());
    }

    [TestMethod]
    public async Task PosaljiPonudu_IzuzetakIzServisa_PrikazujeGreskuIPreusmjerava()
    {
        // Arrange
        var dto = NovaPonuda();
        TestHelpers.PrijaviKorisnika(_kontroler, IzvrsilacId, "Majstor");
        _servis.Setup(s => s.PosaljiPonudu(dto, IzvrsilacId))
            .ThrowsAsync(new InvalidOperationException("Slanje nije uspjelo."));

        // Act
        var rezultat = await _kontroler.PosaljiPonudu(dto);

        // Assert
        ProvjeriDetaljeOglasa(rezultat);
        Assert.AreEqual("Došlo je do greške pri slanju ponude.",
            _kontroler.TempData["Error"]);
        Assert.IsNull(_kontroler.TempData["Success"]);
        _servis.Verify(s => s.PosaljiPonudu(dto, IzvrsilacId), Times.Once());
    }

    [TestMethod]
    public async Task ObrisiPonudu_GetZaZadanuPonudu_VracaPogled()
    {
        // Arrange
        const int ponudaId = PonudaId;

        // Act
        var rezultat = await _kontroler.ObrisiPonudu(ponudaId);

        // Assert
        Assert.IsInstanceOfType<ViewResult>(rezultat);
    }

    [TestMethod]
    public async Task ObrisanaPonuda_PostZaZadanuPonudu_PreusmjeravaNaIndex()
    {
        // Arrange
        const int ponudaId = PonudaId;

        // Act
        var rezultat = await _kontroler.ObrisanaPonuda(ponudaId);

        // Assert
        Assert.AreEqual(nameof(PonudaUslugeController.Index),
            ((RedirectToActionResult)rezultat).ActionName);
    }

    [TestMethod]
    [DataRow(true, "Success", "Ponuda je prihvaćena. Sve ostale ponude su automatski odbijene.")]
    [DataRow(false, "Error", "Greška pri prihvatanju ponude.")]
    public async Task Prihvati_IshodServisa_PrikazujePorukuIPreusmjerava(
        bool uspjeh, string kljucPoruke, string poruka)
    {
        // Arrange
        _servis.Setup(s => s.PrihvatiPonudu(PonudaId))
            .Returns(() => IshodServisa(uspjeh));

        // Act
        var rezultat = await _kontroler.Prihvati(PonudaId, OglasId);

        // Assert
        ProvjeriDetaljeOglasa(rezultat);
        Assert.AreEqual(poruka, _kontroler.TempData[kljucPoruke]);
        _servis.Verify(s => s.PrihvatiPonudu(PonudaId), Times.Once());
    }

    [TestMethod]
    [DataRow(true, "Success", "Ponuda je odbijena.")]
    [DataRow(false, "Error", "Greška pri odbijanju ponude.")]
    public async Task Odbij_IshodServisa_PrikazujePorukuIPreusmjerava(
        bool uspjeh, string kljucPoruke, string poruka)
    {
        // Arrange
        _servis.Setup(s => s.OdbijPonudu(PonudaId))
            .Returns(() => IshodServisa(uspjeh));

        // Act
        var rezultat = await _kontroler.Odbij(PonudaId, OglasId);

        // Assert
        ProvjeriDetaljeOglasa(rezultat);
        Assert.AreEqual(poruka, _kontroler.TempData[kljucPoruke]);
        _servis.Verify(s => s.OdbijPonudu(PonudaId), Times.Once());
    }
}