using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Implementation;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using static PopravkaBa.Tests.PrijaveOglasRadnoMjesto.PrijavaOglasServiceTestHelper;

namespace PopravkaBa.Tests.PrijaveOglasRadnoMjesto;

[TestClass]
[TestCategory("Service")]
public class PrijavaOglasServiceTests
{
    private const int PrijavaId = 5;

    private Mock<IPrijavaRadnoMjestoRepository> _prijave = null!;
    private Mock<IOglasRadnoMjestoRepository> _oglasi = null!;
    private PrijavaOglasService _servis = null!;

    [TestInitialize]
    public void PripremiTest()
    {
        _prijave = new Mock<IPrijavaRadnoMjestoRepository>();
        _oglasi = new Mock<IOglasRadnoMjestoRepository>();
        _servis = new PrijavaOglasService(_prijave.Object, _oglasi.Object);
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task DajPrijavuPoId_PostojecaPrijava_VracaPrijavu()
    {
        // Arrange
        var prijava = NovaPrijava(PrijavaId);
        _prijave.Setup(r => r.DajPoIdAsync(PrijavaId)).ReturnsAsync(prijava);

        // Act
        var rezultat = await _servis.DajPrijavuPoId(PrijavaId);

        // Assert
        Assert.AreSame(prijava, rezultat);
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task DajSvePrijave_ZadaniOglas_VracaNjegovePrijave()
    {
        // Arrange
        var prijave = new[] { NovaPrijava(PrijavaId) };
        _prijave.Setup(r => r.DajSvePrijaveOglasaAsync(OglasId))
            .ReturnsAsync(prijave);

        // Act
        var rezultat = await _servis.DajSvePrijave(OglasId);

        // Assert
        CollectionAssert.AreEqual(prijave, rezultat.ToArray());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task KreirajPrijavu_NepostojeciOglas_BacaKeyNotFoundException()
    {
        // Arrange
        var dto = NoviZahtjev();
        _oglasi.Setup(r => r.DajPoIdAsync(OglasId))
            .ReturnsAsync((OglasRadnoMjesto?)null);

        // Act
        Func<Task> kreiraj = () => _servis.KreirajPrijavu(dto);

        // Assert
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(kreiraj);
        _prijave.Verify(r => r.DodajAsync(It.IsAny<PrijavaRadnoMjesto>()),
            Times.Never());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task KreirajPrijavu_NeaktivanOglas_OdbijaPrijavu()
    {
        // Arrange
        var dto = NoviZahtjev();
        PostaviOglas(_oglasi, 1, Status.Neaktivan);

        // Act
        Func<Task> kreiraj = () => _servis.KreirajPrijavu(dto);

        // Assert
        var greska = await Assert.ThrowsExactlyAsync<InvalidOperationException>(kreiraj);
        Assert.AreEqual("Oglas više nije aktivan.", greska.Message);
        _prijave.Verify(r => r.DodajAsync(It.IsAny<PrijavaRadnoMjesto>()),
            Times.Never());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task KreirajPrijavu_MajstorVecPrijavljen_OdbijaDuplikat()
    {
        // Arrange
        var dto = NoviZahtjev();
        var oglas = PostaviOglas(_oglasi, 1);
        oglas.Prijave = new List<PrijavaRadnoMjesto> { NovaPrijava(PrijavaId) };

        // Act
        Func<Task> kreiraj = () => _servis.KreirajPrijavu(dto);

        // Assert
        var greska = await Assert.ThrowsExactlyAsync<InvalidOperationException>(kreiraj);
        Assert.AreEqual("Već ste se prijavili na ovaj oglas.", greska.Message);
        _prijave.Verify(r => r.DodajAsync(It.IsAny<PrijavaRadnoMjesto>()),
            Times.Never());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    [DataRow(false)]
    [DataRow(true)]
    public async Task KreirajPrijavu_AktivanOglasBezDuplikata_SpremaPrijavuNaCekanju(
        bool imaPrijavuDrugogMajstora)
    {
        // Arrange
        var dto = NoviZahtjev();
        var oglas = PostaviOglas(_oglasi, 2);
        oglas.Prijave = imaPrijavuDrugogMajstora
            ? new List<PrijavaRadnoMjesto> { NovaPrijava(6, "m2") }
            : null;

        // Act
        await _servis.KreirajPrijavu(dto);

        // Assert
        _prijave.Verify(r => r.DodajAsync(It.Is<PrijavaRadnoMjesto>(p =>
            p.OglasID == OglasId &&
            p.MajstorID == MajstorId &&
            p.StatusPrijave == Status.NaCekanju &&
            p.VrijemePrijave.Kind == DateTimeKind.Utc)),
            Times.Once());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task PrihvatiPonudu_NepostojecaPrijava_BacaKeyNotFoundException()
    {
        // Arrange
        _prijave.Setup(r => r.DajPoIdAsync(PrijavaId))
            .ReturnsAsync((PrijavaRadnoMjesto?)null);

        // Act
        Func<Task> prihvati = () => _servis.PrihvatiPonudu(PrijavaId);

        // Assert
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(prihvati);
        _oglasi.Verify(r => r.DajPoIdAsync(It.IsAny<int>()), Times.Never());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task PrihvatiPonudu_NepostojeciOglas_BacaKeyNotFoundException()
    {
        // Arrange
        var prijava = NovaPrijava(PrijavaId);
        _prijave.Setup(r => r.DajPoIdAsync(PrijavaId)).ReturnsAsync(prijava);
        _oglasi.Setup(r => r.DajPoIdAsync(OglasId))
            .ReturnsAsync((OglasRadnoMjesto?)null);

        // Act
        Func<Task> prihvati = () => _servis.PrihvatiPonudu(PrijavaId);

        // Assert
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(prihvati);
        _prijave.Verify(r => r.UrediAsync(It.IsAny<PrijavaRadnoMjesto>()),
            Times.Never());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task PrihvatiPonudu_ImaSlobodnihMjesta_OstavljaOglasAktivnim()
    {
        // Arrange
        var oglas = PostaviOglas(_oglasi, 2);
        var odabrana = NovaPrijava(PrijavaId);
        var druga = NovaPrijava(6, "m2");
        _prijave.Setup(r => r.DajPoIdAsync(PrijavaId)).ReturnsAsync(odabrana);
        _prijave.Setup(r => r.DajSvePrijaveOglasaAsync(OglasId))
            .ReturnsAsync(new[] { odabrana, druga });

        // Act
        bool oglasPopunjen = await _servis.PrihvatiPonudu(PrijavaId);

        // Assert
        Assert.IsFalse(oglasPopunjen);
        Assert.AreEqual(Status.Aktivan, oglas.StatusOglasa);
        Assert.AreEqual(Status.Prihvaceno, odabrana.StatusPrijave);
        Assert.AreEqual(Status.NaCekanju, druga.StatusPrijave);
        _prijave.Verify(r => r.UrediAsync(odabrana), Times.Once());
        _prijave.Verify(r => r.UrediAsync(druga), Times.Never());
        _oglasi.Verify(r => r.UrediAsync(oglas), Times.Never());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task PrihvatiPonudu_PopunjenOglas_OdbijaPrijaveNaCekanju()
    {
        // Arrange: oglas traži 2 izvršioca, jedna prijava je ranije prihvaćena, jedna čeka
        PostaviOglas(_oglasi, 2);
        var odabrana = NovaPrijava(PrijavaId);
        var ranijePrihvacena = NovaPrijava(6, "m2", Status.Prihvaceno);
        var naCekanju = NovaPrijava(7, "m3");
        _prijave.Setup(r => r.DajPoIdAsync(PrijavaId)).ReturnsAsync(odabrana);
        _prijave.Setup(r => r.DajSvePrijaveOglasaAsync(OglasId))
            .ReturnsAsync(new[] { odabrana, ranijePrihvacena, naCekanju });

        // Act
        await _servis.PrihvatiPonudu(PrijavaId);

        // Assert: odbija se samo prijava na čekanju, ranije prihvaćena ostaje
        Assert.AreEqual(Status.Odbijeno, naCekanju.StatusPrijave);
        Assert.AreEqual(Status.Prihvaceno, ranijePrihvacena.StatusPrijave);
        _prijave.Verify(r => r.UrediAsync(naCekanju), Times.Once());
        _prijave.Verify(r => r.UrediAsync(ranijePrihvacena), Times.Never());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task PrihvatiPonudu_PopunjenOglas_ZatvaraOglas()
    {
        // Arrange: oglas traži 2 izvršioca, jedna prijava je ranije prihvaćena
        var oglas = PostaviOglas(_oglasi, 2);
        var odabrana = NovaPrijava(PrijavaId);
        var ranijePrihvacena = NovaPrijava(6, "m2", Status.Prihvaceno);
        _prijave.Setup(r => r.DajPoIdAsync(PrijavaId)).ReturnsAsync(odabrana);
        _prijave.Setup(r => r.DajSvePrijaveOglasaAsync(OglasId))
            .ReturnsAsync(new[] { odabrana, ranijePrihvacena });

        // Act
        bool oglasPopunjen = await _servis.PrihvatiPonudu(PrijavaId);

        // Assert
        Assert.IsTrue(oglasPopunjen);
        Assert.AreEqual(Status.Neaktivan, oglas.StatusOglasa);
        _oglasi.Verify(r => r.UrediAsync(oglas), Times.Once());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task OdbijPrijavu_PostojecaPrijava_SpremaOdbijeniStatus()
    {
        // Arrange
        var prijava = NovaPrijava(PrijavaId);
        _prijave.Setup(r => r.DajPoIdAsync(PrijavaId)).ReturnsAsync(prijava);

        // Act
        await _servis.OdbijPrijavu(PrijavaId);

        // Assert
        Assert.AreEqual(Status.Odbijeno, prijava.StatusPrijave);
        _prijave.Verify(r => r.UrediAsync(prijava), Times.Once());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task OdbijPrijavu_NepostojecaPrijava_BacaKeyNotFoundException()
    {
        // Arrange
        _prijave.Setup(r => r.DajPoIdAsync(PrijavaId))
            .ReturnsAsync((PrijavaRadnoMjesto?)null);

        // Act
        Func<Task> odbij = () => _servis.OdbijPrijavu(PrijavaId);

        // Assert
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(odbij);
        _prijave.Verify(r => r.UrediAsync(It.IsAny<PrijavaRadnoMjesto>()),
            Times.Never());
    }

    [TestMethod]
    [Owner("Amina Bukvić")]
    public async Task ObrisiPrijavu_ZadaniId_TraziBrisanjePrijave()
    {
        // Arrange
        _prijave.Setup(r => r.ObrisiAsync(PrijavaId)).Returns(Task.CompletedTask);

        // Act
        await _servis.ObrisiPrijavu(PrijavaId);

        // Assert
        _prijave.Verify(r => r.ObrisiAsync(PrijavaId), Times.Once());
    }
}