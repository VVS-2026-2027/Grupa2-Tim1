using Microsoft.AspNetCore.Identity;
using PopravkaBa.Application.Services.Implementation;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.VerifikacijaEmaila;

[TestClass]
public class VerifikacijaEmailaServiceTests
{
    // SHA-256 (HEX, velika slova) teksta "sirovi-token-123"
    private const string SiroviToken = "sirovi-token-123";
    private const string HashSirovogTokena = "A81AA4D06D67C8A71A50FEC7796D0008FC87DA4CE398CBCFD94C94F4B6129285";

    private Mock<IVerifikacijskiTokenRepository> _tokenRepo = null!;
    private Mock<UserManager<ApplicationUser>> _userManager = null!;
    private VerifikacijaEmailaService _servis = null!;

    [TestInitialize]
    public void Postavi()
    {
        _tokenRepo = new Mock<IVerifikacijskiTokenRepository>();
        _userManager = TestHelpers.MockUserManager();
        _servis = new VerifikacijaEmailaService(_tokenRepo.Object, _userManager.Object);
    }

    private static ApplicationUser Korisnik() => new() { Id = "k1", Email = "k1@popravka.ba" };

    private static VerifikacijskiToken Token(DateTime? koristen = null, int minutaDoIsteka = 10) => new()
    {
        ID = 1,
        KorisnikID = "k1",
        TokenHash = HashSirovogTokena,
        Tip = TipTokena.PotvrdaEmaila,
        VrijemeGenerisanja = DateTime.UtcNow.AddMinutes(-5),
        VrijemeIsteka = DateTime.UtcNow.AddMinutes(minutaDoIsteka),
        VrijemeKoristenja = koristen
    };

    // Priprema repozitorij i UserManager prema redu iz XML-a
    private void PostaviStanje(bool tokenPostoji, bool iskoristen, bool istekao, bool korisnikPostoji)
    {
        var token = tokenPostoji
            ? Token(iskoristen ? DateTime.UtcNow : null, istekao ? -1 : 10)
            : null;
        _tokenRepo.Setup(r => r.DajTokenPoHashuAsync(It.IsAny<string>(), It.IsAny<TipTokena>())).ReturnsAsync(token);
        _userManager.Setup(m => m.FindByIdAsync("k1")).ReturnsAsync(korisnikPostoji ? Korisnik() : null);
    }

    // ---------- GenerisiTokenAsync ----------

    [TestMethod]
    public async Task GenerisiTokenAsync_NemaPrethodnogTokena_VracaSiroviTokenIKreiraHash()
    {
        // Arrange
        _tokenRepo.Setup(r => r.DajNajnovijiZaKorisnikaAsync("k1", TipTokena.PotvrdaEmaila))
            .ReturnsAsync((VerifikacijskiToken?)null);

        // Act
        var raw = await _servis.GenerisiTokenAsync(Korisnik(), TipTokena.PotvrdaEmaila);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(raw));
        _tokenRepo.Verify(r => r.KreirajAsync(It.Is<VerifikacijskiToken>(t =>
            t.KorisnikID == "k1" &&
            t.Tip == TipTokena.PotvrdaEmaila &&
            t.TokenHash.Length == 64 &&
            t.TokenHash != raw &&
            t.VrijemeIsteka - t.VrijemeGenerisanja == TimeSpan.FromMinutes(15))), Times.Once);
    }

    [TestMethod]
    public async Task GenerisiTokenAsync_NemaPrethodnogTokena_PonistavaStareNeiskoristeneTokene()
    {
        // Arrange
        _tokenRepo.Setup(r => r.DajNajnovijiZaKorisnikaAsync("k1", TipTokena.ResetLozinke))
            .ReturnsAsync((VerifikacijskiToken?)null);

        // Act
        await _servis.GenerisiTokenAsync(Korisnik(), TipTokena.ResetLozinke);

        // Assert
        _tokenRepo.Verify(r => r.PonistiNeiskoristeneAsync("k1", TipTokena.ResetLozinke), Times.Once);
    }

    public static IEnumerable<object[]> SlucajeviCooldowna =>
        DataDrivenInputParser.CitajXml<int, bool>("TestData/xml/VerifikacijaEmailaService_cooldown.xml");

    [TestMethod]
    [DynamicData(nameof(SlucajeviCooldowna))]
    public async Task GenerisiTokenAsync_PodaciIzXml_TokenSeGenerisePoslijeCooldowna(int minuta, bool generisan)
    {
        // Arrange
        var zadnji = Token();
        zadnji.VrijemeGenerisanja = DateTime.UtcNow.AddMinutes(-minuta);
        _tokenRepo.Setup(r => r.DajNajnovijiZaKorisnikaAsync("k1", TipTokena.PotvrdaEmaila)).ReturnsAsync(zadnji);

        // Act
        var raw = await _servis.GenerisiTokenAsync(Korisnik(), TipTokena.PotvrdaEmaila);

        // Assert
        Assert.AreEqual(generisan, raw is not null);
    }

    [TestMethod]
    public async Task GenerisiTokenAsync_ZahtjevUnutarCooldowna_NistaNeKreiraNiNePonistava()
    {
        // Arrange
        var zadnji = Token();
        zadnji.VrijemeGenerisanja = DateTime.UtcNow.AddMinutes(-1);
        _tokenRepo.Setup(r => r.DajNajnovijiZaKorisnikaAsync("k1", TipTokena.PotvrdaEmaila)).ReturnsAsync(zadnji);

        // Act
        await _servis.GenerisiTokenAsync(Korisnik(), TipTokena.PotvrdaEmaila);

        // Assert
        _tokenRepo.Verify(r => r.PonistiNeiskoristeneAsync(It.IsAny<string>(), It.IsAny<TipTokena>()), Times.Never);
        _tokenRepo.Verify(r => r.KreirajAsync(It.IsAny<VerifikacijskiToken>()), Times.Never);
    }

    // ---------- PotvrdiAsync ----------

    public static IEnumerable<object[]> SlucajeviStanjaTokena =>
        DataDrivenInputParser.CitajXml<bool, bool, bool, bool, Status>("TestData/xml/VerifikacijaEmailaService_stanjaTokena.xml");

    [TestMethod]
    [DynamicData(nameof(SlucajeviStanjaTokena))]
    public async Task PotvrdiAsync_PodaciIzXml_VracaOcekivaniStatus(
        bool tokenPostoji, bool iskoristen, bool istekao, bool korisnikPostoji, Status ocekivano)
    {
        // Arrange
        PostaviStanje(tokenPostoji, iskoristen, istekao, korisnikPostoji);

        // Act
        var status = await _servis.PotvrdiAsync(SiroviToken, TipTokena.PotvrdaEmaila);

        // Assert
        Assert.AreEqual(ocekivano, status);
    }

    [TestMethod]
    public async Task PotvrdiAsync_ValidanToken_TrazeTokenPoSha256HashuSirovogTokena()
    {
        // Arrange
        PostaviStanje(tokenPostoji: true, iskoristen: false, istekao: false, korisnikPostoji: true);

        // Act
        await _servis.PotvrdiAsync(SiroviToken, TipTokena.PotvrdaEmaila);

        // Assert
        _tokenRepo.Verify(r => r.DajTokenPoHashuAsync(HashSirovogTokena, TipTokena.PotvrdaEmaila), Times.Once);
    }

    [TestMethod]
    public async Task PotvrdiAsync_ValidanToken_PotvrdjujeTokenZaKorisnika()
    {
        // Arrange
        PostaviStanje(tokenPostoji: true, iskoristen: false, istekao: false, korisnikPostoji: true);

        // Act
        await _servis.PotvrdiAsync(SiroviToken, TipTokena.PotvrdaEmaila);

        // Assert
        _tokenRepo.Verify(r => r.PotvrdiAsync(
            It.Is<VerifikacijskiToken>(t => t.KorisnikID == "k1"),
            It.Is<ApplicationUser>(u => u.Id == "k1")), Times.Once);
    }

    [TestMethod]
    public async Task PotvrdiAsync_IstekaoToken_NePotvrdjujeToken()
    {
        // Arrange
        PostaviStanje(tokenPostoji: true, iskoristen: false, istekao: true, korisnikPostoji: true);

        // Act
        await _servis.PotvrdiAsync(SiroviToken, TipTokena.PotvrdaEmaila);

        // Assert
        _tokenRepo.Verify(r => r.PotvrdiAsync(It.IsAny<VerifikacijskiToken>(), It.IsAny<ApplicationUser>()), Times.Never);
    }

    // ---------- ValidirajResetTokenAsync ----------

    [TestMethod]
    [DynamicData(nameof(SlucajeviStanjaTokena))]
    public async Task ValidirajResetTokenAsync_PodaciIzXml_VracaOcekivaniStatus(
        bool tokenPostoji, bool iskoristen, bool istekao, bool korisnikPostoji, Status ocekivano)
    {
        // Arrange
        PostaviStanje(tokenPostoji, iskoristen, istekao, korisnikPostoji);

        // Act
        var (status, _, _) = await _servis.ValidirajResetTokenAsync(SiroviToken);

        // Assert
        Assert.AreEqual(ocekivano, status);
    }

    [TestMethod]
    public async Task ValidirajResetTokenAsync_ValidanToken_VracaTokenIKorisnika()
    {
        // Arrange
        PostaviStanje(tokenPostoji: true, iskoristen: false, istekao: false, korisnikPostoji: true);

        // Act
        var (_, token, korisnik) = await _servis.ValidirajResetTokenAsync(SiroviToken);

        // Assert
        Assert.IsNotNull(token);
        Assert.AreEqual("k1", korisnik!.Id);
        _tokenRepo.Verify(r => r.DajTokenPoHashuAsync(HashSirovogTokena, TipTokena.ResetLozinke), Times.Once);
    }

    [TestMethod]
    public async Task ValidirajResetTokenAsync_NevalidanToken_VracaNullTokenINullKorisnika()
    {
        // Arrange
        PostaviStanje(tokenPostoji: false, iskoristen: false, istekao: false, korisnikPostoji: true);

        // Act
        var (_, token, korisnik) = await _servis.ValidirajResetTokenAsync(SiroviToken);

        // Assert
        Assert.IsNull(token);
        Assert.IsNull(korisnik);
    }

    // ---------- OznaciKoristenimAsync (mock: metoda ništa ne vraća) ----------

    [TestMethod]
    public async Task OznaciKoristenimAsync_PostojeciToken_PozivaRepozitorijJednom()
    {
        // Arrange
        var token = Token();

        // Act
        await _servis.OznaciKoristenimAsync(token);

        // Assert
        _tokenRepo.Verify(r => r.OznaciKoristenimAsync(token), Times.Once);
    }
}
