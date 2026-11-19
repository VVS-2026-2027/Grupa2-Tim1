using Popravka.ba.Data;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Models;
using PopravkaBa.Infrastructure.Repositories;

namespace PopravkaBa.Tests.Tokeni;

// Repozitorij se testira nad InMemory bazom (nema smisla mockati bazu).
[TestClass]
public class VerifikacijskiTokenRepositoryTests
{
    private ApplicationDbContext _db = null!;
    private VerifikacijskiTokenRepository _repo = null!;

    [TestInitialize]
    public void Postavi()
    {
        _db = TestHelpers.NovaBaza();
        _repo = new VerifikacijskiTokenRepository(_db, TestHelpers.MockUserManager().Object);
    }

    [TestCleanup]
    public void Pocisti() => _db.Dispose();

    private static VerifikacijskiToken Token(
        string hash, string korisnikId = "k1", TipTokena tip = TipTokena.PotvrdaEmaila,
        int minutaOdGenerisanja = 10, DateTime? koristen = null) => new()
    {
        KorisnikID = korisnikId,
        TokenHash = hash,
        Tip = tip,
        VrijemeGenerisanja = DateTime.UtcNow.AddMinutes(-minutaOdGenerisanja),
        VrijemeIsteka = DateTime.UtcNow.AddMinutes(15 - minutaOdGenerisanja),
        VrijemeKoristenja = koristen
    };

    private async Task Dodaj(params VerifikacijskiToken[] tokeni)
    {
        _db.Tokeni.AddRange(tokeni);
        await _db.SaveChangesAsync();
    }

    // ---------- DajNajnovijiZaKorisnikaAsync ----------

    [TestMethod]
    public async Task DajNajnovijiZaKorisnikaAsync_ViseTokena_VracaNajnovijiTogTipa()
    {
        // Arrange
        await Dodaj(
            Token("h-star", minutaOdGenerisanja: 30),
            Token("h-novi", minutaOdGenerisanja: 2),
            Token("h-reset", tip: TipTokena.ResetLozinke, minutaOdGenerisanja: 1),
            Token("h-tudji", korisnikId: "k2", minutaOdGenerisanja: 0));

        // Act
        var rezultat = await _repo.DajNajnovijiZaKorisnikaAsync("k1", TipTokena.PotvrdaEmaila);

        // Assert
        Assert.AreEqual("h-novi", rezultat!.TokenHash);
    }

    [TestMethod]
    public async Task DajNajnovijiZaKorisnikaAsync_KorisnikBezTokena_VracaNull()
    {
        // Arrange
        await Dodaj(Token("h1", korisnikId: "k2"));

        // Act
        var rezultat = await _repo.DajNajnovijiZaKorisnikaAsync("k1", TipTokena.PotvrdaEmaila);

        // Assert
        Assert.IsNull(rezultat);
    }

    // ---------- DajTokenPoHashuAsync ----------

    [TestMethod]
    public async Task DajTokenPoHashuAsync_PostojeciHashITip_VracaToken()
    {
        // Arrange
        await Dodaj(Token("h1"), Token("h2"));

        // Act
        var rezultat = await _repo.DajTokenPoHashuAsync("h2", TipTokena.PotvrdaEmaila);

        // Assert
        Assert.AreEqual("h2", rezultat!.TokenHash);
    }

    [TestMethod]
    public async Task DajTokenPoHashuAsync_NepostojeciHash_VracaNull()
    {
        // Arrange
        await Dodaj(Token("h1"));

        // Act
        var rezultat = await _repo.DajTokenPoHashuAsync("nepostojeci", TipTokena.PotvrdaEmaila);

        // Assert
        Assert.IsNull(rezultat);
    }

    [TestMethod]
    public async Task DajTokenPoHashuAsync_PostojeciHashAliDrugiTip_VracaNull()
    {
        // Arrange
        await Dodaj(Token("h1", tip: TipTokena.PotvrdaEmaila));

        // Act
        var rezultat = await _repo.DajTokenPoHashuAsync("h1", TipTokena.ResetLozinke);

        // Assert
        Assert.IsNull(rezultat);
    }

    // ---------- KreirajAsync ----------

    [TestMethod]
    public async Task KreirajAsync_NoviToken_SnimaTokenUBazu()
    {
        // Arrange
        var token = Token("h-novi");

        // Act
        await _repo.KreirajAsync(token);

        // Assert
        Assert.AreEqual(1, _db.Tokeni.Count(t => t.TokenHash == "h-novi"));
    }

    // ---------- PotvrdiAsync ----------

    [TestMethod]
    public async Task PotvrdiAsync_NeiskoristenToken_PotvrdjujeEmailIOznacavaTokenKoristenim()
    {
        // Arrange
        var token = Token("h1");
        await Dodaj(token);
        var korisnik = new Klijent { Id = "k1", EmailConfirmed = false };

        // Act
        await _repo.PotvrdiAsync(token, korisnik);

        // Assert
        Assert.IsTrue(korisnik.EmailConfirmed);
        Assert.IsNotNull(token.VrijemeKoristenja);
    }

    [TestMethod]
    public async Task PotvrdiAsync_NeiskoristenToken_KorisnikPostajeAktivan()
    {
        // Arrange
        var token = Token("h1");
        await Dodaj(token);
        var korisnik = new Klijent { Id = "k1", EmailConfirmed = false };

        // Act
        await _repo.PotvrdiAsync(token, korisnik);

        // Assert
        Assert.AreEqual(Status.Aktivan, korisnik.StatusVerifikacije);
    }

    // ---------- PonistiNeiskoristeneAsync ----------

    [TestMethod]
    public async Task PonistiNeiskoristeneAsync_ViseTokena_PonistavaSamoNeiskoristeneTogKorisnikaITipa()
    {
        // Arrange
        await Dodaj(
            Token("h-1"),
            Token("h-2"),
            Token("h-koristen", koristen: DateTime.UtcNow.AddMinutes(-5)),
            Token("h-drugi-tip", tip: TipTokena.ResetLozinke),
            Token("h-drugi-korisnik", korisnikId: "k2"));

        // Act
        var ponisteni = await _repo.PonistiNeiskoristeneAsync("k1", TipTokena.PotvrdaEmaila);

        // Assert
        Assert.AreEqual(2, ponisteni.Count);
        Assert.IsTrue(ponisteni.All(t => t.VrijemeKoristenja != null));
    }

    [TestMethod]
    public async Task PonistiNeiskoristeneAsync_ViseTokena_NeDiraTokeneDrugogKorisnikaITipa()
    {
        // Arrange
        await Dodaj(
            Token("h-1"),
            Token("h-drugi-tip", tip: TipTokena.ResetLozinke),
            Token("h-drugi-korisnik", korisnikId: "k2"));

        // Act
        await _repo.PonistiNeiskoristeneAsync("k1", TipTokena.PotvrdaEmaila);

        // Assert
        Assert.AreEqual(2, _db.Tokeni.Count(t => t.VrijemeKoristenja == null));
    }

    [TestMethod]
    public async Task PonistiNeiskoristeneAsync_NemaTokena_VracaPraznuListu()
    {
        // Act
        var ponisteni = await _repo.PonistiNeiskoristeneAsync("k1", TipTokena.PotvrdaEmaila);

        // Assert
        Assert.AreEqual(0, ponisteni.Count);
    }

    // ---------- OznaciKoristenimAsync ----------

    [TestMethod]
    public async Task OznaciKoristenimAsync_NeiskoristenToken_PostavljaVrijemeKoristenja()
    {
        // Arrange
        var token = Token("h1");
        await Dodaj(token);

        // Act
        await _repo.OznaciKoristenimAsync(token);

        // Assert
        Assert.IsNotNull(_db.Tokeni.Single(t => t.TokenHash == "h1").VrijemeKoristenja);
    }
}
