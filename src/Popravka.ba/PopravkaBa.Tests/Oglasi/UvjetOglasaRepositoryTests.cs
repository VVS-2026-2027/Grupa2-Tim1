using Popravka.ba.Data;
using PopravkaBa.Domain.Models;
using PopravkaBa.Infrastructure.Repositories;

namespace PopravkaBa.Tests.Oglasi;

[TestClass]
public class UvjetOglasaRepositoryTests
{
    private ApplicationDbContext _db = null!;
    private UvjetOglasaRepository _repo = null!;

    [TestInitialize]
    public void Postavi()
    {
        _db = TestHelpers.NovaBaza();
        _repo = new UvjetOglasaRepository(_db);
    }

    [TestCleanup]
    public void Pocisti() => _db.Dispose();

    private static UvjetOglasa Uvjet(int id, int oglasId, string tekst) =>
        new() { ID = id, OglasID = oglasId, TekstUvjeta = tekst };

    public static IEnumerable<object[]> UvjetiPoOglasu =>
        DataDrivenInputParser.CitajXml<int, int>("TestData/xml/UvjetOglasaRepository_poOglasu.xml");

    [TestMethod]
    [DynamicData(nameof(UvjetiPoOglasu))]
    public async Task DajSveUvjeteOglasa_PodaciIzXml_VracaOcekivaniBroj(int oglasId, int ocekivaniBroj)
    {
        // Arrange
        _db.UvjetiOglasa.AddRange(
            Uvjet(1, 1, "A"), Uvjet(2, 1, "B"), Uvjet(3, 1, "C"), Uvjet(4, 2, "D"));
        await _db.SaveChangesAsync();

        // Act
        var rezultat = await _repo.DajSveUvjeteOglasa(oglasId);

        // Assert
        Assert.AreEqual(ocekivaniBroj, rezultat.Count());
    }

    [TestMethod]
    public async Task DajUvjetPoIdAsync_PostojeciId_VracaUvjet()
    {
        // Arrange
        _db.UvjetiOglasa.Add(Uvjet(5, 10, "Uvjet A"));
        await _db.SaveChangesAsync();

        // Act
        var rezultat = await _repo.DajUvjetPoIdAsync(5);

        // Assert
        Assert.AreEqual("Uvjet A", rezultat?.TekstUvjeta);
    }

    [TestMethod]
    public async Task DajUvjetPoIdAsync_NepostojeciId_VracaNull()
    {
        // Act
        var rezultat = await _repo.DajUvjetPoIdAsync(99);

        // Assert
        Assert.IsNull(rezultat);
    }

    [TestMethod]
    public async Task DodajUvjeteOglasu_PraznaLista_NeDodajeNista()
    {
        // Act
        await _repo.DodajUvjeteOglasu(10, new List<string>());

        // Assert
        Assert.AreEqual(0, _db.UvjetiOglasa.Count());
    }

    [TestMethod]
    public async Task DodajUvjeteOglasu_NullLista_NeDodajeNista()
    {
        // Act
        await _repo.DodajUvjeteOglasu(10, null!);

        // Assert
        Assert.AreEqual(0, _db.UvjetiOglasa.Count());
    }

    [TestMethod]
    public async Task DodajUvjeteOglasu_NoveUvjete_DodajeRedove()
    {
        // Act
        await _repo.DodajUvjeteOglasu(10, new List<string> { "A", "B" });

        // Assert
        Assert.AreEqual(2, _db.UvjetiOglasa.Count(uo => uo.OglasID == 10));
    }

    [TestMethod]
    public async Task UkloniSveUvjeteOglasa_DvaOglasaSUvjetima_BriseSamoUvjeteTrazenogOglasa()
    {
        // Arrange
        _db.UvjetiOglasa.AddRange(
            Uvjet(1, 10, "A"), Uvjet(2, 10, "B"), Uvjet(3, 20, "C"));
        await _db.SaveChangesAsync();

        // Act
        await _repo.UkloniSveUvjeteOglasa(10);

        // Assert
        Assert.AreEqual(0, _db.UvjetiOglasa.Count(uo => uo.OglasID == 10));
        Assert.AreEqual(1, _db.UvjetiOglasa.Count(uo => uo.OglasID == 20));
    }

    [TestMethod]
    public async Task AzurirajUvjeteOglasa_PostojecaLista_ZamjenjujeRedove()
    {
        // Arrange
        _db.UvjetiOglasa.AddRange(Uvjet(1, 10, "Stari A"), Uvjet(2, 10, "Stari B"));
        await _db.SaveChangesAsync();

        // Act
        await _repo.AzurirajUvjeteOglasa(10, new List<string> { "Novi" });

        // Assert
        var uvjeti = _db.UvjetiOglasa.Where(uo => uo.OglasID == 10).ToList();
        Assert.AreEqual(1, uvjeti.Count);
        Assert.AreEqual("Novi", uvjeti.Single().TekstUvjeta);
    }
}
