using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Popravka.ba.Data;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Domain.Specifications.Interface;
using PopravkaBa.Infrastructure.Repositories;

namespace PopravkaBa.Tests.Profil;

[TestClass]
public class IzvrsilacUslugeRepositoryTests
{
    private ApplicationDbContext _db = null!;
    private IzvrsilacUslugeRepository _repo = null!;

    [TestInitialize]
    public void Postavi()
    {
        _db = TestHelpers.NovaBaza();
        _repo = new IzvrsilacUslugeRepository(_db);
    }

    [TestCleanup]
    public void Pocisti() => _db.Dispose();

    private static Majstor Majstor(string id, decimal ocjena = 0m, int zavrsenih = 0, string opis = "opis") => new()
    {
        Id = id,
        UserName = id,
        Email = $"{id}@test.ba",
        EmailConfirmed = true,
        Ime = "Test",
        Prezime = "Test",
        Adresa = "",
        ProsjecnaOcjena = ocjena,
        BrojZavrsenihPoslova = zavrsenih,
        Opis = opis
    };

    public static IEnumerable<object[]> Stranice =>
        DataDrivenInputParser.CitajXml<int, int, int, int>("TestData/xml/IzvrsilacUslugeRepository_stranice.xml");

    [TestMethod]
    public async Task DajProfilPoIdAsync_PostojeciMajstor_VracaProfilSaNavigacijama()
    {
        // Arrange
        var majstor = Majstor("m1");
        var kategorija = new Kategorija { Naziv = "Vodoinstalacije" };
        var mjesto = new Mjesto { Naziv = "Sarajevo", Kanton = Kanton.Sarajevski };
        var klijent = new Klijent { Id = "k1", UserName = "k1", Email = "k1@test.ba" };
        _db.Users.AddRange(majstor, klijent);
        _db.Kategorije.Add(kategorija);
        _db.Mjesta.Add(mjesto);
        _db.IzvrsilacKategorija.Add(new IzvrsilacKategorija { IzvrsilacID = "m1", Kategorija = kategorija });
        _db.KorisnikMjesto.Add(new KorisnikMjesto { KorisnikID = "m1", Mjesto = mjesto });
        _db.PortfolioSlika.Add(new PortfolioSlika { IzvrsilacID = "m1", URL = "slika.png" });
        _db.Recenzije.Add(new Recenzija { KlijentID = "k1", IzvrsilacID = "m1", Ocjena = 5, Komentar = "ok" });
        await _db.SaveChangesAsync();

        // Act
        var profil = await _repo.DajProfilPoIdAsync("m1");

        // Assert
        Assert.IsNotNull(profil);
        Assert.AreEqual(1, profil.Kategorije!.Count);
        Assert.AreEqual(1, profil.SlikePortfolija!.Count);
        Assert.AreEqual(1, profil.Mjesta!.Count);
        Assert.AreEqual(1, profil.Recenzije!.Count);
    }

    [TestMethod]
    public async Task DajProfilPoIdAsync_NepostojeciId_VracaNull()
    {
        // Act
        var rezultat = await _repo.DajProfilPoIdAsync("nema");

        // Assert
        Assert.IsNull(rezultat);
    }

    [TestMethod]
    [DynamicData(nameof(Stranice))]
    public async Task PronadjiAsync_StraniceIzXml_VracaOcekivaniBrojIUkupno(
        int stranica, int stavkiPoStranici, int ocekivanoNaStranici, int ukupno)
    {
        // Arrange
        _db.Users.AddRange(Majstor("m1"), Majstor("m2"), Majstor("m3"), Majstor("m4"), Majstor("m5"));
        await _db.SaveChangesAsync();
        var spec = new Mock<ISpecification<IzvrsilacUsluge>>();
        spec.Setup(s => s.ToExpression()).Returns(i => true);

        // Act
        var rezultat = await _repo.PronadjiAsync(spec.Object, stranica, stavkiPoStranici);

        // Assert
        Assert.AreEqual(ocekivanoNaStranici, rezultat.Stavke.Count);
        Assert.AreEqual(ukupno, rezultat.Ukupno);
    }

    [TestMethod]
    public async Task PronadjiAsync_OrderBy_RastuceSortiranje()
    {
        // Arrange
        _db.Users.AddRange(Majstor("m1", 4m), Majstor("m2", 2m), Majstor("m3", 5m));
        await _db.SaveChangesAsync();
        Expression<Func<IzvrsilacUsluge, object>> sortiranje = i => i.ProsjecnaOcjena;
        var spec = new Mock<ISpecification<IzvrsilacUsluge>>();
        spec.Setup(s => s.ToExpression()).Returns(i => true);
        spec.SetupGet(s => s.OrderBy).Returns(sortiranje);

        // Act
        var rezultat = await _repo.PronadjiAsync(spec.Object, 1, 10);

        // Assert
        Assert.AreEqual("m2", rezultat.Stavke.First().Id);
    }

    [TestMethod]
    public async Task PronadjiAsync_OrderByDescending_OpadajuceSortiranje()
    {
        // Arrange
        _db.Users.AddRange(Majstor("m1", 4m), Majstor("m2", 2m), Majstor("m3", 5m));
        await _db.SaveChangesAsync();
        Expression<Func<IzvrsilacUsluge, object>> sortiranje = i => i.ProsjecnaOcjena;
        var spec = new Mock<ISpecification<IzvrsilacUsluge>>();
        spec.Setup(s => s.ToExpression()).Returns(i => true);
        spec.SetupGet(s => s.OrderByDescending).Returns(sortiranje);

        // Act
        var rezultat = await _repo.PronadjiAsync(spec.Object, 1, 10);

        // Assert
        Assert.AreEqual("m3", rezultat.Stavke.First().Id);
    }

    [TestMethod]
    public async Task UrediAsync_NepostojeciIzvrsilac_NeMijenjaNista()
    {
        // Act
        await _repo.UrediAsync(Majstor("nema"));

        // Assert
        Assert.AreEqual(0, await _db.Users.OfType<Majstor>().CountAsync());
    }

    [TestMethod]
    public async Task UrediAsync_PostojeciIzvrsilac_MijenjaOpis()
    {
        // Arrange
        _db.Users.Add(Majstor("m1", opis: "stari"));
        await _db.SaveChangesAsync();

        // Act
        await _repo.UrediAsync(Majstor("m1", opis: "novi"));

        // Assert
        var sacuvan = await _db.Users.OfType<Majstor>().SingleAsync(m => m.Id == "m1");
        Assert.AreEqual("novi", sacuvan.Opis);
    }

    [TestMethod]
    public async Task UrediAsync_PostojecaSlikaNijeUListi_BriseSe()
    {
        // Arrange
        _db.Users.Add(Majstor("m1"));
        _db.PortfolioSlika.Add(new PortfolioSlika { IzvrsilacID = "m1", URL = "stara.png" });
        await _db.SaveChangesAsync();
        var izmijenjen = Majstor("m1");
        izmijenjen.SlikePortfolija = new List<PortfolioSlika>
        {
            new() { IzvrsilacID = "m1", URL = "nova.png" }
        };

        // Act
        await _repo.UrediAsync(izmijenjen);

        // Assert
        var slike = await _db.PortfolioSlika.Where(s => s.IzvrsilacID == "m1").ToListAsync();
        Assert.AreEqual(1, slike.Count);
        Assert.AreEqual("nova.png", slike.Single().URL);
    }

    [TestMethod]
    public async Task UrediAsync_NovaSlikaBezId_DodajeSe()
    {
        // Arrange
        _db.Users.Add(Majstor("m1"));
        await _db.SaveChangesAsync();
        var izmijenjen = Majstor("m1");
        izmijenjen.SlikePortfolija = new List<PortfolioSlika>
        {
            new() { IzvrsilacID = "m1", URL = "nova.png" }
        };

        // Act
        await _repo.UrediAsync(izmijenjen);

        // Assert
        Assert.AreEqual(1, await _db.PortfolioSlika.CountAsync(s => s.IzvrsilacID == "m1"));
    }

    [TestMethod]
    public async Task UrediAsync_NullListaSlica_BriseSveSlike()
    {
        // Arrange
        _db.Users.Add(Majstor("m1"));
        _db.PortfolioSlika.AddRange(
            new PortfolioSlika { IzvrsilacID = "m1", URL = "a.png" },
            new PortfolioSlika { IzvrsilacID = "m1", URL = "b.png" });
        await _db.SaveChangesAsync();
        var izmijenjen = Majstor("m1");
        izmijenjen.SlikePortfolija = null;

        // Act
        await _repo.UrediAsync(izmijenjen);

        // Assert
        Assert.AreEqual(0, await _db.PortfolioSlika.CountAsync(s => s.IzvrsilacID == "m1"));
    }

    [TestMethod]
    public async Task OsvjeziOcjeneAsync_PostojeciIzvrsilac_AzuriraOcjenuIBrojRecenzija()
    {
        // Arrange
        _db.Users.Add(Majstor("m1"));
        await _db.SaveChangesAsync();

        // Act
        await _repo.OsvjeziOcjeneAsync("m1", 4.5m, 10);

        // Assert
        var sacuvan = await _db.Users.OfType<Majstor>().SingleAsync(m => m.Id == "m1");
        Assert.AreEqual(4.5m, sacuvan.ProsjecnaOcjena);
        Assert.AreEqual(10, sacuvan.BrojRecenzija);
    }

    [TestMethod]
    public async Task OsvjeziOcjeneAsync_NepostojeciIzvrsilac_NeMijenjaNista()
    {
        // Act
        await _repo.OsvjeziOcjeneAsync("nema", 4.5m, 10);

        // Assert
        Assert.AreEqual(0, await _db.Users.OfType<Majstor>().CountAsync());
    }

    [TestMethod]
    public async Task PovecajBrojZavrsenihPoslovaAsync_PostojeciIzvrsilac_PovecavaBroj()
    {
        // Arrange
        _db.Users.Add(Majstor("m1", zavrsenih: 2));
        await _db.SaveChangesAsync();

        // Act
        await _repo.PovecajBrojZavrsenihPoslovaAsync("m1");

        // Assert
        var sacuvan = await _db.Users.OfType<Majstor>().SingleAsync(m => m.Id == "m1");
        Assert.AreEqual(3, sacuvan.BrojZavrsenihPoslova);
    }

    [TestMethod]
    public async Task PovecajBrojZavrsenihPoslovaAsync_NepostojeciIzvrsilac_NeMijenjaNista()
    {
        // Act
        await _repo.PovecajBrojZavrsenihPoslovaAsync("nema");

        // Assert
        Assert.AreEqual(0, await _db.Users.OfType<Majstor>().CountAsync());
    }

    [TestMethod]
    public void DajSveAsync_NijeImplementirano_BacaNotImplementedException()
    {
        // Act & Assert
        Assert.ThrowsExactly<NotImplementedException>(() => _repo.DajSveAsync());
    }

    [TestMethod]
    public void DajPoIdAsync_NijeImplementirano_BacaNotImplementedException()
    {
        // Act & Assert
        Assert.ThrowsExactly<NotImplementedException>(() => _repo.DajPoIdAsync(1));
    }

    [TestMethod]
    public void DodajAsync_NijeImplementirano_BacaNotImplementedException()
    {
        // Act & Assert
        Assert.ThrowsExactly<NotImplementedException>(() => _repo.DodajAsync(Majstor("m1")));
    }

    [TestMethod]
    public void ObrisiAsync_NijeImplementirano_BacaNotImplementedException()
    {
        // Act & Assert
        Assert.ThrowsExactly<NotImplementedException>(() => _repo.ObrisiAsync(1));
    }

    [TestMethod]
    public void IzvrsiPretraguTekstaAsync_NijeImplementirano_BacaNotImplementedException()
    {
        // Act & Assert
        Assert.ThrowsExactly<NotImplementedException>(() => _repo.IzvrsiPretraguTekstaAsync("test"));
    }
}
