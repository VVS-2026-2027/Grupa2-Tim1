using PopravkaBa.Application.Services;
using PopravkaBa.Application.Services.Interface;
using PopravkaBa.Domain.Models;
using static PopravkaBa.Tests.Oglasi.OglasiTestHelper;

namespace PopravkaBa.Tests.Oglasi;

[TestClass]
[TestCategory("Service")]
public class OglasRadnoMjestoFacadeTests
{
    private Mock<IOglasRadnoMjestoService> _oglasi = null!;
    private Mock<IKategorijaService> _kategorije = null!;
    private Mock<IUvjetOglasaService> _uvjeti = null!;
    private Mock<IVozackeDozvoleService> _dozvole = null!;
    private OglasRadnoMjestoFacade _fasada = null!;

    [TestInitialize]
    public void Postavi()
    {
        _oglasi = new Mock<IOglasRadnoMjestoService>();
        _kategorije = new Mock<IKategorijaService>();
        _uvjeti = new Mock<IUvjetOglasaService>();
        _dozvole = new Mock<IVozackeDozvoleService>();
        _fasada = new OglasRadnoMjestoFacade(_oglasi.Object, _kategorije.Object, _uvjeti.Object, _dozvole.Object);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajSveOglase_ServisVracaOglase_VracaIsteOglase()
    {
        // Arrange
        var oglasi = new[] { OglasRadnoMjesto(1) };
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
        var oglas = OglasRadnoMjesto(2);
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
        var oglasi = new[] { OglasRadnoMjesto(1) };
        _oglasi.Setup(s => s.PronadjiOglase("električar", 1)).ReturnsAsync(oglasi);

        // Act
        var rezultat = await _fasada.PronadjiOglase("električar", 1);

        // Assert
        Assert.AreSame(oglasi, rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajSveKategorije_ServisKategorija_VracaKategorije()
    {
        // Arrange
        var kategorije = new[] { new Kategorija { ID = 1, Naziv = "Elektroinstalacije" } };
        _kategorije.Setup(s => s.DajSveKategorije()).ReturnsAsync(kategorije);

        // Act
        var rezultat = await _fasada.DajSveKategorije();

        // Assert
        Assert.AreSame(kategorije, rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajSveUvjeteOglasa_IdOglasa_VracaUvjeteIzServisa()
    {
        // Arrange
        var uvjeti = new[] { new UvjetOglasa { OglasID = 1, TekstUvjeta = "Vozačka B" } };
        _uvjeti.Setup(s => s.DajSveUvjeteOglasa(1)).ReturnsAsync(uvjeti);

        // Act
        var rezultat = await _fasada.DajSveUvjeteOglasa(1);

        // Assert
        Assert.AreSame(uvjeti, rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_IspravanDto_DodajeKategorijeNovomOglasu()
    {
        // Arrange
        var dto = ObjaviRadnoMjestoDto();
        _oglasi.Setup(s => s.ObjaviOglas(dto, VlasnikId)).ReturnsAsync(12);

        // Act
        await _fasada.ObjaviOglas(dto, VlasnikId);

        // Assert
        _kategorije.Verify(s => s.DodajKategorijeOglasu(12, dto.KategorijeID), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_IspravanDto_DodajeUvjeteNovomOglasu()
    {
        // Arrange
        var dto = ObjaviRadnoMjestoDto();
        _oglasi.Setup(s => s.ObjaviOglas(dto, VlasnikId)).ReturnsAsync(12);

        // Act
        await _fasada.ObjaviOglas(dto, VlasnikId);

        // Assert
        _uvjeti.Verify(s => s.DodajUvjeteOglasu(12, dto.Uvjeti), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_IspravanDto_DodajeVozackeDozvoleNovomOglasu()
    {
        // Arrange
        var dto = ObjaviRadnoMjestoDto();
        _oglasi.Setup(s => s.ObjaviOglas(dto, VlasnikId)).ReturnsAsync(12);

        // Act
        await _fasada.ObjaviOglas(dto, VlasnikId);

        // Assert
        _dozvole.Verify(s => s.DodajVozackeDozvoleOglasu(12, dto.VozackeDozvole), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_SnimljenOglas_VracaIdIzServisa()
    {
        // Arrange
        var dto = ObjaviRadnoMjestoDto();
        _oglasi.Setup(s => s.ObjaviOglas(dto, VlasnikId)).ReturnsAsync(12);

        // Act
        var id = await _fasada.ObjaviOglas(dto, VlasnikId);

        // Assert
        Assert.AreEqual(12, id);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_IspravanDto_ProsljedjujeDtoServisu()
    {
        // Arrange
        var dto = UrediRadnoMjestoDto(1);

        // Act
        await _fasada.UrediOglas(dto);

        // Assert
        _oglasi.Verify(s => s.UrediOglas(dto), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_IspravanDto_AzuriraKategorijeUvjeteIDozvole()
    {
        // Arrange
        var dto = UrediRadnoMjestoDto(1);

        // Act
        await _fasada.UrediOglas(dto);

        // Assert
        _kategorije.Verify(s => s.AzurirajKategorijeOglasa(1, dto.KategorijeID), Times.Once);
        _uvjeti.Verify(s => s.AzurirajUvjeteOglasa(1, dto.Uvjeti), Times.Once);
        _dozvole.Verify(s => s.AzurirajVozackeDozvoleOglasa(1, dto.VozackeDozvole), Times.Once);
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
}
