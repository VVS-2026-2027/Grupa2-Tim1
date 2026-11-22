using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using PopravkaBa.Application.Services.Interface;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using PopravkaBa.Web.Controllers;
using PopravkaBa.Web.Models.ViewModels;
using static PopravkaBa.Tests.VerifikacijaFirme.VerifikacijaFirmeRepositoryTestHelper;

namespace PopravkaBa.Tests.VerifikacijaFirme;

[TestClass]
[TestCategory("Controller")]
public class AdminControllerTests
{
    private Mock<IVerifikacijaFirmeService> _verifikacija = null!;
    private Mock<IRecenzijaService> _recenzije = null!;
    private Mock<IEmailSender> _email = null!;
    private AdminController _kontroler = null!;

    [TestInitialize]
    public void Postavi()
    {
        _verifikacija = new Mock<IVerifikacijaFirmeService>();
        _recenzije = new Mock<IRecenzijaService>();
        _email = new Mock<IEmailSender>();
        _verifikacija.Setup(v => v.DajZahtjeveNaCekanjuAsync()).ReturnsAsync(Array.Empty<EmailVerifikacijaFirme>());
        _recenzije.Setup(r => r.DajPrijavljeneRecenzije()).ReturnsAsync(Array.Empty<Recenzija>());

        _kontroler = new AdminController(_verifikacija.Object, _recenzije.Object, _email.Object,
            NullLogger<AdminController>.Instance);
        TestHelpers.PrijaviKorisnika(_kontroler, "admin", "Administrator");
    }

    private static Firma FirmaSaKategorijom() => new()
    {
        Id = "f1", UserName = "haso.instalacije", Email = "firma@popravka.ba", NazivFirme = "Haso instalacije d.o.o.",
        Kategorije = new List<IzvrsilacKategorija>
        {
            new() { Kategorija = null! },
            new() { Kategorija = new Kategorija { Naziv = "Vodoinstalacije" } }
        }
    };

    private static Recenzija PrijavljenaRecenzija(IzvrsilacUsluge? izvrsilac) => new()
    {
        RecenzijaID = 3, Komentar = "Nije došao", Izvrsilac = izvrsilac!, Prijavljena = true,
        RazlogPrijave = "Uvredljiv sadržaj", DatumPrijave = new DateTime(2026, 11, 18, 0, 0, 0, DateTimeKind.Utc)
    };

    private async Task<AdminDashboardViewModel> Index() =>
        (AdminDashboardViewModel)((ViewResult)await _kontroler.Index()).Model!;

    // ---------- Index ----------

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_ZahtjevNaCekanju_MapiraPodatkeFirme()
    {
        // Arrange
        var zahtjev = Zahtjev(1, "f1");
        zahtjev.Firma = FirmaSaKategorijom();
        _verifikacija.Setup(v => v.DajZahtjeveNaCekanjuAsync()).ReturnsAsync(new[] { zahtjev });

        // Act
        var stavka = (await Index()).ZahtjeviVerifikacije.Single();

        // Assert
        Assert.AreEqual(1, stavka.VerifikacioniId);
        Assert.AreEqual("firma@popravka.ba", stavka.Email);
        Assert.AreEqual("haso.instalacije", stavka.Username);
        Assert.AreEqual("logo.png", stavka.Logotip);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_FirmaSaViseKategorija_PrikazujePrvuPostojecuKategoriju()
    {
        // Arrange
        var zahtjev = Zahtjev(1, "f1");
        zahtjev.Firma = FirmaSaKategorijom();
        _verifikacija.Setup(v => v.DajZahtjeveNaCekanjuAsync()).ReturnsAsync(new[] { zahtjev });

        // Act
        var stavka = (await Index()).ZahtjeviVerifikacije.Single();

        // Assert
        Assert.AreEqual("Vodoinstalacije", stavka.Kategorija);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_ZahtjevBezUcitaneFirme_NeBacaIzuzetak()
    {
        // Arrange
        _verifikacija.Setup(v => v.DajZahtjeveNaCekanjuAsync()).ReturnsAsync(new[] { Zahtjev(1, "f1") });

        // Act
        var stavka = (await Index()).ZahtjeviVerifikacije.Single();

        // Assert
        Assert.IsNull(stavka.Email);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_PrijavljenaRecenzija_MapiraRecenzijuIIzvrsioca()
    {
        // Arrange
        var izvrsilac = new Majstor { UserName = "mujo", Ime = "Mujo", Prezime = "Mujić" };
        _recenzije.Setup(r => r.DajPrijavljeneRecenzije()).ReturnsAsync(new[] { PrijavljenaRecenzija(izvrsilac) });

        // Act
        var stavka = (await Index()).PrijavljeneRecenzije.Single();

        // Assert
        Assert.AreEqual(3, stavka.RecenzijaId);
        Assert.AreEqual("Mujo Mujić", stavka.IzvrsilacIme);
        Assert.AreEqual("mujo", stavka.IzvrsilacUsername);
        Assert.AreEqual("Uvredljiv sadržaj", stavka.Razlog);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task Index_RecenzijaBezIzvrsioca_PrikazujeNepoznato()
    {
        // Arrange
        _recenzije.Setup(r => r.DajPrijavljeneRecenzije()).ReturnsAsync(new[] { PrijavljenaRecenzija(null) });

        // Act
        var stavka = (await Index()).PrijavljeneRecenzije.Single();

        // Assert
        Assert.AreEqual("Nepoznato", stavka.IzvrsilacIme);
    }

    // ---------- ObradiVerifikaciju ----------

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObradiVerifikaciju_ZahtjevNijePronadjen_PostavljaGreskuBezEmaila()
    {
        // Arrange
        _verifikacija.Setup(v => v.ObradiZahtjevAsync(9, true)).ReturnsAsync((EmailVerifikacijaFirme?)null);

        // Act
        var rezultat = await _kontroler.ObradiVerifikaciju(9, true);

        // Assert
        Assert.AreEqual("Zahtjev za verifikaciju nije pronađen ili je već obrađen.", _kontroler.TempData["Greska"]);
        _email.Verify(e => e.PosaljiEmailAsync(It.IsAny<EmailVerifikacijaFirme>(), It.IsAny<bool>()), Times.Never);
        Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObradiVerifikaciju_ObradjenZahtjev_ObavjestavaFirmuEmailom()
    {
        // Arrange
        var zahtjev = Zahtjev(1, "f1", Status.Prihvaceno);
        _verifikacija.Setup(v => v.ObradiZahtjevAsync(1, true)).ReturnsAsync(zahtjev);

        // Act
        await _kontroler.ObradiVerifikaciju(1, true);

        // Assert
        _email.Verify(e => e.PosaljiEmailAsync(zahtjev, false), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    [DataRow(true, "Firma \"Firma f1\" je verificirana.")]
    [DataRow(false, "Zahtjev firme \"Firma f1\" je odbijen.")]
    public async Task ObradiVerifikaciju_OdobrenIliOdbijen_PrikazujePorukuIshoda(bool odobri, string poruka)
    {
        // Arrange
        _verifikacija.Setup(v => v.ObradiZahtjevAsync(1, odobri)).ReturnsAsync(Zahtjev(1, "f1"));

        // Act
        await _kontroler.ObradiVerifikaciju(1, odobri);

        // Assert
        Assert.AreEqual(poruka, _kontroler.TempData["Uspjeh"]);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObradiVerifikaciju_SlanjeEmailaNeuspjesno_ObradaIpakUspijeva()
    {
        // Arrange
        var zahtjev = Zahtjev(1, "f1");
        _verifikacija.Setup(v => v.ObradiZahtjevAsync(1, true)).ReturnsAsync(zahtjev);
        _email.Setup(e => e.PosaljiEmailAsync(zahtjev, false)).ThrowsAsync(new InvalidOperationException("SMTP nedostupan"));

        // Act
        await _kontroler.ObradiVerifikaciju(1, true);

        // Assert
        Assert.IsNotNull(_kontroler.TempData["Uspjeh"]);
    }

    // ---------- UkloniRecenziju i OdbaciPrijavu ----------

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UkloniRecenziju_PostojecaRecenzija_BriseRecenziju()
    {
        // Act
        await _kontroler.UkloniRecenziju(3);

        // Assert
        _recenzije.Verify(r => r.ObrisiRecenziju(3), Times.Once);
        Assert.AreEqual("Recenzija je uklonjena.", _kontroler.TempData["Uspjeh"]);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UkloniRecenziju_NepostojecaRecenzija_PostavljaGresku()
    {
        // Arrange
        _recenzije.Setup(r => r.ObrisiRecenziju(99)).ThrowsAsync(new KeyNotFoundException());

        // Act
        await _kontroler.UkloniRecenziju(99);

        // Assert
        Assert.AreEqual("Recenzija nije pronađena.", _kontroler.TempData["Greska"]);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task OdbaciPrijavu_PostojecaRecenzija_OdbacujePrijavu()
    {
        // Act
        await _kontroler.OdbaciPrijavu(3);

        // Assert
        _recenzije.Verify(r => r.OdbaciPrijavu(3), Times.Once);
        Assert.AreEqual("Prijava recenzije je odbačena, recenzija ostaje objavljena.", _kontroler.TempData["Uspjeh"]);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task OdbaciPrijavu_NepostojecaRecenzija_PostavljaGresku()
    {
        // Arrange
        _recenzije.Setup(r => r.OdbaciPrijavu(99)).ThrowsAsync(new KeyNotFoundException());

        // Act
        await _kontroler.OdbaciPrijavu(99);

        // Assert
        Assert.AreEqual("Recenzija nije pronađena.", _kontroler.TempData["Greska"]);
    }
}
