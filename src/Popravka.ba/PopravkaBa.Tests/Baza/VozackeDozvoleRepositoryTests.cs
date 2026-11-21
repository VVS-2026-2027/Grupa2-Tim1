using Popravka.ba.Data;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Infrastructure.Repositories;

namespace PopravkaBa.Tests.Baza;

[TestClass]
public class VozackeDozvoleRepositoryTests
{
    private ApplicationDbContext _db = null!;
    private VozackeDozvoleRepository _repo = null!;

    [TestInitialize]
    public void Postavi()
    {
        _db = TestHelpers.NovaBaza();
        _repo = new VozackeDozvoleRepository(_db);
    }

    [TestCleanup]
    public void Pocisti() => _db.Dispose();

    private static OglasVozackaDozvola Red(int oglasId, VozackaDozvola dozvola) =>
        new() { OglasID = oglasId, VozackaDozvola = dozvola };

    public static IEnumerable<object[]> DozvolePoOglasu =>
        DataDrivenInputParser.CitajXml<int, int>("TestData/xml/VozackeDozvoleRepository_poOglasu.xml");

    [TestMethod]
    [DynamicData(nameof(DozvolePoOglasu))]
    public async Task DajSveVozackeDozvoleOglasa_PodaciIzXml_VracaOcekivaniBroj(int oglasId, int ocekivaniBroj)
    {
        // Arrange
        _db.OglasVozackeDozvole.AddRange(
            Red(1, VozackaDozvola.A),
            Red(1, VozackaDozvola.B),
            Red(1, VozackaDozvola.BE),
            Red(2, VozackaDozvola.C));
        await _db.SaveChangesAsync();

        // Act
        var rezultat = await _repo.DajSveVozackeDozvoleOglasa(oglasId);

        // Assert
        Assert.AreEqual(ocekivaniBroj, rezultat.Count());
    }

    [TestMethod]
    public async Task DodajVozackeDozvoleOglasu_NullLista_NeDodajeNista()
    {
        // Act
        await _repo.DodajVozackeDozvoleOglasu(10, null);

        // Assert
        Assert.AreEqual(0, _db.OglasVozackeDozvole.Count());
    }

    [TestMethod]
    public async Task DodajVozackeDozvoleOglasu_DuplikatiUListi_DodajeSamoJedinstvene()
    {
        // Act
        await _repo.DodajVozackeDozvoleOglasu(
            10, new List<VozackaDozvola> { VozackaDozvola.B, VozackaDozvola.BE, VozackaDozvola.B });

        // Assert
        Assert.AreEqual(2, _db.OglasVozackeDozvole.Count(vd => vd.OglasID == 10));
    }

    [TestMethod]
    public async Task DodajVozackeDozvoleOglasu_NoveDozvole_DodajeRedove()
    {
        // Act
        await _repo.DodajVozackeDozvoleOglasu(10, new List<VozackaDozvola> { VozackaDozvola.A, VozackaDozvola.C });

        // Assert
        Assert.AreEqual(2, _db.OglasVozackeDozvole.Count(vd => vd.OglasID == 10));
    }

    [TestMethod]
    public async Task UkloniSveVozackeDozvoleOglasa_DvaOglasaSDozvolama_BriseSamoDozvoleTrazenogOglasa()
    {
        // Arrange
        _db.OglasVozackeDozvole.AddRange(Red(10, VozackaDozvola.B), Red(20, VozackaDozvola.C));
        await _db.SaveChangesAsync();

        // Act
        await _repo.UkloniSveVozackeDozvoleOglasa(10);

        // Assert
        Assert.AreEqual(0, _db.OglasVozackeDozvole.Count(vd => vd.OglasID == 10));
        Assert.AreEqual(1, _db.OglasVozackeDozvole.Count(vd => vd.OglasID == 20));
    }

    [TestMethod]
    public async Task AzurirajVozackeDozvoleOglasa_PostojecaLista_ZamjenjujeRedove()
    {
        // Arrange
        _db.OglasVozackeDozvole.AddRange(Red(10, VozackaDozvola.B), Red(10, VozackaDozvola.C));
        await _db.SaveChangesAsync();

        // Act
        await _repo.AzurirajVozackeDozvoleOglasa(10, new List<VozackaDozvola> { VozackaDozvola.D });

        // Assert
        var dozvole = _db.OglasVozackeDozvole.Where(vd => vd.OglasID == 10).ToList();
        Assert.AreEqual(1, dozvole.Count);
        Assert.AreEqual(VozackaDozvola.D, dozvole.Single().VozackaDozvola);
    }
}
