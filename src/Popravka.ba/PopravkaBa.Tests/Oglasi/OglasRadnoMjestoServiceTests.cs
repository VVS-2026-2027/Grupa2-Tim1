using PopravkaBa.Application.Services;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using static PopravkaBa.Tests.Oglasi.OglasiTestHelper;

namespace PopravkaBa.Tests.Oglasi;

[TestClass]
[TestCategory("Service")]
public class OglasRadnoMjestoServiceTests
{
    private Mock<IOglasRadnoMjestoRepository> _repo = null!;
    private OglasRadnoMjestoService _servis = null!;

    [TestInitialize]
    public void Postavi()
    {
        _repo = new Mock<IOglasRadnoMjestoRepository>();
        _servis = new OglasRadnoMjestoService(_repo.Object);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajSveOglase_RepozitorijVracaOglase_VracaIsteOglase()
    {
        // Arrange
        var oglasi = new[] { OglasRadnoMjesto(1) };
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
        var oglas = OglasRadnoMjesto(3);
        _repo.Setup(r => r.DajPoIdAsync(3)).ReturnsAsync(oglas);

        // Act
        var rezultat = await _servis.DajOglasPoId(3);

        // Assert
        Assert.AreSame(oglas, rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_IspravanDto_SnimaOglasSaPodacimaIzDto()
    {
        // Arrange
        var dto = ObjaviRadnoMjestoDto();

        // Act
        await _servis.ObjaviOglas(dto, VlasnikId);

        // Assert
        _repo.Verify(r => r.DodajAsync(It.Is<OglasRadnoMjesto>(o =>
            o.Naslov == dto.Naslov &&
            o.Opis == dto.Opis &&
            o.MjestoID == dto.MjestoID &&
            o.BrojIzvrsilaca == dto.BrojIzvrsilaca &&
            o.VrstaZaposlenja == dto.VrstaZaposlenja &&
            o.MinPrihod == 1000 &&
            o.MaxPrihod == 2000 &&
            o.TipIsplate == dto.TipIsplate &&
            o.VlasnikOglasaID == VlasnikId)), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_PrihodNijeUnesen_PostavljaPrihodNaNulu()
    {
        // Arrange
        var dto = ObjaviRadnoMjestoDto(minPrihod: null, maxPrihod: null);

        // Act
        await _servis.ObjaviOglas(dto, VlasnikId);

        // Assert
        _repo.Verify(r => r.DodajAsync(It.Is<OglasRadnoMjesto>(o => o.MinPrihod == 0 && o.MaxPrihod == 0)), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_SnimljenOglas_VracaIdOglasa()
    {
        // Arrange
        _repo.Setup(r => r.DodajAsync(It.IsAny<OglasRadnoMjesto>()))
            .Callback<OglasRadnoMjesto>(o => o.OglasID = 31)
            .Returns(Task.CompletedTask);

        // Act
        var id = await _servis.ObjaviOglas(ObjaviRadnoMjestoDto(), VlasnikId);

        // Assert
        Assert.AreEqual(31, id);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_PostojeciOglas_PrepisujePodatkeIzDto()
    {
        // Arrange
        var dto = UrediRadnoMjestoDto(1);
        _repo.Setup(r => r.DajPoIdAsync(1)).ReturnsAsync(OglasRadnoMjesto(1));

        // Act
        await _servis.UrediOglas(dto);

        // Assert
        _repo.Verify(r => r.UrediAsync(It.Is<OglasRadnoMjesto>(o =>
            o.Naslov == dto.Naslov &&
            o.Opis == dto.Opis &&
            o.MjestoID == dto.MjestoID &&
            o.BrojIzvrsilaca == dto.BrojIzvrsilaca &&
            o.MinPrihod == 1500 &&
            o.MaxPrihod == 2500 &&
            o.Slika == "nova.png")), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_PrihodObrisan_PostavljaPrihodNaNulu()
    {
        // Arrange
        var oglas = OglasRadnoMjesto(1);
        _repo.Setup(r => r.DajPoIdAsync(1)).ReturnsAsync(oglas);

        // Act
        await _servis.UrediOglas(UrediRadnoMjestoDto(1, minPrihod: null, maxPrihod: null));

        // Assert
        Assert.AreEqual(0, oglas.MinPrihod + oglas.MaxPrihod);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_BezNoveSlike_ZadrzavaStaruSliku()
    {
        // Arrange
        var oglas = OglasRadnoMjesto(1);
        _repo.Setup(r => r.DajPoIdAsync(1)).ReturnsAsync(oglas);

        // Act
        await _servis.UrediOglas(UrediRadnoMjestoDto(1, slika: null));

        // Assert
        Assert.AreEqual("stara.png", oglas.Slika);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_NepostojeciOglas_BacaKeyNotFoundException()
    {
        // Arrange
        _repo.Setup(r => r.DajPoIdAsync(99)).ReturnsAsync((OglasRadnoMjesto?)null);

        // Act + Assert
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _servis.UrediOglas(UrediRadnoMjestoDto(99)));
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObrisiOglas_PostojeciOglas_PostavljaStatusNeaktivan()
    {
        // Arrange
        _repo.Setup(r => r.DajPoIdAsync(1)).ReturnsAsync(OglasRadnoMjesto(1));

        // Act
        await _servis.ObrisiOglas(1);

        // Assert
        _repo.Verify(r => r.UrediAsync(It.Is<OglasRadnoMjesto>(o => o.StatusOglasa == Status.Neaktivan)), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObrisiOglas_NepostojeciOglas_BacaKeyNotFoundException()
    {
        // Arrange
        _repo.Setup(r => r.DajPoIdAsync(99)).ReturnsAsync((OglasRadnoMjesto?)null);

        // Act + Assert
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _servis.ObrisiOglas(99));
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task PronadjiOglase_BezLokacije_VracaSveOglaseIzPretrage()
    {
        // Arrange
        var oglasi = new List<OglasRadnoMjesto> { OglasRadnoMjesto(1), OglasRadnoMjesto(2) };
        _repo.Setup(r => r.IzvrsiPretraguTekstaAsync("električar")).ReturnsAsync(oglasi);

        // Act
        var rezultat = await _servis.PronadjiOglase("električar", lokacija: null);

        // Assert
        Assert.AreEqual(2, rezultat.Count());
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task PronadjiOglase_SaLokacijom_VracaSamoOglaseIzTogMjesta()
    {
        // Arrange
        var uMjestu = OglasRadnoMjesto(1);
        var drugoMjesto = OglasRadnoMjesto(2);
        drugoMjesto.MjestoID = 7;
        _repo.Setup(r => r.IzvrsiPretraguTekstaAsync("električar"))
            .ReturnsAsync(new List<OglasRadnoMjesto> { uMjestu, drugoMjesto });

        // Act
        var rezultat = await _servis.PronadjiOglase("električar", lokacija: 1);

        // Assert
        CollectionAssert.AreEqual(new[] { uMjestu }, rezultat.ToList());
    }
}
