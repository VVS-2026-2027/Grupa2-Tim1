using Popravka.ba.Data;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Infrastructure.Repositories;
using static PopravkaBa.Tests.Recenzije.RecenzijaRepositoryTestHelper;

namespace PopravkaBa.Tests.Recenzije;

[TestClass]
public class RecenzijaRepositoryTests
{
    private ApplicationDbContext _baza = null!;
    private RecenzijaRepository _repozitorij = null!;

    [TestInitialize]
    public async Task PripremiBazu()
    {
        _baza = TestHelpers.NovaBaza();
        _repozitorij = new RecenzijaRepository(_baza);

        _baza.AddRange(
            new Klijent { Id = "k1", UserName = "klijent1" },
            new Klijent { Id = "k2", UserName = "klijent2" },
            new Majstor { Id = "m1", UserName = "majstor1", Adresa = "Adresa 1" },
            new Majstor { Id = "m2", UserName = "majstor2", Adresa = "Adresa 2" });

        await _baza.SaveChangesAsync();
        _baza.ChangeTracker.Clear();
    }

    [TestCleanup]
    public async Task OslobodiBazu()
    {
        await _baza.DisposeAsync();
    }

    [TestMethod]
    public async Task DodajAsync_NovaRecenzija_SpremaRecenzijuUBazu()
    {
        // Arrange
        var recenzija = NovaRecenzija(1);
        recenzija.Komentar = "Odlična usluga";
        recenzija.Ocjena = 5;

        // Act
        await _repozitorij.DodajAsync(recenzija);

        // Assert
        _baza.ChangeTracker.Clear();
        var spremljena = await _baza.Recenzije.FindAsync(1);

        Assert.IsNotNull(spremljena);
        Assert.AreEqual("k1", spremljena.KlijentID);
        Assert.AreEqual("m1", spremljena.IzvrsilacID);
        Assert.AreEqual(5, spremljena.Ocjena);
        Assert.AreEqual("Odlična usluga", spremljena.Komentar);
    }

    [TestMethod]
    public async Task SacuvajAsync_IzmijenjenaRecenzija_SpremaNoveVrijednosti()
    {
        // Arrange
        var recenzija = NovaRecenzija(1);
        await UbaciRecenzije(_baza, recenzija);
        recenzija.Ocjena = 2;
        recenzija.Komentar = "Dopunjen komentar";

        // Act
        await _repozitorij.SacuvajAsync(recenzija);

        // Assert
        _baza.ChangeTracker.Clear();
        var spremljena = await _baza.Recenzije.FindAsync(1);

        Assert.IsNotNull(spremljena);
        Assert.AreEqual(2, spremljena.Ocjena);
        Assert.AreEqual("Dopunjen komentar", spremljena.Komentar);
    }

    [TestMethod]
    public async Task ObrisiAsync_PostojecaRecenzija_BriseSamoZadanuRecenziju()
    {
        // Arrange
        await UbaciRecenzije(_baza, NovaRecenzija(1), NovaRecenzija(2, "k2"));

        // Act
        await _repozitorij.ObrisiAsync(1);

        // Assert
        _baza.ChangeTracker.Clear();
        Assert.IsNull(await _baza.Recenzije.FindAsync(1));
        Assert.IsNotNull(await _baza.Recenzije.FindAsync(2));
    }

    [TestMethod]
    public async Task ObrisiAsync_NepostojecaRecenzija_OstavljaPostojecuRecenziju()
    {
        // Arrange
        await UbaciRecenzije(_baza, NovaRecenzija(1));

        // Act
        await _repozitorij.ObrisiAsync(99);

        // Assert
        _baza.ChangeTracker.Clear();
        Assert.IsNotNull(await _baza.Recenzije.FindAsync(1));
        Assert.AreEqual(1, _baza.Recenzije.Count());
    }

    [TestMethod]
    public async Task DajPoIdAsync_PostojeciId_VracaRecenzijuSKorisnicima()
    {
        // Arrange
        await UbaciRecenzije(_baza, NovaRecenzija(1), NovaRecenzija(2, "k2", "m2"));

        // Act
        var rezultat = await _repozitorij.DajPoIdAsync(2);

        // Assert
        Assert.IsNotNull(rezultat);
        Assert.AreEqual(2, rezultat.RecenzijaID);
        Assert.AreEqual("k2", rezultat.Klijent.Id);
        Assert.AreEqual("m2", rezultat.Izvrsilac.Id);
    }

    [TestMethod]
    public async Task DajPoIdAsync_NepostojeciId_VracaNull()
    {
        // Arrange
        await UbaciRecenzije(_baza, NovaRecenzija(1));

        // Act
        var rezultat = await _repozitorij.DajPoIdAsync(99);

        // Assert
        Assert.IsNull(rezultat);
    }

    [TestMethod]
    [DataRow("k1", "m1", true)]
    [DataRow("k1", "m2", false)]
    [DataRow("k2", "m1", false)]
    public async Task PostojiAsync_ParKlijentaIIzvrsioca_VracaOcekivaniRezultat(
        string klijentId, string izvrsilacId, bool ocekivano)
    {
        // Arrange
        await UbaciRecenzije(_baza, NovaRecenzija(1));

        // Act
        var rezultat = await _repozitorij.PostojiAsync(klijentId, izvrsilacId);

        // Assert
        Assert.AreEqual(ocekivano, rezultat);
    }

    [TestMethod]
    public async Task DajSveAsync_DvijeRecenzije_VracaObjeSPovezanimKorisnicima()
    {
        // Arrange
        await UbaciRecenzije(_baza, NovaRecenzija(1), NovaRecenzija(2, "k2", "m2"));

        // Act
        var rezultat = (await _repozitorij.DajSveAsync())
            .OrderBy(r => r.RecenzijaID)
            .ToArray();

        // Assert
        CollectionAssert.AreEqual(
            new[] { 1, 2 },
            rezultat.Select(r => r.RecenzijaID).ToArray());
        Assert.AreEqual("k1", rezultat[0].Klijent.Id);
        Assert.AreEqual("m1", rezultat[0].Izvrsilac.Id);
        Assert.AreEqual("k2", rezultat[1].Klijent.Id);
        Assert.AreEqual("m2", rezultat[1].Izvrsilac.Id);
    }

    [TestMethod]
    public async Task DajPrijavljeneAsync_RazlicitiStatusi_VracaNerijeseneOdNajstarije()
    {
        // Arrange
        var novija = NovaRecenzija(1);
        novija.Prijavljena = true;
        novija.StatusPrijave = Status.NaCekanju;
        novija.DatumPrijave = new DateTime(2026, 11, 2);

        var starija = NovaRecenzija(2, "k2");
        starija.Prijavljena = true;
        starija.StatusPrijave = null;
        starija.DatumPrijave = new DateTime(2026, 11, 1);

        var odbijena = NovaRecenzija(3, "k1", "m2");
        odbijena.Prijavljena = true;
        odbijena.StatusPrijave = Status.Odbijeno;
        odbijena.DatumPrijave = new DateTime(2026, 10, 31);

        var neprijavljena = NovaRecenzija(4, "k2", "m2");
        await UbaciRecenzije(_baza, novija, starija, odbijena, neprijavljena);

        // Act
        var rezultat = await _repozitorij.DajPrijavljeneAsync();

        // Assert
        CollectionAssert.AreEqual(
            new[] { 2, 1 },
            rezultat.Select(r => r.RecenzijaID).ToArray());
    }

    [TestMethod]
    public async Task DajRecenzijeIzvrsiocaAsync_ViseIzvrsilaca_FiltriraISortiraOdNajnovije()
    {
        // Arrange
        await UbaciRecenzije(_baza,
            NovaRecenzija(1, "k1", "m1"),
            NovaRecenzija(2, "k2", "m1"),
            NovaRecenzija(3, "k1", "m2"));

        // Act
        var rezultat = (await _repozitorij.DajRecenzijeIzvrsiocaAsync("m1"))
            .ToArray();

        // Assert
        CollectionAssert.AreEqual(
            new[] { 2, 1 },
            rezultat.Select(r => r.RecenzijaID).ToArray());
        Assert.AreEqual("k2", rezultat[0].Klijent.Id);
    }

    [TestMethod]
    public async Task DajRecenzijeKlijentaAsync_ViseKlijenata_FiltriraISortiraOdNajnovije()
    {
        // Arrange
        await UbaciRecenzije(_baza,
            NovaRecenzija(1, "k1", "m1"),
            NovaRecenzija(2, "k1", "m2"),
            NovaRecenzija(3, "k2", "m1"));

        // Act
        var rezultat = (await _repozitorij.DajRecenzijeKlijentaAsync("k1"))
            .ToArray();

        // Assert
        CollectionAssert.AreEqual(
            new[] { 2, 1 },
            rezultat.Select(r => r.RecenzijaID).ToArray());
        Assert.AreEqual("m2", rezultat[0].Izvrsilac.Id);
    }
}