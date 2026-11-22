using Popravka.ba.Data;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Infrastructure.Repositories;
using static PopravkaBa.Tests.Oglasi.OglasiTestHelper;

namespace PopravkaBa.Tests.Oglasi;

[TestClass]
[TestCategory("Repository")]
public class OglasMajstoraRepositoryTests
{
    private ApplicationDbContext _baza = null!;
    private OglasMajstoraRepository _repo = null!;

    [TestInitialize]
    public async Task PripremiBazu()
    {
        _baza = TestHelpers.NovaBaza();
        _repo = new OglasMajstoraRepository(_baza);
        _baza.AddRange(
            new Majstor { Id = VlasnikId, UserName = "majstor1", Adresa = "Adresa 1" },
            new Mjesto { MjestoID = 1, Naziv = "Sarajevo" });
        await _baza.SaveChangesAsync();
    }

    [TestCleanup]
    public async Task OslobodiBazu() => await _baza.DisposeAsync();

    private async Task Ubaci(params OglasMajstora[] oglasi)
    {
        _baza.OglasiMajstora.AddRange(oglasi);
        await _baza.SaveChangesAsync();
        _baza.ChangeTracker.Clear();
    }

    private int? MinCijenaVlasnika()
    {
        _baza.ChangeTracker.Clear();
        return _baza.ApplicationUsers.OfType<IzvrsilacUsluge>().Single(u => u.Id == VlasnikId).MinCijenaUsluge;
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajSveAsync_DvaOglasa_VracaObaSaMjestom()
    {
        // Arrange
        await Ubaci(OglasMajstora(1), OglasMajstora(2));

        // Act
        var oglasi = (await _repo.DajSveAsync()).ToList();

        // Assert
        Assert.AreEqual(2, oglasi.Count);
        Assert.IsTrue(oglasi.All(o => o.Mjesto?.Naziv == "Sarajevo"));
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajPoIdAsync_PostojeciOglas_VracaOglasSaVlasnikom()
    {
        // Arrange
        await Ubaci(OglasMajstora(1));

        // Act
        var oglas = await _repo.DajPoIdAsync(1);

        // Assert
        Assert.AreEqual("majstor1", oglas!.VlasnikOglasa.UserName);
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
    public async Task DajNedavneAsync_TriOglasaTopDva_VracaDvaNajnovija()
    {
        // Arrange (veći id = kasniji datum objave)
        await Ubaci(OglasMajstora(1), OglasMajstora(2), OglasMajstora(3));

        // Act
        var oglasi = (await _repo.DajNedavneAsync(2)).Select(o => o.OglasID).ToList();

        // Assert
        CollectionAssert.AreEqual(new[] { 3, 2 }, oglasi);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DodajAsync_NoviOglas_SnimaOglasUBazu()
    {
        // Act
        await _repo.DodajAsync(OglasMajstora(1));

        // Assert
        Assert.AreEqual(1, _baza.OglasiMajstora.Count());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DodajAsync_NoviOglas_PostavljaDatumObjaveNaSada()
    {
        // Arrange
        var oglas = OglasMajstora(1);
        var prije = DateTime.UtcNow;

        // Act
        await _repo.DodajAsync(oglas);

        // Assert
        Assert.IsTrue(oglas.DatumObjave >= prije);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DodajAsync_OglasSaCijenom_PostavljaMinCijenuVlasnika()
    {
        // Act
        await _repo.DodajAsync(OglasMajstora(1, minCijena: 45));

        // Assert
        Assert.AreEqual(45, MinCijenaVlasnika());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DodajAsync_ViseOglasa_MinCijenaJeNajnizaCijena()
    {
        // Arrange
        await _repo.DodajAsync(OglasMajstora(1, minCijena: 80));

        // Act
        await _repo.DodajAsync(OglasMajstora(2, minCijena: 30));

        // Assert
        Assert.AreEqual(30, MinCijenaVlasnika());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DodajAsync_CijenaPoDogovoru_MinCijenaVlasnikaOstajeNull()
    {
        // Act: cijena 0 znači "po dogovoru" i ne ulazi u minimalnu cijenu
        await _repo.DodajAsync(OglasMajstora(1, minCijena: 0));

        // Assert
        Assert.IsNull(MinCijenaVlasnika());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DodajAsync_VlasnikNijeIzvrsilac_SnimaOglasBezGreske()
    {
        // Arrange: vlasnik je klijent, pa nema minimalnu cijenu za ažurirati
        _baza.Add(new Klijent { Id = "k1", UserName = "klijent1" });
        await _baza.SaveChangesAsync();

        // Act
        await _repo.DodajAsync(OglasMajstora(1, vlasnikId: "k1"));

        // Assert
        Assert.AreEqual(1, _baza.OglasiMajstora.Count());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediAsync_DeaktiviranOglas_MinCijenaViseNeUkljucujeTajOglas()
    {
        // Arrange
        await _repo.DodajAsync(OglasMajstora(1, minCijena: 30));
        await _repo.DodajAsync(OglasMajstora(2, minCijena: 60));
        _baza.ChangeTracker.Clear();
        var oglas = OglasMajstora(1, status: Status.Neaktivan, minCijena: 30);

        // Act
        await _repo.UrediAsync(oglas);

        // Assert
        Assert.AreEqual(60, MinCijenaVlasnika());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediAsync_IzmijenjenNaslov_SnimaIzmjenu()
    {
        // Arrange
        await Ubaci(OglasMajstora(1));
        var oglas = OglasMajstora(1);
        oglas.Naslov = "Novi naslov";

        // Act
        await _repo.UrediAsync(oglas);

        // Assert
        _baza.ChangeTracker.Clear();
        Assert.AreEqual("Novi naslov", _baza.OglasiMajstora.Single().Naslov);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObrisiAsync_PostojeciOglas_BriseOglas()
    {
        // Arrange
        await Ubaci(OglasMajstora(1));

        // Act
        await _repo.ObrisiAsync(1);

        // Assert
        Assert.AreEqual(0, _baza.OglasiMajstora.Count());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObrisiAsync_JediniOglasSaCijenom_MinCijenaVlasnikaPostajeNull()
    {
        // Arrange
        await _repo.DodajAsync(OglasMajstora(1, minCijena: 50));
        _baza.ChangeTracker.Clear();

        // Act
        await _repo.ObrisiAsync(1);

        // Assert
        Assert.IsNull(MinCijenaVlasnika());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObrisiAsync_NepostojeciOglas_NistaNeBrise()
    {
        // Arrange
        await Ubaci(OglasMajstora(1));

        // Act
        await _repo.ObrisiAsync(99);

        // Assert
        Assert.AreEqual(1, _baza.OglasiMajstora.Count());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    [DataRow("Majstor oglas", 2)]
    [DataRow("bojlera", 2)]
    [DataRow("krov", 0)]
    public async Task IzvrsiPretraguTekstaAsync_TekstUNaslovuIliOpisu_VracaOglaseKojiGaSadrze(string tekst, int ocekivano)
    {
        // Arrange
        await Ubaci(OglasMajstora(1), OglasMajstora(2));

        // Act
        var oglasi = await _repo.IzvrsiPretraguTekstaAsync(tekst);

        // Assert
        Assert.AreEqual(ocekivano, oglasi.Count());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajBrojAktivnihZaKorisnikaAsync_AktivniINeaktivni_BrojiSamoAktivne()
    {
        // Arrange
        await Ubaci(OglasMajstora(1), OglasMajstora(2), OglasMajstora(3, status: Status.Neaktivan));

        // Act
        var broj = await _repo.DajBrojAktivnihZaKorisnikaAsync(VlasnikId);

        // Assert
        Assert.AreEqual(2, broj);
    }
}
