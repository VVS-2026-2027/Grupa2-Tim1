using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Implementation;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using static PopravkaBa.Tests.PonudeUsluga.PonudaUslugeServiceTestHelper;

namespace PopravkaBa.Tests.PonudeUsluga;

[TestClass]
[TestCategory("Service")]
public class PonudaUslugeServiceTests
{
    private const int PonudaId = 5;

    private Mock<IPonudaUslugeRepository> _ponude = null!;
    private Mock<IOglasUslugeRepository> _oglasi = null!;
    private Mock<IIzvrsilacUslugeRepository> _izvrsioci = null!;
    private PonudaUslugeService _servis = null!;

    [TestInitialize]
    public void PripremiTest()
    {
        _ponude = new Mock<IPonudaUslugeRepository>();
        _oglasi = new Mock<IOglasUslugeRepository>();
        _izvrsioci = new Mock<IIzvrsilacUslugeRepository>();
        _servis = new PonudaUslugeService(
            _ponude.Object, _oglasi.Object, _izvrsioci.Object);
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task DajPonuduPoId_PostojecaPonuda_VracaPonudu()
    {
        // Arrange
        var ponuda = NovaPonuda(PonudaId, Status.NaCekanju);
        _ponude.Setup(r => r.DajPoIdAsync(PonudaId)).ReturnsAsync(ponuda);

        // Act
        var rezultat = await _servis.DajPonuduPoId(PonudaId);

        // Assert
        Assert.AreSame(ponuda, rezultat);
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task DajSvePonude_PostojePonude_VracaListuIzRepozitorija()
    {
        // Arrange
        var ponude = new[] { NovaPonuda(PonudaId, Status.NaCekanju) };
        _ponude.Setup(r => r.DajSvePonudeOglasaAsync(0)).ReturnsAsync(ponude);

        // Act
        var rezultat = await _servis.DajSvePonude();

        // Assert
        CollectionAssert.AreEqual(ponude, rezultat.ToArray());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task DajSvePonudeOglasa_ZadaniOglas_VracaNjegovePonude()
    {
        // Arrange
        var ponude = new[] { NovaPonuda(PonudaId, Status.NaCekanju) };
        _ponude.Setup(r => r.DajSvePonudeOglasaAsync(OglasId)).ReturnsAsync(ponude);

        // Act
        var rezultat = await _servis.DajSvePonudeOglasa(OglasId);

        // Assert
        CollectionAssert.AreEqual(ponude, rezultat.ToArray());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task DajSvePonudeIzvrsioca_ZadaniIzvrsilac_VracaNjegovePonude()
    {
        // Arrange
        var ponude = new[] { NovaPonuda(PonudaId, Status.NaCekanju) };
        _ponude.Setup(r => r.DajSvePonudeIzvrsiocaAsync(IzvrsilacId))
            .ReturnsAsync(ponude);

        // Act
        var rezultat = await _servis.DajSvePonudeIzvrsioca(IzvrsilacId);

        // Assert
        CollectionAssert.AreEqual(ponude, rezultat.ToArray());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task ObrisiPonudu_ZadaniId_TraziBrisanjePonude()
    {
        // Arrange
        _ponude.Setup(r => r.ObrisiAsync(PonudaId)).Returns(Task.CompletedTask);

        // Act
        await _servis.ObrisiPonudu(PonudaId);

        // Assert
        _ponude.Verify(r => r.ObrisiAsync(PonudaId), Times.Once());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task PosaljiPonudu_NepostojeciOglas_BacaKeyNotFoundException()
    {
        // Arrange
        var dto = NovaObjava(null);
        _oglasi.Setup(r => r.DajPoIdAsync(OglasId)).ReturnsAsync((OglasUsluge?)null);

        // Act
        Func<Task> posalji = () => _servis.PosaljiPonudu(dto, IzvrsilacId);

        // Assert
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(posalji);
        _ponude.Verify(r => r.DodajAsync(It.IsAny<PonudaUsluge>()), Times.Never());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task PosaljiPonudu_NeaktivanOglas_BacaInvalidOperationException()
    {
        // Arrange
        var dto = NovaObjava(null);
        PostaviOglas(_oglasi, Status.Neaktivan);

        // Act
        Func<Task> posalji = () => _servis.PosaljiPonudu(dto, IzvrsilacId);

        // Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(posalji);
        _ponude.Verify(r => r.DodajAsync(It.IsAny<PonudaUsluge>()), Times.Never());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    [DataRow(true)]
    [DataRow(false)]
    public async Task PosaljiPonudu_AktivanOglas_SpremaPonuduSaUtcDatumima(
        bool imaDatumZavrsetka)
    {
        // Arrange
        DateTime? datumZavrsetka = imaDatumZavrsetka
            ? new DateTime(2026, 11, 23)
            : null;
        var dto = NovaObjava(datumZavrsetka);
        DateTime? ocekivaniZavrsetak = datumZavrsetka.HasValue
            ? DateTime.SpecifyKind(datumZavrsetka.Value, DateTimeKind.Utc)
            : null;
        PostaviOglas(_oglasi, Status.Aktivan);

        // Act
        await _servis.PosaljiPonudu(dto, IzvrsilacId);

    // Assert
_ponude.Verify(r => r.DodajAsync(It.Is<PonudaUsluge>(p =>
    p.OglasUslugeID == OglasId &&
    p.IzvrsilacID == IzvrsilacId &&
    p.StatusPonude == Status.NaCekanju &&
    p.Cijena == dto.Cijena &&
    p.TipIsplate == dto.TipIsplate &&
    p.Poruka == dto.Poruka &&
    p.DatumSlanja.Kind == DateTimeKind.Utc &&
    p.DatumPocetkaUsluge == dto.DatumPocetkaUsluge &&
    p.DatumPocetkaUsluge.Kind == DateTimeKind.Utc &&
    p.DatumOcekivanogZavrsetka == ocekivaniZavrsetak &&
    (!p.DatumOcekivanogZavrsetka.HasValue ||
        p.DatumOcekivanogZavrsetka.Value.Kind == DateTimeKind.Utc))),
    Times.Once());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task PrihvatiPonudu_NepostojecaPonuda_BacaKeyNotFoundException()
    {
        // Arrange
        _ponude.Setup(r => r.DajPoIdAsync(PonudaId)).ReturnsAsync((PonudaUsluge?)null);

        // Act
        Func<Task> prihvati = () => _servis.PrihvatiPonudu(PonudaId);

        // Assert
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(prihvati);
        _ponude.Verify(r => r.UrediAsync(It.IsAny<PonudaUsluge>()), Times.Never());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task PrihvatiPonudu_VisePonuda_PrihvataOdabranuPonudu()
    {
        // Arrange
        var odabrana = NovaPonuda(PonudaId, Status.NaCekanju);
        var druga = NovaPonuda(6, Status.NaCekanju);
        PostaviOglas(_oglasi, Status.Aktivan);
        _ponude.Setup(r => r.DajPoIdAsync(PonudaId)).ReturnsAsync(odabrana);
        _ponude.Setup(r => r.DajSvePonudeOglasaAsync(OglasId)).ReturnsAsync(new[] { odabrana, druga });

        // Act
        await _servis.PrihvatiPonudu(PonudaId);

        // Assert
        Assert.AreEqual(Status.Prihvaceno, odabrana.StatusPonude);
        _ponude.Verify(r => r.UrediAsync(odabrana), Times.Once());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task PrihvatiPonudu_VisePonuda_OdbijaOstaleNaCekanju()
    {
        // Arrange: jedna ponuda čeka, jedna je već isporučena
        var odabrana = NovaPonuda(PonudaId, Status.NaCekanju);
        var druga = NovaPonuda(6, Status.NaCekanju);
        var zavrsena = NovaPonuda(7, Status.Isporuceno);
        PostaviOglas(_oglasi, Status.Aktivan);
        _ponude.Setup(r => r.DajPoIdAsync(PonudaId)).ReturnsAsync(odabrana);
        _ponude.Setup(r => r.DajSvePonudeOglasaAsync(OglasId))
            .ReturnsAsync(new[] { odabrana, druga, zavrsena });

        // Act
        await _servis.PrihvatiPonudu(PonudaId);

        // Assert: odbija se samo ponuda na čekanju, isporučena ostaje
        Assert.AreEqual(Status.Odbijeno, druga.StatusPonude);
        Assert.AreEqual(Status.Isporuceno, zavrsena.StatusPonude);
        _ponude.Verify(r => r.UrediAsync(druga), Times.Once());
        _ponude.Verify(r => r.UrediAsync(zavrsena), Times.Never());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task PrihvatiPonudu_VisePonuda_ZatvaraOglas()
    {
        // Arrange
        var odabrana = NovaPonuda(PonudaId, Status.NaCekanju);
        var oglas = PostaviOglas(_oglasi, Status.Aktivan);
        _ponude.Setup(r => r.DajPoIdAsync(PonudaId)).ReturnsAsync(odabrana);
        _ponude.Setup(r => r.DajSvePonudeOglasaAsync(OglasId)).ReturnsAsync(new[] { odabrana });

        // Act
        await _servis.PrihvatiPonudu(PonudaId);

        // Assert
        Assert.AreEqual(Status.Neaktivan, oglas.StatusOglasa);
        _oglasi.Verify(r => r.UrediAsync(oglas), Times.Once());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task OdbijPonudu_PostojecaPonuda_SpremaOdbijeniStatus()
    {
        // Arrange
        var ponuda = NovaPonuda(PonudaId, Status.NaCekanju);
        _ponude.Setup(r => r.DajPoIdAsync(PonudaId)).ReturnsAsync(ponuda);

        // Act
        await _servis.OdbijPonudu(PonudaId);

        // Assert
        Assert.AreEqual(Status.Odbijeno, ponuda.StatusPonude);
        _ponude.Verify(r => r.UrediAsync(ponuda), Times.Once());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task OdbijPonudu_NepostojecaPonuda_BacaKeyNotFoundException()
    {
        // Arrange
        _ponude.Setup(r => r.DajPoIdAsync(PonudaId)).ReturnsAsync((PonudaUsluge?)null);

        // Act
        Func<Task> odbij = () => _servis.OdbijPonudu(PonudaId);

        // Assert
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(odbij);
        _ponude.Verify(r => r.UrediAsync(It.IsAny<PonudaUsluge>()), Times.Never());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task ZavrsiPosao_NepostojeciOglas_BacaKeyNotFoundException()
    {
        // Arrange
        _oglasi.Setup(r => r.DajPoIdAsync(OglasId)).ReturnsAsync((OglasUsluge?)null);

        // Act
        Func<Task> zavrsi = () => _servis.ZavrsiPosao(OglasId, KlijentId);

        // Assert
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(zavrsi);
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task ZavrsiPosao_KlijentNijeVlasnik_BacaUnauthorizedAccessException()
    {
        // Arrange
        PostaviOglas(_oglasi, Status.Neaktivan);

        // Act
        Func<Task> zavrsi = () => _servis.ZavrsiPosao(OglasId, "k2");

        // Assert
        await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(zavrsi);
        _oglasi.Verify(r => r.UrediAsync(It.IsAny<OglasUsluge>()), Times.Never());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task ZavrsiPosao_VecIsporucen_BacaInvalidOperationException()
    {
        // Arrange
        PostaviOglas(_oglasi, Status.Isporuceno);

        // Act
        Func<Task> zavrsi = () => _servis.ZavrsiPosao(OglasId, KlijentId);

        // Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(zavrsi);
        _ponude.Verify(r => r.DajSvePonudeOglasaAsync(OglasId), Times.Never());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task ZavrsiPosao_BezPrihvacenePonude_BacaInvalidOperationException()
    {
        // Arrange
        PostaviOglas(_oglasi, Status.Neaktivan);
        _ponude.Setup(r => r.DajSvePonudeOglasaAsync(OglasId))
            .ReturnsAsync(new[] { NovaPonuda(PonudaId, Status.NaCekanju) });

        // Act
        Func<Task> zavrsi = () => _servis.ZavrsiPosao(OglasId, KlijentId);

        // Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(zavrsi);
        _ponude.Verify(r => r.UrediAsync(It.IsAny<PonudaUsluge>()), Times.Never());
        _oglasi.Verify(r => r.UrediAsync(It.IsAny<OglasUsluge>()), Times.Never());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task ZavrsiPosao_PrihvacenaPonuda_OznacavaIsporukuIPovecavaBrojPoslova()
    {
        // Arrange
        var oglas = PostaviOglas(_oglasi, Status.Neaktivan);
        var ponuda = NovaPonuda(PonudaId, Status.Prihvaceno);
        _ponude.Setup(r => r.DajSvePonudeOglasaAsync(OglasId))
            .ReturnsAsync(new[] { ponuda });

        // Act
        await _servis.ZavrsiPosao(OglasId, KlijentId);

        // Assert
        Assert.AreEqual(Status.Isporuceno, ponuda.StatusPonude);
        Assert.AreEqual(Status.Isporuceno, oglas.StatusOglasa);
        Assert.IsNotNull(ponuda.DatumIzvrsavanjaUsluge);
        Assert.AreEqual(DateTimeKind.Utc, ponuda.DatumIzvrsavanjaUsluge.Value.Kind);
        _ponude.Verify(r => r.UrediAsync(ponuda), Times.Once());
        _oglasi.Verify(r => r.UrediAsync(oglas), Times.Once());
        _izvrsioci.Verify(r => r.PovecajBrojZavrsenihPoslovaAsync(IzvrsilacId), Times.Once());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task DajProsjekCijenePoKategorijama_ZadaneKategorije_VracaProsjek()
    {
        // Arrange
        int[] kategorije = { 1, 2 };
        _ponude.Setup(r => r.DajProsjekCijenePoKategorijama(kategorije))
            .ReturnsAsync((decimal?)75.5m);

        // Act
        var rezultat = await _servis.DajProsjekCijenePoKategorijama(kategorije);

        // Assert
        Assert.AreEqual((decimal?)75.5m, rezultat);
    }
}
