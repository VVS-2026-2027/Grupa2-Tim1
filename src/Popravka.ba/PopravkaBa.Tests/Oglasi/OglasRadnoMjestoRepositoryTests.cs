using Popravka.ba.Data;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Specifications.Subtype;
using PopravkaBa.Infrastructure.Repositories;
using static PopravkaBa.Tests.Oglasi.OglasiTestHelper;

namespace PopravkaBa.Tests.Oglasi;

[TestClass]
[TestCategory("Repository")]
public class OglasRadnoMjestoRepositoryTests
{
    private ApplicationDbContext _baza = null!;
    private OglasRadnoMjestoRepository _repo = null!;

    [TestInitialize]
    public async Task PripremiBazu()
    {
        _baza = TestHelpers.NovaBaza();
        _repo = new OglasRadnoMjestoRepository(_baza);
        _baza.AddRange(
            new Firma { Id = VlasnikId, UserName = "firma1", NazivFirme = "Elektro d.o.o.", Adresa = "Adresa 1" },
            new Majstor { Id = "m1", UserName = "majstor1", Adresa = "Adresa 2" },
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

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajPoIdAsync_OglasSaUvjetomIPrijavom_VracaOglasSaPovezanimPodacima()
    {
        // Arrange
        await Ubaci(OglasRadnoMjesto(1),
            new UvjetOglasa { OglasID = 1, TekstUvjeta = "Vozačka B" },
            new PrijavaRadnoMjesto { OglasID = 1, MajstorID = "m1" });

        // Act
        var oglas = await _repo.DajPoIdAsync(1);

        // Assert
        Assert.AreEqual("Vozačka B", oglas!.Uvjeti.Single().TekstUvjeta);
        Assert.AreEqual("majstor1", oglas.Prijave.Single().Majstor.UserName);
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
    public async Task DajNedavneAsync_NeaktivanNajnoviji_VracaSamoAktivneOdNajnovijeg()
    {
        // Arrange
        await Ubaci(OglasRadnoMjesto(1), OglasRadnoMjesto(2), OglasRadnoMjesto(3, status: Status.Neaktivan));

        // Act
        var oglasi = (await _repo.DajNedavneAsync(5))!.Select(o => o.OglasID).ToList();

        // Assert
        CollectionAssert.AreEqual(new[] { 2, 1 }, oglasi);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajNedavneAsync_TopJedan_VracaSamoNajnoviji()
    {
        // Arrange
        await Ubaci(OglasRadnoMjesto(1), OglasRadnoMjesto(2));

        // Act
        var oglasi = (await _repo.DajNedavneAsync(1))!.Select(o => o.OglasID).ToList();

        // Assert
        CollectionAssert.AreEqual(new[] { 2 }, oglasi);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task PronadjiAsync_SpecifikacijaAktivnih_VracaSamoAktivneIUkupanBroj()
    {
        // Arrange
        await Ubaci(OglasRadnoMjesto(1), OglasRadnoMjesto(2, status: Status.Neaktivan));

        // Act
        var rezultat = await _repo.PronadjiAsync(new AktivanSpecification<OglasRadnoMjesto>(), 1, 10);

        // Assert
        Assert.AreEqual(1, rezultat.Ukupno);
        Assert.AreEqual(1, rezultat.Stavke.Single().OglasID);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task PronadjiAsync_SortiranjeRastuce_PrvaStranicaSaNajstarijim()
    {
        // Arrange
        await Ubaci(OglasRadnoMjesto(1), OglasRadnoMjesto(2), OglasRadnoMjesto(3));
        var spec = new SortiranjeSpecification<OglasRadnoMjesto>(
            new AktivanSpecification<OglasRadnoMjesto>(), o => o.DatumObjave);

        // Act
        var rezultat = await _repo.PronadjiAsync(spec, stranica: 1, stavkiPoStranici: 2);

        // Assert
        CollectionAssert.AreEqual(new[] { 1, 2 }, rezultat.Stavke.Select(o => o.OglasID).ToList());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task PronadjiAsync_SortiranjeOpadajuce_NajnovijiOglasPrvi()
    {
        // Arrange
        await Ubaci(OglasRadnoMjesto(1), OglasRadnoMjesto(2));
        var spec = new SortiranjeSpecification<OglasRadnoMjesto>(
            new AktivanSpecification<OglasRadnoMjesto>(), o => o.DatumObjave, descending: true);

        // Act
        var rezultat = await _repo.PronadjiAsync(spec, 1, 10);

        // Assert
        CollectionAssert.AreEqual(new[] { 2, 1 }, rezultat.Stavke.Select(o => o.OglasID).ToList());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajSveAsync_AktivniINeaktivni_VracaSveOglase()
    {
        // Arrange
        await Ubaci(OglasRadnoMjesto(1), OglasRadnoMjesto(2, status: Status.Neaktivan));

        // Act
        var oglasi = await _repo.DajSveAsync();

        // Assert
        Assert.AreEqual(2, oglasi.Count());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DodajAsync_NoviOglas_SnimaOglasSaDatumomObjave()
    {
        // Arrange
        var prije = DateTime.UtcNow;

        // Act
        await _repo.DodajAsync(OglasRadnoMjesto(1));

        // Assert
        Assert.IsTrue(_baza.OglasiRadnogMjesta.Single().DatumObjave >= prije);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task IzvrsiPretraguTekstaAsync_TekstUOpisu_VracaSamoAktivneOglase()
    {
        // Arrange
        await Ubaci(OglasRadnoMjesto(1), OglasRadnoMjesto(2, status: Status.Neaktivan));

        // Act
        var oglasi = await _repo.IzvrsiPretraguTekstaAsync("električara");

        // Assert
        CollectionAssert.AreEqual(new[] { 1 }, oglasi.Select(o => o.OglasID).ToList());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObrisiAsync_PostojeciOglas_BriseOglas()
    {
        // Arrange
        await Ubaci(OglasRadnoMjesto(1));

        // Act
        await _repo.ObrisiAsync(1);

        // Assert
        Assert.AreEqual(0, _baza.OglasiRadnogMjesta.Count());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObrisiAsync_NepostojeciOglas_NistaNeBrise()
    {
        // Arrange
        await Ubaci(OglasRadnoMjesto(1));

        // Act
        await _repo.ObrisiAsync(99);

        // Assert
        Assert.AreEqual(1, _baza.OglasiRadnogMjesta.Count());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediAsync_IzmijenjenBrojIzvrsilaca_SnimaIzmjenu()
    {
        // Arrange
        await Ubaci(OglasRadnoMjesto(1));
        var oglas = OglasRadnoMjesto(1);
        oglas.BrojIzvrsilaca = 5;

        // Act
        await _repo.UrediAsync(oglas);

        // Assert
        _baza.ChangeTracker.Clear();
        Assert.AreEqual(5, _baza.OglasiRadnogMjesta.Single().BrojIzvrsilaca);
    }
}
