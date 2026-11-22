using Popravka.ba.Data;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Specifications.Subtype;
using PopravkaBa.Infrastructure.Repositories;
using static PopravkaBa.Tests.Oglasi.OglasiTestHelper;

namespace PopravkaBa.Tests.Oglasi;

[TestClass]
[TestCategory("Repository")]
public class OglasUslugeRepositoryTests
{
    private ApplicationDbContext _baza = null!;
    private OglasUslugeRepository _repo = null!;

    [TestInitialize]
    public async Task PripremiBazu()
    {
        _baza = TestHelpers.NovaBaza();
        _repo = new OglasUslugeRepository(_baza);
        _baza.AddRange(
            new Klijent { Id = VlasnikId, UserName = "klijent1" },
            new Majstor { Id = "m1", UserName = "majstor1", Adresa = "Adresa 1" },
            new Mjesto { MjestoID = 1, Naziv = "Sarajevo" });
        await _baza.SaveChangesAsync();
    }

    [TestCleanup]
    public async Task OslobodiBazu() => await _baza.DisposeAsync();

    private async Task Ubaci(params object[] entiteti)
    {
        _baza.AddRange(entiteti);
        await _baza.SaveChangesAsync();
        _baza.ChangeTracker.Clear();
    }

    private static PonudaUsluge Ponuda(int id, int oglasId) => new()
    {
        ID = id, OglasUslugeID = oglasId, IzvrsilacID = "m1", Cijena = 100,
        DatumPocetkaUsluge = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajPoIdAsync_OglasSaPonudom_VracaOglasSaPonudamaIIzvrsiocem()
    {
        // Arrange
        await Ubaci(OglasUsluge(1), Ponuda(1, 1));

        // Act
        var oglas = await _repo.DajPoIdAsync(1);

        // Assert
        Assert.AreEqual("majstor1", oglas!.Ponude!.Single().Izvrsilac.UserName);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajPoIdAsync_NepostojeciOglas_VracaNull()
    {
        // Act
        var oglas = await _repo.DajPoIdAsync(99);

        // Assert
        Assert.IsNull(oglas);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajSveAsync_DvaOglasa_VracaObaSaVlasnikom()
    {
        // Arrange
        await Ubaci(OglasUsluge(1), OglasUsluge(2));

        // Act
        var oglasi = (await _repo.DajSveAsync()).ToList();

        // Assert
        Assert.AreEqual(2, oglasi.Count);
        Assert.IsTrue(oglasi.All(o => o.VlasnikOglasa.UserName == "klijent1"));
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task PronadjiAsync_SpecifikacijaAktivnih_VracaSamoAktivneIUkupanBroj()
    {
        // Arrange
        await Ubaci(OglasUsluge(1), OglasUsluge(2), OglasUsluge(3, status: Status.Neaktivan));

        // Act
        var rezultat = await _repo.PronadjiAsync(new AktivanSpecification<OglasUsluge>(), 1, 10);

        // Assert
        Assert.AreEqual(2, rezultat.Ukupno);
        Assert.AreEqual(2, rezultat.Stavke.Count);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task PronadjiAsync_DrugaStranica_PreskaceOglasePrveStranice()
    {
        // Arrange
        await Ubaci(OglasUsluge(1), OglasUsluge(2), OglasUsluge(3));
        var spec = new SortiranjeSpecification<OglasUsluge>(new AktivanSpecification<OglasUsluge>(), o => o.OglasID);

        // Act
        var rezultat = await _repo.PronadjiAsync(spec, stranica: 2, stavkiPoStranici: 2);

        // Assert
        Assert.AreEqual(3, rezultat.Ukupno);
        CollectionAssert.AreEqual(new[] { 3 }, rezultat.Stavke.Select(o => o.OglasID).ToList());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task PronadjiAsync_SortiranjeOpadajuce_NajnovijiOglasPrvi()
    {
        // Arrange
        await Ubaci(OglasUsluge(1), OglasUsluge(2), OglasUsluge(3));
        var spec = new SortiranjeSpecification<OglasUsluge>(
            new AktivanSpecification<OglasUsluge>(), o => o.DatumObjave, descending: true);

        // Act
        var rezultat = await _repo.PronadjiAsync(spec, 1, 10);

        // Assert
        CollectionAssert.AreEqual(new[] { 3, 2, 1 }, rezultat.Stavke.Select(o => o.OglasID).ToList());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajNedavneAsync_NeaktivanNajnoviji_VracaSamoAktivneOdNajnovijeg()
    {
        // Arrange
        await Ubaci(OglasUsluge(1), OglasUsluge(2), OglasUsluge(3, status: Status.Neaktivan));

        // Act
        var oglasi = (await _repo.DajNedavneAsync(5)).Select(o => o.OglasID).ToList();

        // Assert
        CollectionAssert.AreEqual(new[] { 2, 1 }, oglasi);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DodajAsync_NoviOglas_SnimaOglasSaDatumomObjave()
    {
        // Arrange
        var prije = DateTime.UtcNow;

        // Act
        await _repo.DodajAsync(OglasUsluge(1));

        // Assert
        Assert.IsTrue(_baza.OglasiUsluga.Single().DatumObjave >= prije);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task IzvrsiPretraguTekstaAsync_TekstUOpisu_VracaSamoAktivneOglase()
    {
        // Arrange
        await Ubaci(OglasUsluge(1), OglasUsluge(2, status: Status.Neaktivan));

        // Act
        var oglasi = await _repo.IzvrsiPretraguTekstaAsync("okrečiti");

        // Assert
        CollectionAssert.AreEqual(new[] { 1 }, oglasi.Select(o => o.OglasID).ToList());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task IzvrsiPretraguTekstaAsync_TekstKojiNePostoji_VracaPraznuListu()
    {
        // Arrange
        await Ubaci(OglasUsluge(1));

        // Act
        var oglasi = await _repo.IzvrsiPretraguTekstaAsync("krov");

        // Assert
        Assert.AreEqual(0, oglasi.Count());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObrisiAsync_PostojeciOglas_BriseOglas()
    {
        // Arrange
        await Ubaci(OglasUsluge(1));

        // Act
        await _repo.ObrisiAsync(1);

        // Assert
        Assert.AreEqual(0, _baza.OglasiUsluga.Count());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObrisiAsync_NepostojeciOglas_NistaNeBrise()
    {
        // Arrange
        await Ubaci(OglasUsluge(1));

        // Act
        await _repo.ObrisiAsync(99);

        // Assert
        Assert.AreEqual(1, _baza.OglasiUsluga.Count());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediAsync_IzmijenjenBudzet_SnimaIzmjenu()
    {
        // Arrange
        await Ubaci(OglasUsluge(1));
        var oglas = OglasUsluge(1);
        oglas.MaxBudzet = 999;

        // Act
        await _repo.UrediAsync(oglas);

        // Assert
        _baza.ChangeTracker.Clear();
        Assert.AreEqual(999, _baza.OglasiUsluga.Single().MaxBudzet);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajBrojZavrsenih_IsporuceniIAktivni_BrojiSamoIsporucene()
    {
        // Arrange
        await Ubaci(OglasUsluge(1, status: Status.Isporuceno), OglasUsluge(2, status: Status.Isporuceno), OglasUsluge(3));

        // Act
        var broj = await _repo.DajBrojZavrsenih();

        // Assert
        Assert.AreEqual(2, broj);
    }
}
