using Popravka.ba.Data;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Infrastructure.Repositories;
using static PopravkaBa.Tests.Baza.MjestoRepositoryTestHelper;

namespace PopravkaBa.Tests.Baza;

[TestClass]
public class MjestoRepositoryTests
{
    private ApplicationDbContext _db = null!;
    private MjestoRepository _repo = null!;

    [TestInitialize]
    public void Postavi()
    {
        _db = TestHelpers.NovaBaza();
        _repo = new MjestoRepository(_db);
    }

    [TestCleanup]
    public void Pocisti() => _db.Dispose();

    public static IEnumerable<object[]> MjestaPoId =>
        DataDrivenInputParser.CitajXml<int, string>("TestData/xml/MjestoRepository_poId.xml");

    [TestMethod]
    public async Task DajSvaMjestaAsync_ViseMjesta_VracaSva()
    {
        // Arrange
        _db.Mjesta.AddRange(Mjesto(1, "Sarajevo"), Mjesto(2, "Mostar"));
        await _db.SaveChangesAsync();

        // Act
        var rezultat = await _repo.DajSvaMjestaAsync();

        // Assert
        Assert.AreEqual(2, rezultat.Count());
    }

    [TestMethod]
    public async Task DajSvaMjestaAsync_PraznaBaza_VracaPraznuListu()
    {
        // Act
        var rezultat = await _repo.DajSvaMjestaAsync();

        // Assert
        Assert.AreEqual(0, rezultat.Count());
    }

    [TestMethod]
    [DynamicData(nameof(MjestaPoId))]
    public async Task DajPoIdAsync_PodaciIzXml_VracaOcekivano(int id, string? ocekivaniNaziv)
    {
        // Arrange
        _db.Mjesta.AddRange(Mjesto(1, "Sarajevo"), Mjesto(2, "Mostar"));
        await _db.SaveChangesAsync();

        // Act
        var rezultat = await _repo.DajPoIdAsync(id);

        // Assert
        Assert.AreEqual(ocekivaniNaziv, rezultat?.Naziv);
    }

    [TestMethod]
    public async Task DajMjestaPoKantonuAsync_PostojeciKanton_VracaSamoMjestaUTomKantonu()
    {
        // Arrange
        _db.Mjesta.AddRange(
            Mjesto(1, "Sarajevo", Kanton.Sarajevski),
            Mjesto(2, "Mostar", Kanton.HercegovackoNeretvanski));
        await _db.SaveChangesAsync();

        // Act
        var rezultat = await _repo.DajMjestaPoKantonuAsync((int)Kanton.Sarajevski);

        // Assert
        Assert.AreEqual(1, rezultat!.Count());
        Assert.AreEqual("Sarajevo", rezultat!.Single().Naziv);
    }

    [TestMethod]
    public async Task PronadjiMjestaAsync_PretragaPoNazivu_VracaSamoPoklapajuca()
    {
        // Arrange
        _db.Mjesta.AddRange(Mjesto(1, "Sarajevo"), Mjesto(2, "Mostar"));
        await _db.SaveChangesAsync();

        // Act
        var rezultat = await _repo.PronadjiMjestaAsync("sara");

        // Assert
        Assert.AreEqual(1, rezultat.Count());
        Assert.AreEqual("Sarajevo", rezultat.Single().Naziv);
    }

    [TestMethod]
    public async Task DajSveKorisnikeMjestaAsync_KorisnikImaMjesta_VracaSamoNjegova()
    {
        // Arrange
        var sarajevo = Mjesto(1, "Sarajevo");
        var mostar = Mjesto(2, "Mostar");
        var tuzla = Mjesto(3, "Tuzla");
        _db.Mjesta.AddRange(sarajevo, mostar, tuzla);
        _db.KorisnikMjesto.AddRange(
            new KorisnikMjesto { KorisnikID = "k1", Mjesto = sarajevo },
            new KorisnikMjesto { KorisnikID = "k1", Mjesto = mostar },
            new KorisnikMjesto { KorisnikID = "k2", Mjesto = tuzla });
        await _db.SaveChangesAsync();

        // Act
        var rezultat = await _repo.DajSveKorisnikeMjestaAsync("k1");

        // Assert
        Assert.AreEqual(2, rezultat.Count());
        Assert.IsTrue(rezultat.All(m => m.Naziv != "Tuzla"));
    }

    [TestMethod]
    public async Task DajSveKorisnikeMjestaAsync_KorisnikBezMjesta_VracaPrazno()
    {
        // Arrange
        _db.KorisnikMjesto.Add(new KorisnikMjesto { KorisnikID = "k2", Mjesto = Mjesto(1, "Sarajevo") });
        await _db.SaveChangesAsync();

        // Act
        var rezultat = await _repo.DajSveKorisnikeMjestaAsync("k1");

        // Assert
        Assert.AreEqual(0, rezultat.Count());
    }

    [TestMethod]
    public async Task DodajMjestaKorisniku_NovaMjesta_SnimaRedove()
    {
        // Act
        await _repo.DodajMjestaKorisniku("k1", new List<int> { 1, 2 });

        // Assert
        Assert.AreEqual(2, _db.KorisnikMjesto.Count(km => km.KorisnikID == "k1"));
    }

    [TestMethod]
    public async Task UkloniSvaMjestaKorisniku_ViseKorisnika_BriseSamoTrazenog()
    {
        // Arrange
        _db.KorisnikMjesto.AddRange(
            new KorisnikMjesto { KorisnikID = "k1", MjestoID = 1 },
            new KorisnikMjesto { KorisnikID = "k1", MjestoID = 2 },
            new KorisnikMjesto { KorisnikID = "k2", MjestoID = 1 });
        await _db.SaveChangesAsync();

        // Act
        await _repo.UkloniSvaMjestaKorisniku("k1");

        // Assert
        Assert.AreEqual(0, _db.KorisnikMjesto.Count(km => km.KorisnikID == "k1"));
        Assert.AreEqual(1, _db.KorisnikMjesto.Count(km => km.KorisnikID == "k2"));
    }

    [TestMethod]
    public async Task UkloniSvaMjestaKorisniku_KorisnikBezMjesta_NeBriseNista()
    {
        // Arrange
        _db.KorisnikMjesto.Add(new KorisnikMjesto { KorisnikID = "k2", MjestoID = 1 });
        await _db.SaveChangesAsync();

        // Act
        await _repo.UkloniSvaMjestaKorisniku("k1");

        // Assert
        Assert.AreEqual(1, _db.KorisnikMjesto.Count());
    }

    [TestMethod]
    public async Task AzurirajMjestaKorisnika_PostojecaMjesta_ZamjenjujeSkup()
    {
        // Arrange
        _db.KorisnikMjesto.AddRange(
            new KorisnikMjesto { KorisnikID = "k1", MjestoID = 1 },
            new KorisnikMjesto { KorisnikID = "k1", MjestoID = 2 });
        await _db.SaveChangesAsync();

        // Act
        await _repo.AzurirajMjestaKorisnika("k1", new List<int> { 3 });

        // Assert
        var mjesta = _db.KorisnikMjesto.Where(km => km.KorisnikID == "k1").ToList();
        Assert.AreEqual(1, mjesta.Count);
        Assert.AreEqual(3, mjesta.Single().MjestoID);
    }

    [TestMethod]
    public async Task DajOglaseMajstoraPoMjestu_DvaMjesta_VracaSamoTrazenoMjesto()
    {
        // Arrange
        await DodajOglase();

        // Act
        var rezultat = await _repo.DajOglaseMajstoraPoMjestu(1);

        // Assert
        Assert.AreEqual(1, rezultat.Count());
        Assert.AreEqual("Majstor Sarajevo", rezultat.Single().Naslov);
    }

    [TestMethod]
    public async Task DajOglaseRadnogMjestaPoMjestu_DvaMjesta_VracaSamoTrazenoMjesto()
    {
        // Arrange
        await DodajOglase();

        // Act
        var rezultat = await _repo.DajOglaseRadnogMjestaPoMjestu(1);

        // Assert
        Assert.AreEqual(1, rezultat.Count());
        Assert.AreEqual("Posao Sarajevo", rezultat.Single().Naslov);
    }

    [TestMethod]
    public async Task DajOglaseUslugePoMjestu_DvaMjesta_VracaSamoTrazenoMjesto()
    {
        // Arrange
        await DodajOglase();

        // Act
        var rezultat = await _repo.DajOglaseUslugePoMjestu(1);

        // Assert
        Assert.AreEqual(1, rezultat.Count());
        Assert.AreEqual("Usluga Sarajevo", rezultat.Single().Naslov);
    }

    private async Task DodajOglase()
    {
        var klijent = new Klijent { Id = "k1", UserName = "k1", Email = "k1@test.ba" };
        var sarajevo = Mjesto(1, "Sarajevo");
        var mostar = Mjesto(2, "Mostar");
        _db.Users.Add(klijent);
        _db.Mjesta.AddRange(sarajevo, mostar);
        _db.OglasiMajstora.AddRange(
            new OglasMajstora
            {
                Naslov = "Majstor Sarajevo", Opis = "opis", DatumObjave = DateTime.UtcNow,
                Mjesto = sarajevo, VlasnikOglasa = klijent
            },
            new OglasMajstora
            {
                Naslov = "Majstor Mostar", Opis = "opis", DatumObjave = DateTime.UtcNow,
                Mjesto = mostar, VlasnikOglasa = klijent
            });
        _db.OglasiRadnogMjesta.AddRange(
            new OglasRadnoMjesto
            {
                Naslov = "Posao Sarajevo", Opis = "opis", DatumObjave = DateTime.UtcNow,
                Mjesto = sarajevo, VlasnikOglasa = klijent
            },
            new OglasRadnoMjesto
            {
                Naslov = "Posao Mostar", Opis = "opis", DatumObjave = DateTime.UtcNow,
                Mjesto = mostar, VlasnikOglasa = klijent
            });
        _db.OglasiUsluga.AddRange(
            new OglasUsluge
            {
                Naslov = "Usluga Sarajevo", Opis = "opis", DatumObjave = DateTime.UtcNow,
                Mjesto = sarajevo, VlasnikOglasa = klijent, MaxBudzet = 100
            },
            new OglasUsluge
            {
                Naslov = "Usluga Mostar", Opis = "opis", DatumObjave = DateTime.UtcNow,
                Mjesto = mostar, VlasnikOglasa = klijent, MaxBudzet = 100
            });
        await _db.SaveChangesAsync();
    }
}
