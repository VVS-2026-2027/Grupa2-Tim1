using PopravkaBa.Application.Services;
using PopravkaBa.Domain.Interfaces.Repositories;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.MaliServisi;

[TestClass]
public class UvjetOglasaServiceTests
{
    private Mock<IUvjetOglasaRepository> _repo = null!;
    private UvjetOglasaService _servis = null!;

    [TestInitialize]
    public void Postavi()
    {
        _repo = new Mock<IUvjetOglasaRepository>();
        _servis = new UvjetOglasaService(_repo.Object);
    }

    private static UvjetOglasa Uvjet(int id, int oglasId, string tekst) =>
        new() { ID = id, OglasID = oglasId, TekstUvjeta = tekst };

    [TestMethod]
    public async Task DajSveUvjeteOglasa_ProslijedjenId_VracaUvjete()
    {
        // Arrange
        var uvjeti = new List<UvjetOglasa?> { Uvjet(1, 10, "Uvjet A") };
        _repo.Setup(r => r.DajSveUvjeteOglasa(10)).ReturnsAsync(uvjeti);

        // Act
        var rezultat = await _servis.DajSveUvjeteOglasa(10);

        // Assert
        Assert.AreSame(uvjeti, rezultat);
    }

    [TestMethod]
    public async Task DodajUvjeteOglasu_ProslijedjeniPodaci_PozivaRepozitorij()
    {
        // Arrange
        var uvjeti = new List<string> { "Uvjet A", "Uvjet B" };

        // Act
        await _servis.DodajUvjeteOglasu(10, uvjeti);

        // Assert
        _repo.Verify(r => r.DodajUvjeteOglasu(10, uvjeti), Times.Once);
    }

    [TestMethod]
    public async Task AzurirajUvjeteOglasa_ProslijedjeniPodaci_PozivaRepozitorij()
    {
        // Arrange
        var uvjeti = new List<string> { "Novi uvjet" };

        // Act
        await _servis.AzurirajUvjeteOglasa(10, uvjeti);

        // Assert
        _repo.Verify(r => r.AzurirajUvjeteOglasa(10, uvjeti), Times.Once);
    }

    [TestMethod]
    public async Task DajUvjetPoIdAsync_PostojeciUvjet_VracaUvjet()
    {
        // Arrange
        var uvjet = Uvjet(5, 10, "Uvjet A");
        _repo.Setup(r => r.DajUvjetPoIdAsync(5)).ReturnsAsync(uvjet);

        // Act
        var rezultat = await _servis.DajUvjetPoIdAsync(5);

        // Assert
        Assert.AreSame(uvjet, rezultat);
    }

    [TestMethod]
    public async Task DajUvjetPoIdAsync_NepostojeciUvjet_VracaNull()
    {
        // Arrange
        _repo.Setup(r => r.DajUvjetPoIdAsync(99)).ReturnsAsync((UvjetOglasa?)null);

        // Act
        var rezultat = await _servis.DajUvjetPoIdAsync(99);

        // Assert
        Assert.IsNull(rezultat);
    }

    [TestMethod]
    public async Task UkloniSveUvjeteOglasa_ProslijedjenId_PozivaRepozitorij()
    {
        // Act
        await _servis.UkloniSveUvjeteOglasa(10);

        // Assert
        _repo.Verify(r => r.UkloniSveUvjeteOglasa(10), Times.Once);
    }

    [TestMethod]
    public async Task DajSveUvjeteOglasa_RepozitorijBacaIzuzetak_PropagiraGa()
    {
        // Arrange
        _repo.Setup(r => r.DajSveUvjeteOglasa(10)).ThrowsAsync(new InvalidOperationException("greska"));

        // Act & Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _servis.DajSveUvjeteOglasa(10));
    }
}
