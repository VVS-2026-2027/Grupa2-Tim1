using PopravkaBa.Application.Services;
using PopravkaBa.Application.Services.Interface;
using PopravkaBa.Domain.Models;
using static PopravkaBa.Tests.Oglasi.OglasiTestHelper;

namespace PopravkaBa.Tests.Oglasi;

[TestClass]
[TestCategory("Service")]
public class OglasUslugeFacadeTests
{
    private Mock<IOglasUslugeService> _oglasi = null!;
    private Mock<IKategorijaService> _kategorije = null!;
    private OglasUslugeFacade _fasada = null!;

    [TestInitialize]
    public void Postavi()
    {
        _oglasi = new Mock<IOglasUslugeService>();
        _kategorije = new Mock<IKategorijaService>();
        _fasada = new OglasUslugeFacade(_oglasi.Object, _kategorije.Object);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajSveOglase_ServisVracaOglase_VracaIsteOglase()
    {
        // Arrange
        var oglasi = new[] { OglasUsluge(1) };
        _oglasi.Setup(s => s.DajSveOglase()).ReturnsAsync(oglasi);

        // Act
        var rezultat = await _fasada.DajSveOglase();

        // Assert
        Assert.AreSame(oglasi, rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajOglasPoId_PostojeciOglas_VracaOglasIzServisa()
    {
        // Arrange
        var oglas = OglasUsluge(2);
        _oglasi.Setup(s => s.DajOglasPoId(2)).ReturnsAsync(oglas);

        // Act
        var rezultat = await _fasada.DajOglasPoId(2);

        // Assert
        Assert.AreSame(oglas, rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task PronadjiOglase_TekstILokacija_ProsljedjujeServisu()
    {
        // Arrange
        var oglasi = new[] { OglasUsluge(1) };
        _oglasi.Setup(s => s.PronadjiOglase("krečenje", 2)).ReturnsAsync(oglasi);

        // Act
        var rezultat = await _fasada.PronadjiOglase("krečenje", 2);

        // Assert
        Assert.AreSame(oglasi, rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajSveKategorije_ServisKategorija_VracaKategorije()
    {
        // Arrange
        var kategorije = new[] { new Kategorija { ID = 1, Naziv = "Soboslikarstvo" } };
        _kategorije.Setup(s => s.DajSveKategorije()).ReturnsAsync(kategorije);

        // Act
        var rezultat = await _fasada.DajSveKategorije();

        // Assert
        Assert.AreSame(kategorije, rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_IspravanDto_DodajeKategorijeNovomOglasu()
    {
        // Arrange
        var dto = ObjaviUslugeDto();
        _oglasi.Setup(s => s.ObjaviOglas(dto, VlasnikId)).ReturnsAsync(6);

        // Act
        await _fasada.ObjaviOglas(dto, VlasnikId);

        // Assert
        _kategorije.Verify(s => s.DodajKategorijeOglasu(6, dto.KategorijeID), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_SnimljenOglas_VracaIdIzServisa()
    {
        // Arrange
        var dto = ObjaviUslugeDto();
        _oglasi.Setup(s => s.ObjaviOglas(dto, VlasnikId)).ReturnsAsync(6);

        // Act
        var id = await _fasada.ObjaviOglas(dto, VlasnikId);

        // Assert
        Assert.AreEqual(6, id);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_IspravanDto_UredjujeOglasIAzuriraKategorije()
    {
        // Arrange
        var dto = UrediUslugeDto(1);

        // Act
        await _fasada.UrediOglas(dto);

        // Assert
        _oglasi.Verify(s => s.UrediOglas(dto), Times.Once);
        _kategorije.Verify(s => s.AzurirajKategorijeOglasa(1, dto.KategorijeID), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObrisiOglas_IdOglasa_ProsljedjujeServisu()
    {
        // Act
        await _fasada.ObrisiOglas(4);

        // Assert
        _oglasi.Verify(s => s.ObrisiOglas(4), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajBrojZavrsenihAsync_ServisVracaBroj_VracaIstiBroj()
    {
        // Arrange
        _oglasi.Setup(s => s.DajBrojZavrsenihAsync()).ReturnsAsync(8);

        // Act
        var broj = await _fasada.DajBrojZavrsenihAsync();

        // Assert
        Assert.AreEqual(8, broj);
    }
}
