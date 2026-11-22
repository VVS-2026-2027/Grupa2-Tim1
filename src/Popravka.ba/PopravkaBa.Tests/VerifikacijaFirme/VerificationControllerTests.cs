using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Interface;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using PopravkaBa.Web.Controllers;
using PopravkaBa.Web.Models.ViewModels;
using static PopravkaBa.Tests.VerifikacijaFirme.VerificationControllerTestHelper;
using static PopravkaBa.Tests.VerifikacijaFirme.VerifikacijaFirmeRepositoryTestHelper;

namespace PopravkaBa.Tests.VerifikacijaFirme;

[TestClass]
[TestCategory("Controller")]
public class VerificationControllerTests
{
    private Mock<UserManager<ApplicationUser>> _userManager = null!;
    private Mock<IVerifikacijaFirmeService> _verifikacija = null!;
    private Mock<IEmailSender> _email = null!;
    private Mock<IFileStorage> _storage = null!;
    private VerificationController _kontroler = null!;
    private Firma _firma = null!;
    private EmailVerifikacijaFirme _podnesen = null!;

    [TestInitialize]
    public void Postavi()
    {
        _userManager = TestHelpers.MockUserManager();
        _verifikacija = new Mock<IVerifikacijaFirmeService>();
        _email = new Mock<IEmailSender>();
        _storage = new Mock<IFileStorage>();
        _firma = NeverificiranaFirma();
        _podnesen = Zahtjev(1, "f1");

        _userManager.PrijavljenKorisnik(_firma);
        _storage.Setup(s => s.SpremiPrivate(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream _, string ime, string _, CancellationToken _) => ime);
        _storage.Setup(s => s.SpremiPublic(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.test/logo.png");
        _storage.Setup(s => s.GetSignedURL(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns((string kljuc, TimeSpan _) => $"potpisano:{kljuc}");
        _verifikacija.Setup(v => v.PodnesiZahtjevAsync(It.IsAny<PodnesiVerifikacijuDto>(), "f1", AdminEmail))
            .ReturnsAsync(_podnesen);

        var konfiguracija = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["SeedData:AdminEmail"] = AdminEmail })
            .Build();

        _kontroler = new VerificationController(_userManager.Object, _verifikacija.Object, _email.Object,
            _storage.Object, konfiguracija, NullLogger<VerificationController>.Instance);
        TestHelpers.PrijaviKorisnika(_kontroler, "f1", "Firma");
    }

    // ---------- Index (GET) ----------

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_Get_KorisnikNijeFirma_VracaForbid()
    {
        // Arrange
        _userManager.PrijavljenKorisnik(new Klijent { Id = "k1" });

        // Act
        var rezultat = await _kontroler.Index();

        // Assert
        Assert.IsInstanceOfType<ForbidResult>(rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_Get_Firma_PopunjavaFormuPodacimaFirme()
    {
        // Act
        var vm = (VerifikacijaFirmeViewModel)((ViewResult)await _kontroler.Index()).Model!;

        // Assert
        Assert.AreEqual(_firma.NazivFirme, vm.NazivFirme);
        Assert.AreEqual(_firma.WebStranica, vm.WebStranica);
        Assert.AreEqual(_firma.PhoneNumber, vm.KontaktTelefon);
        Assert.AreEqual(_firma.Email, vm.OdgovornaOsobaEmail);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    [DataRow(Status.NaCekanju, true)]
    [DataRow(Status.Odbijeno, false)]
    public async Task Index_Get_ZadnjiZahtjev_OznacavaDaLiJeNaCekanju(Status status, bool naCekanju)
    {
        // Arrange
        _verifikacija.Setup(v => v.DajZadnjiZahtjevAsync("f1")).ReturnsAsync(Zahtjev(1, "f1", status));

        // Act
        await _kontroler.Index();

        // Assert
        Assert.AreEqual(naCekanju, _kontroler.ViewData["ZahtjevNaCekanju"]);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_Get_VerificiranaFirma_OznacavaDaJeVerificirana()
    {
        // Arrange
        _firma.AdminVerificirao = true;

        // Act
        await _kontroler.Index();

        // Assert
        Assert.AreEqual(true, _kontroler.ViewData["Verificirana"]);
    }

    // ---------- Index (POST): kad se zahtjev ne smije podnijeti ----------

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_Post_KorisnikNijeFirma_VracaForbid()
    {
        // Arrange
        _userManager.PrijavljenKorisnik(new Klijent { Id = "k1" });

        // Act
        var rezultat = await _kontroler.Index(IspravnaForma(), CancellationToken.None);

        // Assert
        Assert.IsInstanceOfType<ForbidResult>(rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_Post_FirmaVecVerificirana_NePodnosiNoviZahtjev()
    {
        // Arrange
        _firma.AdminVerificirao = true;

        // Act
        await _kontroler.Index(IspravnaForma(), CancellationToken.None);

        // Assert
        _verifikacija.Verify(v => v.PodnesiZahtjevAsync(It.IsAny<PodnesiVerifikacijuDto>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_Post_ZahtjevVecNaCekanju_NePodnosiNoviZahtjev()
    {
        // Arrange
        _verifikacija.Setup(v => v.DajZadnjiZahtjevAsync("f1")).ReturnsAsync(Zahtjev(1, "f1"));

        // Act
        var rezultat = await _kontroler.Index(IspravnaForma(), CancellationToken.None);

        // Assert
        Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
        _verifikacija.Verify(v => v.PodnesiZahtjevAsync(It.IsAny<PodnesiVerifikacijuDto>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ---------- Index (POST): validacija dokumenata ----------

    public static IEnumerable<object[]> Dokumenti =>
        DataDrivenInputParser.CitajXml<string, long, bool>("TestData/xml/VerificationController_dokumenti.xml");

    [TestMethod]
    [TestCategory("DataDriven")]
    [Owner("Aid Mustafić")]
    [DynamicData(nameof(Dokumenti))]
    public async Task Index_Post_RjesenjeIzXml_GreskaZaNedozvoljenFormatIliVelicinu(string ime, long velicina, bool greska)
    {
        // Arrange
        var forma = IspravnaForma();
        forma.RjesenjeFajl = Fajl(ime, velicina);

        // Act
        await _kontroler.Index(forma, CancellationToken.None);

        // Assert
        Assert.AreEqual(greska, _kontroler.ModelState.ContainsKey(nameof(forma.RjesenjeFajl)));
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_Post_BezObaveznihDokumenata_VracaFormuSaDvijeGreske()
    {
        // Arrange
        var forma = IspravnaForma();
        forma.RjesenjeFajl = null;
        forma.PoreznoUvjerenjeFajl = null;

        // Act
        var rezultat = await _kontroler.Index(forma, CancellationToken.None);

        // Assert
        Assert.AreSame(forma, ((ViewResult)rezultat).Model);
        Assert.AreEqual(2, _kontroler.ModelState.ErrorCount);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_Post_NevalidnaForma_NeSpremaDokumente()
    {
        // Arrange
        var forma = IspravnaForma();
        forma.PoreznoUvjerenjeFajl = Fajl("porezno.exe");

        // Act
        await _kontroler.Index(forma, CancellationToken.None);

        // Assert
        _storage.Verify(s => s.SpremiPrivate(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_Post_LicencaNedozvoljenogFormata_GreskaZaLicencu()
    {
        // Arrange
        var forma = IspravnaForma();
        forma.LicencaFajl = Fajl("licenca.zip");

        // Act
        await _kontroler.Index(forma, CancellationToken.None);

        // Assert
        Assert.IsTrue(_kontroler.ModelState.ContainsKey(nameof(forma.LicencaFajl)));
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    [DataRow("logo.pdf", 1024)]
    [DataRow("logo.png", 5 * 1024 * 1024 + 1)]
    public async Task Index_Post_NeispravanLogo_GreskaZaLogo(string ime, long velicina)
    {
        // Arrange
        var forma = IspravnaForma();
        forma.LogoFajl = Fajl(ime, velicina);

        // Act
        await _kontroler.Index(forma, CancellationToken.None);

        // Assert
        Assert.IsTrue(_kontroler.ModelState.ContainsKey(nameof(forma.LogoFajl)));
    }

    // ---------- Index (POST): uspješno podnošenje ----------

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_Post_IspravnaForma_SpremaDokumenteUPrivatniStorage()
    {
        // Act
        await _kontroler.Index(IspravnaForma(), CancellationToken.None);

        // Assert
        _storage.Verify(s => s.SpremiPrivate(It.IsAny<Stream>(), It.Is<string>(i => i.StartsWith("verifikacija/rjesenje-") && i.EndsWith(".pdf")),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _storage.Verify(s => s.SpremiPrivate(It.IsAny<Stream>(), It.Is<string>(i => i.StartsWith("verifikacija/porezno-")),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_Post_IspravnaForma_PodnosiZahtjevSaPodacimaFormeIAdminEmailom()
    {
        // Arrange
        var forma = IspravnaForma();

        // Act
        await _kontroler.Index(forma, CancellationToken.None);

        // Assert
        _verifikacija.Verify(v => v.PodnesiZahtjevAsync(It.Is<PodnesiVerifikacijuDto>(d =>
            d.NazivFirme == forma.NazivFirme &&
            d.JIB == forma.JIB &&
            d.OdgovornaOsobaEmail == forma.OdgovornaOsobaEmail &&
            d.Rjesenje.StartsWith("verifikacija/rjesenje-") &&
            d.LicencaDjelatnosti == null &&
            d.Logotip == null), "f1", AdminEmail), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_Post_PrilozeniLicencaILogo_SpremaIhIProsljedjuje()
    {
        // Arrange
        var forma = IspravnaForma();
        forma.LicencaFajl = Fajl("licenca.png");
        forma.LogoFajl = Fajl("logo.jpg");

        // Act
        await _kontroler.Index(forma, CancellationToken.None);

        // Assert
        _verifikacija.Verify(v => v.PodnesiZahtjevAsync(It.Is<PodnesiVerifikacijuDto>(d =>
            d.LicencaDjelatnosti!.StartsWith("verifikacija/licenca-") &&
            d.Logotip == "https://example.test/logo.png"), "f1", AdminEmail), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_Post_IspravnaForma_AdminuSaljePotpisaneLinkoveDokumenata()
    {
        // Arrange
        _podnesen.LicencaDjelatnosti = "licenca.pdf";

        // Act
        await _kontroler.Index(IspravnaForma(), CancellationToken.None);

        // Assert
        _email.Verify(e => e.PosaljiEmailAsync(It.Is<EmailVerifikacijaFirme>(z =>
            z.Rjesenje == "potpisano:rjesenje.pdf" &&
            z.PoreznoUvjerenje == "potpisano:porezno.pdf" &&
            z.LicencaDjelatnosti == "potpisano:licenca.pdf"), true), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_Post_ZahtjevBezLicence_LicencaUEmailuJeNull()
    {
        // Arrange
        _podnesen.LicencaDjelatnosti = "";

        // Act
        await _kontroler.Index(IspravnaForma(), CancellationToken.None);

        // Assert
        Assert.IsNull(_podnesen.LicencaDjelatnosti);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_Post_IspravnaForma_PrikazujePorukuIPreusmjerava()
    {
        // Act
        var rezultat = await _kontroler.Index(IspravnaForma(), CancellationToken.None);

        // Assert
        Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
        Assert.AreEqual("Zahtjev za verifikaciju je uspješno poslan. Odgovor ćete dobiti na e-mail.", _kontroler.TempData["Uspjeh"]);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_Post_SlanjeEmailaNeuspjesno_ZahtjevIpakPodnesen()
    {
        // Arrange
        _email.Setup(e => e.PosaljiEmailAsync(It.IsAny<EmailVerifikacijaFirme>(), true))
            .ThrowsAsync(new InvalidOperationException("SMTP nedostupan"));

        // Act
        await _kontroler.Index(IspravnaForma(), CancellationToken.None);

        // Assert
        Assert.IsNotNull(_kontroler.TempData["Uspjeh"]);
    }
}
