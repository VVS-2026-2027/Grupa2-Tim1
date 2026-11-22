using PopravkaBa.Application.Services;
using PopravkaBa.Application.Services.Interface;
using PopravkaBa.Domain.Models;
using static PopravkaBa.Tests.Oglasi.OglasiTestHelper;

namespace PopravkaBa.Tests.Oglasi;

// Fasada samo usmjerava pozive na servis oglasa i servis kategorija, pa su oba mock
[TestClass]
[TestCategory("Service")]
public class OglasMajstoraFacadeTests
{
    private Mock<IOglasMajstoraService> _oglasi = null!;
    private Mock<IKategorijaService> _kategorije = null!;
    private OglasMajstoraFacade _fasada = null!;

    [TestInitialize]
    public void Postavi()
    {
        _oglasi = new Mock<IOglasMajstoraService>();
        _kategorije = new Mock<IKategorijaService>();
        _fasada = new OglasMajstoraFacade(_oglasi.Object, _kategorije.Object);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajSveOglase_ServisVracaOglase_VracaIsteOglase()
    {
        // Arrange
        var oglasi = new[] { OglasMajstora(1) };
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
        var oglas = OglasMajstora(2);
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
        var oglasi = new[] { OglasMajstora(1) };
        _oglasi.Setup(s => s.PronadjiOglase("bojler", 3)).ReturnsAsync(oglasi);

        // Act
        var rezultat = await _fasada.PronadjiOglase("bojler", 3);

        // Assert
        Assert.AreSame(oglasi, rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajSveKategorije_ServisKategorija_VracaKategorije()
    {
        // Arrange
        var kategorije = new[] { new Kategorija { ID = 1, Naziv = "Vodoinstalacije" } };
        _kategorije.Setup(s => s.DajSveKategorije()).ReturnsAsync(kategorije);

        // Act
        var rezultat = await _fasada.DajSveKategorije();

        // Assert
        Assert.AreSame(kategorije, rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_SaKategorijama_DodajeKategorijeOglasu()
    {
        // Arrange
        var dto = ObjaviMajstoraDto();
        _oglasi.Setup(s => s.ObjaviOglas(dto, VlasnikId)).ReturnsAsync(9);

        // Act
        await _fasada.ObjaviOglas(dto, VlasnikId);

        // Assert
        _kategorije.Verify(s => s.DodajKategorijeOglasu(9, dto.KategorijeID), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_SaKategorijama_DodajeKategorijeIzvrsiocu()
    {
        // Arrange
        var dto = ObjaviMajstoraDto();
        _oglasi.Setup(s => s.ObjaviOglas(dto, VlasnikId)).ReturnsAsync(9);

        // Act
        await _fasada.ObjaviOglas(dto, VlasnikId);

        // Assert
        _kategorije.Verify(s => s.DodajKategorijeIzvrsiocu(VlasnikId, dto.KategorijeID), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_BezKategorija_NeDodajeKategorijeIzvrsiocu()
    {
        // Arrange
        var dto = ObjaviMajstoraDto(new List<int>());

        // Act
        await _fasada.ObjaviOglas(dto, VlasnikId);

        // Assert
        _kategorije.Verify(s => s.DodajKategorijeIzvrsiocu(It.IsAny<string>(), It.IsAny<List<int>>()), Times.Never);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_SnimljenOglas_VracaIdIzServisa()
    {
        // Arrange
        var dto = ObjaviMajstoraDto();
        _oglasi.Setup(s => s.ObjaviOglas(dto, VlasnikId)).ReturnsAsync(9);

        // Act
        var id = await _fasada.ObjaviOglas(dto, VlasnikId);

        // Assert
        Assert.AreEqual(9, id);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_PostojeciOglas_AzuriraKategorijeOglasa()
    {
        // Arrange
        var dto = UrediMajstoraDto(1);
        _oglasi.Setup(s => s.DajOglasPoId(1)).ReturnsAsync(OglasMajstora(1));

        // Act
        await _fasada.UrediOglas(dto);

        // Assert
        _kategorije.Verify(s => s.AzurirajKategorijeOglasa(1, dto.KategorijeID), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_PostojeciOglas_DodajeKategorijeVlasnikuOglasa()
    {
        // Arrange
        var dto = UrediMajstoraDto(1);
        _oglasi.Setup(s => s.DajOglasPoId(1)).ReturnsAsync(OglasMajstora(1, vlasnikId: "m7"));

        // Act
        await _fasada.UrediOglas(dto);

        // Assert
        _kategorije.Verify(s => s.DodajKategorijeIzvrsiocu("m7", dto.KategorijeID), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_OglasNijePronadjen_NeDodajeKategorijeIzvrsiocu()
    {
        // Arrange
        _oglasi.Setup(s => s.DajOglasPoId(1)).ReturnsAsync((OglasMajstora?)null);

        // Act
        await _fasada.UrediOglas(UrediMajstoraDto(1));

        // Assert
        _kategorije.Verify(s => s.DodajKategorijeIzvrsiocu(It.IsAny<string>(), It.IsAny<List<int>>()), Times.Never);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_IspravanDto_ProsljedjujeDtoServisu()
    {
        // Arrange
        var dto = UrediMajstoraDto(1);

        // Act
        await _fasada.UrediOglas(dto);

        // Assert
        _oglasi.Verify(s => s.UrediOglas(dto), Times.Once);
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
