using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Implementation;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using static PopravkaBa.Tests.VerifikacijaFirme.VerifikacijaFirmeServiceTestHelper;

namespace PopravkaBa.Tests.VerifikacijaFirme;

[TestClass]
public class VerifikacijaFirmeServiceTests
{
    private Mock<IVerifikacijaFirmeRepository> _repo = null!;
    private Mock<IFileStorage> _storage = null!;
    private VerifikacijaFirmeService _servis = null!;

    [TestInitialize]
    public void Postavi()
    {
        _repo = new Mock<IVerifikacijaFirmeRepository>();
        _storage = new Mock<IFileStorage>();
        _servis = new VerifikacijaFirmeService(_repo.Object, _storage.Object);
    }

    // ---------- Stub: servis samo vraća ono što dobije od repozitorija ----------

    [TestMethod]
    public async Task DajZadnjiZahtjevAsync_PostojiZahtjev_VracaZahtjevIzRepozitorija()
    {
        var zahtjev = ZahtjevNaCekanju();
        _repo.Setup(r => r.DajZadnjuZaFirmuAsync("f1")).ReturnsAsync(zahtjev);

        var rezultat = await _servis.DajZadnjiZahtjevAsync("f1");

        Assert.AreSame(zahtjev, rezultat);
    }

    [TestMethod]
    public async Task DajZahtjeveNaCekanjuAsync_DvaZahtjeva_VracaOba()
    {
        var zahtjevi = new[] { ZahtjevNaCekanju(), ZahtjevNaCekanju() };
        _repo.Setup(r => r.DajNaCekanjuAsync()).ReturnsAsync(zahtjevi);

        var rezultat = await _servis.DajZahtjeveNaCekanjuAsync();

        Assert.AreEqual(2, rezultat.Count());
    }

    // ---------- PodnesiZahtjevAsync ----------

    [TestMethod]
    public async Task PodnesiZahtjevAsync_IspravanDto_SnimaZahtjevNaCekanju()
    {
        var dto = new PodnesiVerifikacijuDto { NazivFirme = "Haso instalacije d.o.o.", JIB = "4200000000001", Logotip = "logo.png" };

        await _servis.PodnesiZahtjevAsync(dto, "f1", "admin@popravka.ba");

        _repo.Verify(r => r.DodajAsync(It.Is<EmailVerifikacijaFirme>(z =>
            z.FirmaID == "f1" &&
            z.AdminEmail == "admin@popravka.ba" &&
            z.JIB == "4200000000001" &&
            z.StatusVerifikacije == Status.NaCekanju)), Times.Once);
    }

    [TestMethod]
    public async Task PodnesiZahtjevAsync_BezLogotipa_LogotipJePrazanString()
    {
        var dto = new PodnesiVerifikacijuDto { NazivFirme = "Firma", Logotip = null };

        var rezultat = await _servis.PodnesiZahtjevAsync(dto, "f1", "admin@popravka.ba");

        Assert.AreEqual(string.Empty, rezultat.Logotip);
    }

    // ---------- ObradiZahtjevAsync: slučajevi kad se ništa ne radi ----------

    [TestMethod]
    public async Task ObradiZahtjevAsync_NepostojeciZahtjev_VracaNullINeSnima()
    {
        _repo.Setup(r => r.DajPoIdAsync(99)).ReturnsAsync((EmailVerifikacijaFirme?)null);

        var rezultat = await _servis.ObradiZahtjevAsync(99, odobri: true);

        Assert.IsNull(rezultat);
        _repo.Verify(r => r.SacuvajAsync(), Times.Never);
    }

    // Data-driven: zahtjev koji je već obrađen (bilo kojim statusom iz CSV-a) se ne obrađuje ponovo
    public static IEnumerable<object[]> VecObradjeniStatusi =>
        DataDrivenInputParser.CitajCsv<Status>("TestData/csv/VerifikacijaFirme_vecObradjeni.csv");

    [TestMethod]
    [DynamicData(nameof(VecObradjeniStatusi))]
    public async Task ObradiZahtjevAsync_VecObradjenZahtjev_VracaNull(Status status)
    {
        var zahtjev = ZahtjevNaCekanju();
        zahtjev.StatusVerifikacije = status;
        _repo.Setup(r => r.DajPoIdAsync(1)).ReturnsAsync(zahtjev);

        var rezultat = await _servis.ObradiZahtjevAsync(1, odobri: true);

        Assert.IsNull(rezultat);
    }

    // ---------- ObradiZahtjevAsync: odobravanje ----------

    [TestMethod]
    public async Task ObradiZahtjevAsync_Odobren_PrepisujePodatkeUFirmu()
    {
        var firma = new Firma { Slika = "stara.png" };
        _repo.Setup(r => r.DajPoIdAsync(1)).ReturnsAsync(ZahtjevNaCekanju(firma));

        var rezultat = await _servis.ObradiZahtjevAsync(1, odobri: true);

        Assert.AreEqual(Status.Prihvaceno, rezultat!.StatusVerifikacije);
        Assert.IsTrue(firma.AdminVerificirao);
        Assert.AreEqual("Haso instalacije d.o.o.", firma.NazivFirme);
        Assert.AreEqual("logo.png", firma.Slika);
        _repo.Verify(r => r.SacuvajAsync(), Times.Once);
    }

    [TestMethod]
    public async Task ObradiZahtjevAsync_OdobrenBezNovogLogotipa_ZadrzavaStaruSliku()
    {
        var firma = new Firma { Slika = "stara.png" };
        var zahtjev = ZahtjevNaCekanju(firma);
        zahtjev.Logotip = string.Empty;
        _repo.Setup(r => r.DajPoIdAsync(1)).ReturnsAsync(zahtjev);

        await _servis.ObradiZahtjevAsync(1, odobri: true);

        Assert.AreEqual("stara.png", firma.Slika);
    }

    [TestMethod]
    public async Task ObradiZahtjevAsync_Odobren_NeBriseNijedanFajl()
    {
        _repo.Setup(r => r.DajPoIdAsync(1)).ReturnsAsync(ZahtjevNaCekanju(new Firma()));

        await _servis.ObradiZahtjevAsync(1, odobri: true);

        _storage.VerifyNoOtherCalls();
    }

    // ---------- ObradiZahtjevAsync: odbijanje ----------

    [TestMethod]
    public async Task ObradiZahtjevAsync_Odbijen_BriseLogotipIDokumente()
    {
        _repo.Setup(r => r.DajPoIdAsync(1)).ReturnsAsync(ZahtjevNaCekanju(new Firma()));

        var rezultat = await _servis.ObradiZahtjevAsync(1, odobri: false);

        Assert.AreEqual(Status.Odbijeno, rezultat!.StatusVerifikacije);
        _storage.Verify(s => s.ObrisiPublic("logo.png", It.IsAny<CancellationToken>()), Times.Once);
        _storage.Verify(s => s.ObrisiPrivate("licenca.pdf", It.IsAny<CancellationToken>()), Times.Once);
        _storage.Verify(s => s.ObrisiPrivate("porezno.pdf", It.IsAny<CancellationToken>()), Times.Once);
        _storage.Verify(s => s.ObrisiPrivate("rjesenje.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ObradiZahtjevAsync_OdbijenBezFajlova_NistaNeBrise()
    {
        var zahtjev = ZahtjevNaCekanju();
        zahtjev.Logotip = string.Empty;
        zahtjev.LicencaDjelatnosti = null;
        zahtjev.PoreznoUvjerenje = string.Empty;
        zahtjev.Rjesenje = string.Empty;
        _repo.Setup(r => r.DajPoIdAsync(1)).ReturnsAsync(zahtjev);

        await _servis.ObradiZahtjevAsync(1, odobri: false);

        _storage.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ObradiZahtjevAsync_ZahtjevBezFirme_MijenjaSamoStatus()
    {
        _repo.Setup(r => r.DajPoIdAsync(1)).ReturnsAsync(ZahtjevNaCekanju(firma: null));

        var rezultat = await _servis.ObradiZahtjevAsync(1, odobri: true);

        Assert.AreEqual(Status.Prihvaceno, rezultat!.StatusVerifikacije);
        _repo.Verify(r => r.SacuvajAsync(), Times.Once);
    }
}