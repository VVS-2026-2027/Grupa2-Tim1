using Popravka.ba.Data;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Infrastructure.Repositories;
using static PopravkaBa.Tests.Oglasi.PrijavaRadnoMjestoRepositoryTestHelper;

namespace PopravkaBa.Tests.Oglasi;

[TestClass]
public class PrijavaRadnoMjestoRepositoryTests
{
    private ApplicationDbContext _baza = null!;
    private PrijavaRadnoMjestoRepository _repozitorij = null!;

    [TestInitialize]
    public async Task PripremiBazu()
    {
        _baza = TestHelpers.NovaBaza();
        _repozitorij = new PrijavaRadnoMjestoRepository(_baza);

        _baza.AddRange(
            new Klijent { Id = "k1", UserName = "vlasnik1" },
            new Majstor { Id = "m1", UserName = "majstor1", Adresa = "Adresa 1" },
            new Majstor { Id = "m2", UserName = "majstor2", Adresa = "Adresa 2" },
            new Mjesto { MjestoID = 1, Naziv = "Sarajevo" },
            new Kategorija { ID = 1, Naziv = "Vodoinstalacije" },
            new IzvrsilacKategorija
            {
                ID = 1,
                IzvrsilacID = "m1",
                KategorijaID = 1
            },
            NoviOglas(10),
            NoviOglas(20));

        await _baza.SaveChangesAsync();
        _baza.ChangeTracker.Clear();
    }

    [TestCleanup]
    public async Task OslobodiBazu()
    {
        await _baza.DisposeAsync();
    }

    [TestMethod]
    public async Task DajPoIdAsync_PostojecaPrijava_VracaPrijavuSPovezanimPodacima()
    {
        // Arrange
        await UbaciPrijave(_baza, NovaPrijava(1), NovaPrijava(2, 20, "m2"));

        // Act
        var rezultat = await _repozitorij.DajPoIdAsync(1);

        // Assert
        Assert.IsNotNull(rezultat);
        Assert.AreEqual(1, rezultat.ID);
        Assert.AreEqual(10, rezultat.OglasRadnoMjesto.OglasID);
        Assert.AreEqual("m1", rezultat.Majstor.Id);
        Assert.AreEqual("Vodoinstalacije",
            rezultat.Majstor.Kategorije!.Single().Kategorija.Naziv);
    }

    [TestMethod]
    public async Task DajPoIdAsync_NepostojecaPrijava_VracaNull()
    {
        // Arrange
        await UbaciPrijave(_baza, NovaPrijava(1));

        // Act
        var rezultat = await _repozitorij.DajPoIdAsync(99);

        // Assert
        Assert.IsNull(rezultat);
    }

    [TestMethod]
    public async Task DajSvePrijaveOglasaAsync_ViseOglasa_FiltriraISortiraOdNajstarije()
    {
        // Arrange
        await UbaciPrijave(_baza,
            NovaPrijava(2, 10, "m2"),
            NovaPrijava(1),
            NovaPrijava(3, 20));

        // Act
        var rezultat = (await _repozitorij.DajSvePrijaveOglasaAsync(10))
            .ToArray();

        // Assert
        CollectionAssert.AreEqual(
            new[] { 1, 2 },
            rezultat.Select(p => p.ID).ToArray());
        Assert.AreEqual("m1", rezultat[0].Majstor.Id);
        Assert.AreEqual("m2", rezultat[1].Majstor.Id);
        Assert.AreEqual("Vodoinstalacije",
            rezultat[0].Majstor.Kategorije!.Single().Kategorija.Naziv);
    }

    [TestMethod]
    public async Task DodajAsync_NovaPrijava_SpremaPrijavuUBazu()
    {
        // Arrange
        var prijava = NovaPrijava(1);

        // Act
        await _repozitorij.DodajAsync(prijava);

        // Assert
        _baza.ChangeTracker.Clear();
        var spremljena = await _baza.PrijaveRadnoMjesto.FindAsync(1);
        Assert.IsNotNull(spremljena);
        Assert.AreEqual(10, spremljena.OglasID);
        Assert.AreEqual("m1", spremljena.MajstorID);
        Assert.AreEqual(Status.NaCekanju, spremljena.StatusPrijave);
        Assert.AreEqual(prijava.VrijemePrijave, spremljena.VrijemePrijave);
    }

    [TestMethod]
    public async Task UrediAsync_IzmijenjenStatus_SpremaNoviStatus()
    {
        // Arrange
        var prijava = NovaPrijava(1);
        await UbaciPrijave(_baza, prijava);
        prijava.StatusPrijave = Status.Prihvaceno;

        // Act
        await _repozitorij.UrediAsync(prijava);

        // Assert
        _baza.ChangeTracker.Clear();
        var spremljena = await _baza.PrijaveRadnoMjesto.FindAsync(1);
        Assert.IsNotNull(spremljena);
        Assert.AreEqual(Status.Prihvaceno, spremljena.StatusPrijave);
    }

    [TestMethod]
    public async Task ObrisiAsync_PostojecaPrijava_BriseSamoZadanuPrijavu()
    {
        // Arrange
        await UbaciPrijave(_baza, NovaPrijava(1), NovaPrijava(2, 10, "m2"));

        // Act
        await _repozitorij.ObrisiAsync(1);

        // Assert
        _baza.ChangeTracker.Clear();
        Assert.IsNull(await _baza.PrijaveRadnoMjesto.FindAsync(1));
        Assert.IsNotNull(await _baza.PrijaveRadnoMjesto.FindAsync(2));
    }

    [TestMethod]
    public async Task ObrisiAsync_NepostojecaPrijava_OstavljaPostojecuPrijavu()
    {
        // Arrange
        await UbaciPrijave(_baza, NovaPrijava(1));

        // Act
        await _repozitorij.ObrisiAsync(99);

        // Assert
        _baza.ChangeTracker.Clear();
        Assert.IsNotNull(await _baza.PrijaveRadnoMjesto.FindAsync(1));
        Assert.AreEqual(1, _baza.PrijaveRadnoMjesto.Count());
    }
}