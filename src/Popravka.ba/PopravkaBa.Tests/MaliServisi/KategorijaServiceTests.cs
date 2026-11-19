using PopravkaBa.Application.Services;
using PopravkaBa.Domain.Interfaces.Repositories;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.MaliServisi;

[TestClass]
public class KategorijaServiceTests
{
    private Mock<IKategorijaRepository> _repo = null!;
    private KategorijaService _servis = null!;

    [TestInitialize]
    public void Postavi()
    {
        _repo = new Mock<IKategorijaRepository>();
        _servis = new KategorijaService(_repo.Object);
    }

    private static Kategorija Kategorija(int id, string naziv) => new() { ID = id, Naziv = naziv };

    [TestMethod]
    public async Task DajSveKategorije_PozivaRepozitorij_VracaSveKategorije()
    {
        // Arrange
        var kategorije = new List<Kategorija> { Kategorija(1, "Vodoinstalacije") };
        _repo.Setup(r => r.DajSveAsync()).ReturnsAsync(kategorije);

        // Act
        var rezultat = await _servis.DajSveKategorije();

        // Assert
        Assert.AreSame(kategorije, rezultat);
    }

    [TestMethod]
    public async Task DodajKategorijeOglasu_ProslijedjeniPodaci_PozivaRepozitorij()
    {
        // Arrange
        var kategorijeId = new List<int> { 1, 2 };

        // Act
        await _servis.DodajKategorijeOglasu(10, kategorijeId);

        // Assert
        _repo.Verify(r => r.DodajKategorijeOglasu(10, kategorijeId), Times.Once);
    }

    [TestMethod]
    public async Task UkloniSveKategorijeOglasa_ProslijedjenId_PozivaRepozitorij()
    {
        // Act
        await _servis.UkloniSveKategorijeOglasa(10);

        // Assert
        _repo.Verify(r => r.UkloniSveKategorijeOglasa(10), Times.Once);
    }

    [TestMethod]
    public async Task AzurirajKategorijeOglasa_ProslijedjeniPodaci_PozivaRepozitorij()
    {
        // Arrange
        var kategorijeId = new List<int> { 2, 3 };

        // Act
        await _servis.AzurirajKategorijeOglasa(10, kategorijeId);

        // Assert
        _repo.Verify(r => r.AzurirajKategorijeOglasa(10, kategorijeId), Times.Once);
    }

    [TestMethod]
    public async Task DodajKategorijeIzvrsiocu_ProslijedjeniPodaci_PozivaRepozitorij()
    {
        // Arrange
        var kategorijeId = new List<int> { 1, 2 };

        // Act
        await _servis.DodajKategorijeIzvrsiocu("m1", kategorijeId);

        // Assert
        _repo.Verify(r => r.DodajKategorijeIzvrsiocu("m1", kategorijeId), Times.Once);
    }

    [TestMethod]
    public async Task UkloniSveKategorijeIzvrsiocu_ProslijedjenId_PozivaRepozitorij()
    {
        // Act
        await _servis.UkloniSveKategorijeIzvrsiocu("m1");

        // Assert
        _repo.Verify(r => r.UkloniSveKategorijeIzvrsiocu("m1"), Times.Once);
    }

    [TestMethod]
    public async Task AzurirajKategorijeIzvrsioca_ProslijedjeniPodaci_PozivaRepozitorij()
    {
        // Arrange
        var kategorijeId = new List<int> { 3, 4 };

        // Act
        await _servis.AzurirajKategorijeIzvrsioca("m1", kategorijeId);

        // Assert
        _repo.Verify(r => r.AzurirajKategorijeIzvrsioca("m1", kategorijeId), Times.Once);
    }

    [TestMethod]
    public async Task DajTopKategorijePoMajstorimaAsync_ProslijedjenTopN_VracaRezultat()
    {
        // Arrange
        var rezultatRepozitorija = new List<(Kategorija, int)>
        {
            (Kategorija(1, "Vodoinstalacije"), 3)
        };
        _repo.Setup(r => r.DajTopKategorijePoMajstorimaAsync(5)).ReturnsAsync(rezultatRepozitorija);

        // Act
        var rezultat = await _servis.DajTopKategorijePoMajstorimaAsync(5);

        // Assert
        Assert.AreSame(rezultatRepozitorija, rezultat);
    }

    [TestMethod]
    public async Task DajSveKategorije_RepozitorijBacaIzuzetak_PropagiraGa()
    {
        // Arrange
        _repo.Setup(r => r.DajSveAsync()).ThrowsAsync(new InvalidOperationException("greska"));

        // Act & Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _servis.DajSveKategorije());
    }
}
