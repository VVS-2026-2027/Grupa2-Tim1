using PopravkaBa.Application.Services;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using static PopravkaBa.Tests.Oglasi.OglasiTestHelper;

namespace PopravkaBa.Tests.Oglasi;

[TestClass]
[TestCategory("Service")]
public class OglasMajstoraServiceTests
{
    private Mock<IOglasMajstoraRepository> _repo = null!;
    private OglasMajstoraService _servis = null!;

    [TestInitialize]
    public void Postavi()
    {
        _repo = new Mock<IOglasMajstoraRepository>();
        _servis = new OglasMajstoraService(_repo.Object);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DajSveOglase_RepozitorijVracaOglase_VracaIsteOglase()
    {
        // Arrange
        var oglasi = new[] { OglasMajstora(1), OglasMajstora(2) };
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
        var oglas = OglasMajstora(5);
        _repo.Setup(r => r.DajPoIdAsync(5)).ReturnsAsync(oglas);

        // Act
        var rezultat = await _servis.DajOglasPoId(5);

        // Assert
        Assert.AreSame(oglas, rezultat);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_ManjeOdTriAktivna_SnimaOglasSaPodacimaIzDto()
    {
        // Arrange
        var dto = ObjaviMajstoraDto();
        _repo.Setup(r => r.DajBrojAktivnihZaKorisnikaAsync(VlasnikId)).ReturnsAsync(2);

        // Act
        await _servis.ObjaviOglas(dto, VlasnikId);

        // Assert
        _repo.Verify(r => r.DodajAsync(It.Is<OglasMajstora>(o =>
            o.Naslov == dto.Naslov &&
            o.Opis == dto.Opis &&
            o.MjestoID == dto.MjestoID &&
            o.MinCijena == dto.MinCijena &&
            o.TipIsplate == dto.TipIsplate &&
            o.Slika == dto.Slika &&
            o.VlasnikOglasaID == VlasnikId)), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_SnimljenOglas_VracaIdOglasa()
    {
        // Arrange
        _repo.Setup(r => r.DodajAsync(It.IsAny<OglasMajstora>()))
            .Callback<OglasMajstora>(o => o.OglasID = 77)
            .Returns(Task.CompletedTask);

        // Act
        var id = await _servis.ObjaviOglas(ObjaviMajstoraDto(), VlasnikId);

        // Assert
        Assert.AreEqual(77, id);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    [DataRow(3)]
    [DataRow(4)]
    public async Task ObjaviOglas_TriIliViseAktivnih_BacaInvalidOperationException(int brojAktivnih)
    {
        // Arrange
        _repo.Setup(r => r.DajBrojAktivnihZaKorisnikaAsync(VlasnikId)).ReturnsAsync(brojAktivnih);

        // Act + Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _servis.ObjaviOglas(ObjaviMajstoraDto(), VlasnikId));
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObjaviOglas_TriAktivna_NeSnimaOglas()
    {
        // Arrange
        _repo.Setup(r => r.DajBrojAktivnihZaKorisnikaAsync(VlasnikId)).ReturnsAsync(3);

        // Act
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _servis.ObjaviOglas(ObjaviMajstoraDto(), VlasnikId));

        // Assert
        _repo.Verify(r => r.DodajAsync(It.IsAny<OglasMajstora>()), Times.Never);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_PostojeciOglas_PrepisujePodatkeIzDto()
    {
        // Arrange
        var oglas = OglasMajstora(1);
        var dto = UrediMajstoraDto(1);
        _repo.Setup(r => r.DajPoIdAsync(1)).ReturnsAsync(oglas);

        // Act
        await _servis.UrediOglas(dto);

        // Assert
        _repo.Verify(r => r.UrediAsync(It.Is<OglasMajstora>(o =>
            o.Naslov == dto.Naslov &&
            o.Opis == dto.Opis &&
            o.MjestoID == dto.MjestoID &&
            o.MinCijena == dto.MinCijena &&
            o.TipIsplate == dto.TipIsplate &&
            o.Slika == "nova.png")), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_BezNoveSlike_ZadrzavaStaruSliku()
    {
        // Arrange
        var oglas = OglasMajstora(1);
        _repo.Setup(r => r.DajPoIdAsync(1)).ReturnsAsync(oglas);

        // Act
        await _servis.UrediOglas(UrediMajstoraDto(1, slika: null));

        // Assert
        Assert.AreEqual("stara.png", oglas.Slika);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task UrediOglas_NepostojeciOglas_BacaKeyNotFoundException()
    {
        // Arrange
        _repo.Setup(r => r.DajPoIdAsync(99)).ReturnsAsync((OglasMajstora?)null);

        // Act + Assert
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _servis.UrediOglas(UrediMajstoraDto(99)));
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObrisiOglas_PostojeciOglas_PostavljaStatusNeaktivan()
    {
        // Arrange
        var oglas = OglasMajstora(1);
        _repo.Setup(r => r.DajPoIdAsync(1)).ReturnsAsync(oglas);

        // Act
        await _servis.ObrisiOglas(1);

        // Assert
        _repo.Verify(r => r.UrediAsync(It.Is<OglasMajstora>(o => o.StatusOglasa == Status.Neaktivan)), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task ObrisiOglas_NepostojeciOglas_BacaKeyNotFoundException()
    {
        // Arrange
        _repo.Setup(r => r.DajPoIdAsync(99)).ReturnsAsync((OglasMajstora?)null);

        // Act + Assert
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _servis.ObrisiOglas(99));
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task PronadjiOglase_TekstPretrage_ProsljedjujeTekstRepozitoriju()
    {
        // Arrange
        var oglasi = new[] { OglasMajstora(1) };
        _repo.Setup(r => r.IzvrsiPretraguTekstaAsync("bojler")).ReturnsAsync(oglasi);

        // Act
        var rezultat = await _servis.PronadjiOglase("bojler", lokacija: 5);

        // Assert
        Assert.AreSame(oglasi, rezultat);
    }
}
