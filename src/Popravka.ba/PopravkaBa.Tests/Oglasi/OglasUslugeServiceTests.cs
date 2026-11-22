using PopravkaBa.Application.Services;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using static PopravkaBa.Tests.Oglasi.OglasiTestHelper;

namespace PopravkaBa.Tests.Oglasi;

[TestClass]
[TestCategory("Service")]
public class OglasUslugeServiceTests
{
    private Mock<IOglasUslugeRepository> _repo = null!;
    private OglasUslugeService _servis = null!;

    [TestInitialize]
    public void Postavi()
    {
        _repo = new Mock<IOglasUslugeRepository>();
        _servis = new OglasUslugeService(_repo.Object);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajSveOglase_RepozitorijVracaOglase_VracaIsteOglase()
    {
        // Arrange
        var oglasi = new[] { OglasUsluge(1) };
        _repo.Setup(r => r.DajSveAsync()).ReturnsAsync(oglasi);

        // Act
        var rezultat = await _servis.DajSveOglase();

        // Assert
        Assert.AreSame(oglasi, rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajOglasPoId_PostojeciOglas_VracaOglasIzRepozitorija()
    {
        // Arrange
        var oglas = OglasUsluge(4);
        _repo.Setup(r => r.DajPoIdAsync(4)).ReturnsAsync(oglas);

        // Act
        var rezultat = await _servis.DajOglasPoId(4);

        // Assert
        Assert.AreSame(oglas, rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_IspravanDto_SnimaAktivanOglasSaPodacimaIzDto()
    {
        // Arrange
        var dto = ObjaviUslugeDto();

        // Act
        await _servis.ObjaviOglas(dto, VlasnikId);

        // Assert
        _repo.Verify(r => r.DodajAsync(It.Is<OglasUsluge>(o =>
            o.Naslov == dto.Naslov &&
            o.Opis == dto.Opis &&
            o.MjestoID == dto.MjestoID &&
            o.MinBudzet == dto.MinBudzet &&
            o.MaxBudzet == dto.MaxBudzet &&
            o.Slika == dto.Slika &&
            o.VlasnikOglasaID == VlasnikId &&
            o.StatusOglasa == Status.Aktivan)), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_SnimljenOglas_VracaIdOglasa()
    {
        // Arrange
        _repo.Setup(r => r.DodajAsync(It.IsAny<OglasUsluge>()))
            .Callback<OglasUsluge>(o => o.OglasID = 15)
            .Returns(Task.CompletedTask);

        // Act
        var id = await _servis.ObjaviOglas(ObjaviUslugeDto(), VlasnikId);

        // Assert
        Assert.AreEqual(15, id);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_PostojeciOglas_PrepisujePodatkeIzDto()
    {
        // Arrange
        var dto = UrediUslugeDto(1);
        _repo.Setup(r => r.DajPoIdAsync(1)).ReturnsAsync(OglasUsluge(1));

        // Act
        await _servis.UrediOglas(dto);

        // Assert
        _repo.Verify(r => r.UrediAsync(It.Is<OglasUsluge>(o =>
            o.Naslov == dto.Naslov &&
            o.Opis == dto.Opis &&
            o.MjestoID == dto.MjestoID &&
            o.MinBudzet == dto.MinBudzet &&
            o.MaxBudzet == dto.MaxBudzet &&
            o.Slika == "nova.png")), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_BezNoveSlike_ZadrzavaStaruSliku()
    {
        // Arrange
        var oglas = OglasUsluge(1);
        _repo.Setup(r => r.DajPoIdAsync(1)).ReturnsAsync(oglas);

        // Act
        await _servis.UrediOglas(UrediUslugeDto(1, slika: null));

        // Assert
        Assert.AreEqual("stara.png", oglas.Slika);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_NepostojeciOglas_BacaKeyNotFoundException()
    {
        // Arrange
        _repo.Setup(r => r.DajPoIdAsync(99)).ReturnsAsync((OglasUsluge?)null);

        // Act + Assert
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _servis.UrediOglas(UrediUslugeDto(99)));
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObrisiOglas_PostojeciOglas_PostavljaStatusNeaktivan()
    {
        // Arrange
        _repo.Setup(r => r.DajPoIdAsync(1)).ReturnsAsync(OglasUsluge(1));

        // Act
        await _servis.ObrisiOglas(1);

        // Assert
        _repo.Verify(r => r.UrediAsync(It.Is<OglasUsluge>(o => o.StatusOglasa == Status.Neaktivan)), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObrisiOglas_NepostojeciOglas_BacaKeyNotFoundException()
    {
        // Arrange
        _repo.Setup(r => r.DajPoIdAsync(99)).ReturnsAsync((OglasUsluge?)null);

        // Act + Assert
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _servis.ObrisiOglas(99));
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task PronadjiOglase_BezLokacije_VracaSveOglaseIzPretrage()
    {
        // Arrange
        var oglasi = new List<OglasUsluge> { OglasUsluge(1), OglasUsluge(2) };
        _repo.Setup(r => r.IzvrsiPretraguTekstaAsync("krečenje")).ReturnsAsync(oglasi);

        // Act
        var rezultat = await _servis.PronadjiOglase("krečenje", lokacija: null);

        // Assert
        Assert.AreEqual(2, rezultat.Count());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task PronadjiOglase_SaLokacijom_VracaSamoOglaseIzTogMjesta()
    {
        // Arrange
        var uMjestu = OglasUsluge(1);
        var drugoMjesto = OglasUsluge(2);
        drugoMjesto.MjestoID = 7;
        _repo.Setup(r => r.IzvrsiPretraguTekstaAsync("krečenje")).ReturnsAsync(new List<OglasUsluge> { uMjestu, drugoMjesto });

        // Act
        var rezultat = await _servis.PronadjiOglase("krečenje", lokacija: 1);

        // Assert
        CollectionAssert.AreEqual(new[] { uMjestu }, rezultat.ToList());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajBrojZavrsenihAsync_RepozitorijVracaBroj_VracaIstiBroj()
    {
        // Arrange
        _repo.Setup(r => r.DajBrojZavrsenih()).ReturnsAsync(12);

        // Act
        var broj = await _servis.DajBrojZavrsenihAsync();

        // Assert
        Assert.AreEqual(12, broj);
    }
}
