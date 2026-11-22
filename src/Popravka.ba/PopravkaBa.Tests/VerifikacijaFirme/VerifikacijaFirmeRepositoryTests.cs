using Popravka.ba.Data;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Infrastructure.Repositories;
using static PopravkaBa.Tests.VerifikacijaFirme.VerifikacijaFirmeRepositoryTestHelper;

namespace PopravkaBa.Tests.VerifikacijaFirme;

[TestClass]
[TestCategory("Repository")]
public class VerifikacijaFirmeRepositoryTests
{
    private ApplicationDbContext _baza = null!;
    private VerifikacijaFirmeRepository _repo = null!;

    [TestInitialize]
    public async Task PripremiBazu()
    {
        _baza = TestHelpers.NovaBaza();
        _repo = new VerifikacijaFirmeRepository(_baza);
        _baza.AddRange(
            new Firma { Id = "f1", UserName = "firma1", NazivFirme = "Haso instalacije d.o.o.", Adresa = "Adresa 1" },
            new Firma { Id = "f2", UserName = "firma2", NazivFirme = "Elektro d.o.o.", Adresa = "Adresa 2" },
            new Kategorija { ID = 1, Naziv = "Vodoinstalacije" },
            new IzvrsilacKategorija { IzvrsilacID = "f1", KategorijaID = 1 });
        await _baza.SaveChangesAsync();
        _baza.ChangeTracker.Clear();
    }

    [TestCleanup]
    public async Task OslobodiBazu() => await _baza.DisposeAsync();

    private async Task Ubaci(params EmailVerifikacijaFirme[] zahtjevi)
    {
        _baza.EmailVerifikacijaFirme.AddRange(zahtjevi);
        await _baza.SaveChangesAsync();
        _baza.ChangeTracker.Clear();
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DodajAsync_NoviZahtjev_SnimaZahtjevUBazu()
    {
        // Act
        await _repo.DodajAsync(Zahtjev(1, "f1"));

        // Assert
        Assert.AreEqual(1, _baza.EmailVerifikacijaFirme.Count());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajZadnjuZaFirmuAsync_DvaZahtjeva_VracaNajnoviji()
    {
        // Arrange
        await Ubaci(Zahtjev(1, "f1", danaUnazad: 10), Zahtjev(2, "f1", danaUnazad: 1), Zahtjev(3, "f2", danaUnazad: 0));

        // Act
        var zahtjev = await _repo.DajZadnjuZaFirmuAsync("f1");

        // Assert
        Assert.AreEqual(2, zahtjev!.VerifikacioniID);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajZadnjuZaFirmuAsync_FirmaBezZahtjeva_VracaNull()
    {
        // Arrange
        await Ubaci(Zahtjev(1, "f1"));

        // Act
        var zahtjev = await _repo.DajZadnjuZaFirmuAsync("f2");

        // Assert
        Assert.IsNull(zahtjev);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajNaCekanjuAsync_RazlicitiStatusi_VracaSamoNaCekanjuOdNajstarijeg()
    {
        // Arrange
        await Ubaci(
            Zahtjev(1, "f1", danaUnazad: 1),
            Zahtjev(2, "f2", danaUnazad: 5),
            Zahtjev(3, "f1", Status.Prihvaceno, danaUnazad: 9));

        // Act
        var zahtjevi = (await _repo.DajNaCekanjuAsync()).Select(z => z.VerifikacioniID).ToList();

        // Assert
        CollectionAssert.AreEqual(new[] { 2, 1 }, zahtjevi);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajNaCekanjuAsync_ZahtjevFirmeSaKategorijom_UcitavaFirmuIKategoriju()
    {
        // Arrange
        await Ubaci(Zahtjev(1, "f1"));

        // Act
        var zahtjev = (await _repo.DajNaCekanjuAsync()).Single();

        // Assert
        Assert.AreEqual("Vodoinstalacije", zahtjev.Firma.Kategorije!.Single().Kategorija.Naziv);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajPoIdAsync_PostojeciZahtjev_VracaZahtjevSaFirmom()
    {
        // Arrange
        await Ubaci(Zahtjev(1, "f2"));

        // Act
        var zahtjev = await _repo.DajPoIdAsync(1);

        // Assert
        Assert.AreEqual("Elektro d.o.o.", zahtjev!.Firma.NazivFirme);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajPoIdAsync_NepostojeciZahtjev_VracaNull()
    {
        // Act
        var zahtjev = await _repo.DajPoIdAsync(99);

        // Assert
        Assert.IsNull(zahtjev);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task SacuvajAsync_IzmijenjenStatus_SnimaIzmjenu()
    {
        // Arrange
        await Ubaci(Zahtjev(1, "f1"));
        var zahtjev = await _repo.DajPoIdAsync(1);
        zahtjev!.StatusVerifikacije = Status.Odbijeno;

        // Act
        await _repo.SacuvajAsync();

        // Assert
        _baza.ChangeTracker.Clear();
        Assert.AreEqual(Status.Odbijeno, _baza.EmailVerifikacijaFirme.Single().StatusVerifikacije);
    }
}
