using Popravka.ba.Data;
using PopravkaBa.Domain.Models;
using PopravkaBa.Infrastructure.Repositories;

namespace PopravkaBa.Tests.Zajednicko;

[TestClass]
public class KategorijaRepositoryTests
{
    private ApplicationDbContext _db = null!;
    private KategorijaRepository _repo = null!;

    [TestInitialize]
    public void Postavi()
    {
        _db = TestHelpers.NovaBaza();
        _repo = new KategorijaRepository(_db);
    }

    [TestCleanup]
    public void Pocisti() => _db.Dispose();

    private static Kategorija Kategorija(int id, string naziv) => new() { ID = id, Naziv = naziv };

    public static IEnumerable<object[]> TopKategorije =>
        DataDrivenInputParser.CitajXml<int, int, int>("TestData/xml/KategorijaRepository_topKategorije.xml");

    [TestMethod]
    public async Task DajSveAsync_ViseKategorija_VracaSve()
    {
        // Arrange
        _db.Kategorije.AddRange(Kategorija(1, "Vodoinstalacije"), Kategorija(2, "Elektroinstalacije"));
        await _db.SaveChangesAsync();

        // Act
        var rezultat = await _repo.DajSveAsync();

        // Assert
        Assert.AreEqual(2, rezultat.Count());
    }

    [TestMethod]
    public async Task DajPoIdAsync_PostojeciId_VracaKategoriju()
    {
        // Arrange
        _db.Kategorije.Add(Kategorija(5, "Vodoinstalacije"));
        await _db.SaveChangesAsync();

        // Act
        var rezultat = await _repo.DajPoIdAsync(5);

        // Assert
        Assert.AreEqual("Vodoinstalacije", rezultat?.Naziv);
    }

    [TestMethod]
    public async Task DajPoIdAsync_NepostojeciId_VracaNull()
    {
        // Act
        var rezultat = await _repo.DajPoIdAsync(99);

        // Assert
        Assert.IsNull(rezultat);
    }

    [TestMethod]
    public async Task DodajKategorijeOglasu_PraznaLista_NeDodajeNista()
    {
        // Act
        await _repo.DodajKategorijeOglasu(10, new List<int>());

        // Assert
        Assert.AreEqual(0, _db.OglasKategorije.Count());
    }

    [TestMethod]
    public async Task DodajKategorijeOglasu_NullLista_NeDodajeNista()
    {
        // Act
        await _repo.DodajKategorijeOglasu(10, null!);

        // Assert
        Assert.AreEqual(0, _db.OglasKategorije.Count());
    }

    [TestMethod]
    public async Task DodajKategorijeOglasu_NoveKategorije_DodajeRedove()
    {
        // Act
        await _repo.DodajKategorijeOglasu(10, new List<int> { 1, 2 });

        // Assert
        Assert.AreEqual(2, _db.OglasKategorije.Count(ok => ok.OglasID == 10));
    }

    [TestMethod]
    public async Task UkloniSveKategorijeOglasa_DvaOglasaSKategorijama_BriseSamoKategorijeTrazenogOglasa()
    {
        // Arrange
        _db.OglasKategorije.AddRange(
            new OglasKategorija { OglasID = 10, KategorijaID = 1 },
            new OglasKategorija { OglasID = 10, KategorijaID = 2 },
            new OglasKategorija { OglasID = 20, KategorijaID = 1 });
        await _db.SaveChangesAsync();

        // Act
        await _repo.UkloniSveKategorijeOglasa(10);

        // Assert
        Assert.AreEqual(0, _db.OglasKategorije.Count(ok => ok.OglasID == 10));
        Assert.AreEqual(1, _db.OglasKategorije.Count(ok => ok.OglasID == 20));
    }

    [TestMethod]
    public async Task AzurirajKategorijeOglasa_PostojecaLista_ZamjenjujeRedove()
    {
        // Arrange
        _db.OglasKategorije.Add(new OglasKategorija { OglasID = 10, KategorijaID = 1 });
        await _db.SaveChangesAsync();

        // Act
        await _repo.AzurirajKategorijeOglasa(10, new List<int> { 2, 3 });

        // Assert
        var kategorije = _db.OglasKategorije.Where(ok => ok.OglasID == 10).Select(ok => ok.KategorijaID).ToList();
        CollectionAssert.AreEquivalent(new List<int> { 2, 3 }, kategorije);
    }

    [TestMethod]
    public async Task DodajKategorijeIzvrsiocu_PostojecaKategorija_PreskaceDuplikat()
    {
        // Arrange
        _db.IzvrsilacKategorija.Add(new IzvrsilacKategorija { IzvrsilacID = "m1", KategorijaID = 1 });
        await _db.SaveChangesAsync();

        // Act
        await _repo.DodajKategorijeIzvrsiocu("m1", new List<int> { 1, 2 });

        // Assert
        Assert.AreEqual(2, _db.IzvrsilacKategorija.Count(ik => ik.IzvrsilacID == "m1"));
        Assert.AreEqual(1, _db.IzvrsilacKategorija.Count(ik => ik.IzvrsilacID == "m1" && ik.KategorijaID == 1));
    }

    [TestMethod]
    public async Task DodajKategorijeIzvrsiocu_NoveKategorije_DodajeSamoNove()
    {
        // Act
        await _repo.DodajKategorijeIzvrsiocu("m1", new List<int> { 1, 2 });

        // Assert
        Assert.AreEqual(2, _db.IzvrsilacKategorija.Count(ik => ik.IzvrsilacID == "m1"));
    }

    [TestMethod]
    public async Task UkloniSveKategorijeIzvrsiocu_DvaIzvrsiocaSKategorijom_BriseSamoKategorijeTrazenog()
    {
        // Arrange
        _db.IzvrsilacKategorija.AddRange(
            new IzvrsilacKategorija { IzvrsilacID = "m1", KategorijaID = 1 },
            new IzvrsilacKategorija { IzvrsilacID = "m2", KategorijaID = 1 });
        await _db.SaveChangesAsync();

        // Act
        await _repo.UkloniSveKategorijeIzvrsiocu("m1");

        // Assert
        Assert.AreEqual(0, _db.IzvrsilacKategorija.Count(ik => ik.IzvrsilacID == "m1"));
        Assert.AreEqual(1, _db.IzvrsilacKategorija.Count(ik => ik.IzvrsilacID == "m2"));
    }

    [TestMethod]
    public async Task AzurirajKategorijeIzvrsioca_PostojecaLista_ZamjenjujeSkup()
    {
        // Arrange
        _db.IzvrsilacKategorija.Add(new IzvrsilacKategorija { IzvrsilacID = "m1", KategorijaID = 1 });
        await _db.SaveChangesAsync();

        // Act
        await _repo.AzurirajKategorijeIzvrsioca("m1", new List<int> { 2 });

        // Assert
        var kategorije = _db.IzvrsilacKategorija.Where(ik => ik.IzvrsilacID == "m1").Select(ik => ik.KategorijaID).ToList();
        CollectionAssert.AreEqual(new List<int> { 2 }, kategorije);
    }

    [TestMethod]
    [DynamicData(nameof(TopKategorije))]
    public async Task DajTopKategorijePoMajstorimaAsync_PodaciIzXml_VracaOcekivaniBrojIPrviId(
        int topN, int ocekivaniBroj, int ocekivaniPrviId)
    {
        // Arrange
        _db.Kategorije.AddRange(Kategorija(1, "K1"), Kategorija(2, "K2"), Kategorija(3, "K3"));
        _db.IzvrsilacKategorija.AddRange(
            new IzvrsilacKategorija { IzvrsilacID = "m1", KategorijaID = 1 },
            new IzvrsilacKategorija { IzvrsilacID = "m2", KategorijaID = 1 },
            new IzvrsilacKategorija { IzvrsilacID = "m3", KategorijaID = 1 },
            new IzvrsilacKategorija { IzvrsilacID = "m4", KategorijaID = 2 },
            new IzvrsilacKategorija { IzvrsilacID = "m5", KategorijaID = 2 },
            new IzvrsilacKategorija { IzvrsilacID = "m6", KategorijaID = 3 });
        await _db.SaveChangesAsync();

        // Act
        var rezultat = (await _repo.DajTopKategorijePoMajstorimaAsync(topN)).ToList();

        // Assert
        Assert.AreEqual(ocekivaniBroj, rezultat.Count);
        Assert.AreEqual(ocekivaniPrviId, rezultat.FirstOrDefault().Item1?.ID ?? 0);
    }

    [TestMethod]
    public async Task DajTopKategorijePoMajstorimaAsync_NepostojecaKategorija_IgnoriseSe()
    {
        // Arrange
        _db.Kategorije.Add(Kategorija(1, "K1"));
        _db.IzvrsilacKategorija.AddRange(
            new IzvrsilacKategorija { IzvrsilacID = "m1", KategorijaID = 1 },
            new IzvrsilacKategorija { IzvrsilacID = "m2", KategorijaID = 99 },
            new IzvrsilacKategorija { IzvrsilacID = "m3", KategorijaID = 99 },
            new IzvrsilacKategorija { IzvrsilacID = "m4", KategorijaID = 99 });
        await _db.SaveChangesAsync();

        // Act
        var rezultat = (await _repo.DajTopKategorijePoMajstorimaAsync(5)).ToList();

        // Assert
        Assert.AreEqual(1, rezultat.Count);
        Assert.AreEqual(1, rezultat.Single().Item1.ID);
    }
}
