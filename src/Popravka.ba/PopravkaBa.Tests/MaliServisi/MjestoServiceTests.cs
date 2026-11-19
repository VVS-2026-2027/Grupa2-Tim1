using PopravkaBa.Application.Services;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.MaliServisi;

[TestClass]
public class MjestoServiceTests
{
    private Mock<IMjestoRepository> _repo = null!;
    private MjestoService _servis = null!;

    [TestInitialize]
    public void Postavi()
    {
        _repo = new Mock<IMjestoRepository>();
        _servis = new MjestoService(_repo.Object);
    }

    private static Mjesto Mjesto(int id, string naziv) => new() { MjestoID = id, Naziv = naziv };

    [TestMethod]
    public async Task DajSvaMjestaAsync_PozivaRepozitorij_VracaSvaMjesta()
    {
        // Arrange
        var mjesta = new List<Mjesto> { Mjesto(1, "Sarajevo"), Mjesto(2, "Mostar") };
        _repo.Setup(r => r.DajSvaMjestaAsync()).ReturnsAsync(mjesta);

        // Act
        var rezultat = await _servis.DajSvaMjestaAsync();

        // Assert
        Assert.AreEqual(2, rezultat.Count());
        Assert.AreSame(mjesta, rezultat);
    }

    [TestMethod]
    public async Task DajMjestaPoKantonuAsync_ProslijedjenKanton_VracaMjestaIzRepozitorija()
    {
        // Arrange
        var mjesta = new List<Mjesto> { Mjesto(1, "Sarajevo") };
        _repo.Setup(r => r.DajMjestaPoKantonuAsync(9)).ReturnsAsync(mjesta);

        // Act
        var rezultat = await _servis.DajMjestaPoKantonuAsync(9);

        // Assert
        Assert.AreSame(mjesta, rezultat);
    }

    [TestMethod]
    public async Task DajMjestoPoIdAsync_PostojeceMjesto_VracaMjesto()
    {
        // Arrange
        var mjesto = Mjesto(5, "Sarajevo");
        _repo.Setup(r => r.DajPoIdAsync(5)).ReturnsAsync(mjesto);

        // Act
        var rezultat = await _servis.DajMjestoPoIdAsync(5);

        // Assert
        Assert.AreSame(mjesto, rezultat);
    }

    [TestMethod]
    public async Task DajMjestoPoIdAsync_NepostojeceMjesto_VracaNull()
    {
        // Arrange
        _repo.Setup(r => r.DajPoIdAsync(99)).ReturnsAsync((Mjesto?)null);

        // Act
        var rezultat = await _servis.DajMjestoPoIdAsync(99);

        // Assert
        Assert.IsNull(rezultat);
    }

    [TestMethod]
    public async Task PronadjiMjestaAsync_ProslijedjenaPretraga_VracaRezultatIPozivaRepozitorij()
    {
        // Arrange
        var mjesta = new List<Mjesto> { Mjesto(1, "Sarajevo") };
        _repo.Setup(r => r.PronadjiMjestaAsync("sara")).ReturnsAsync(mjesta);

        // Act
        var rezultat = await _servis.PronadjiMjestaAsync("sara");

        // Assert
        Assert.AreSame(mjesta, rezultat);
        _repo.Verify(r => r.PronadjiMjestaAsync("sara"), Times.Once);
    }

    [TestMethod]
    public async Task DajSveKorisnikeMjestaAsync_ProslijedjenIdKorisnika_VracaMjesta()
    {
        // Arrange
        var mjesta = new List<Mjesto> { Mjesto(1, "Sarajevo") };
        _repo.Setup(r => r.DajSveKorisnikeMjestaAsync("k1")).ReturnsAsync(mjesta);

        // Act
        var rezultat = await _servis.DajSveKorisnikeMjestaAsync("k1");

        // Assert
        Assert.AreSame(mjesta, rezultat);
    }

    [TestMethod]
    public async Task DodajMjestaKorisniku_ProslijedjeniPodaci_PozivaRepozitorij()
    {
        // Arrange
        var mjestaId = new List<int> { 1, 2 };

        // Act
        await _servis.DodajMjestaKorisniku("k1", mjestaId);

        // Assert
        _repo.Verify(r => r.DodajMjestaKorisniku("k1", mjestaId), Times.Once);
    }

    [TestMethod]
    public async Task UkloniMjestaKorisniku_ProslijedjenId_PozivaRepozitorij()
    {
        // Act
        await _servis.UkloniMjestaKorisniku("k1");

        // Assert
        _repo.Verify(r => r.UkloniSvaMjestaKorisniku("k1"), Times.Once);
    }

    [TestMethod]
    public async Task AzurirajMjestaKorisnika_ProslijedjeniPodaci_PozivaRepozitorij()
    {
        // Arrange
        var mjestaId = new List<int> { 2, 3 };

        // Act
        await _servis.AzurirajMjestaKorisnika("k1", mjestaId);

        // Assert
        _repo.Verify(r => r.AzurirajMjestaKorisnika("k1", mjestaId), Times.Once);
    }

    [TestMethod]
    public async Task DajOglaseMajstoraPoMjestu_ProslijedjenId_VracaOglase()
    {
        // Arrange
        var oglasi = new List<OglasMajstora> { new() { OglasID = 1 } };
        _repo.Setup(r => r.DajOglaseMajstoraPoMjestu(3)).ReturnsAsync(oglasi);

        // Act
        var rezultat = await _servis.DajOglaseMajstoraPoMjestu(3);

        // Assert
        Assert.AreSame(oglasi, rezultat);
    }

    [TestMethod]
    public async Task DajOglaseRadnogMjestaPoMjestu_ProslijedjenId_VracaOglase()
    {
        // Arrange
        var oglasi = new List<OglasRadnoMjesto> { new() { OglasID = 1 } };
        _repo.Setup(r => r.DajOglaseRadnogMjestaPoMjestu(3)).ReturnsAsync(oglasi);

        // Act
        var rezultat = await _servis.DajOglaseRadnogMjestaPoMjestu(3);

        // Assert
        Assert.AreSame(oglasi, rezultat);
    }

    [TestMethod]
    public async Task DajOglaseUslugePoMjestu_ProslijedjenId_VracaOglase()
    {
        // Arrange
        var oglasi = new List<OglasUsluge> { new() { OglasID = 1 } };
        _repo.Setup(r => r.DajOglaseUslugePoMjestu(3)).ReturnsAsync(oglasi);

        // Act
        var rezultat = await _servis.DajOglaseUslugePoMjestu(3);

        // Assert
        Assert.AreSame(oglasi, rezultat);
    }

    [TestMethod]
    public async Task DajSvaMjestaAsync_RepozitorijBacaIzuzetak_PropagiraGa()
    {
        // Arrange
        _repo.Setup(r => r.DajSvaMjestaAsync()).ThrowsAsync(new InvalidOperationException("greska"));

        // Act & Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _servis.DajSvaMjestaAsync());
    }
}
