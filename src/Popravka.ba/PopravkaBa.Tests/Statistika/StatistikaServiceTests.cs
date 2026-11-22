using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Implementation;
using PopravkaBa.Domain.Enums;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;
using static PopravkaBa.Tests.Statistika.StatistikaTestHelper;

namespace PopravkaBa.Tests.Statistika;

[TestClass]
[TestCategory("Service")]
public class StatistikaServiceTests
{
    private Mock<IMjesecnaStatistikaRepository> _repo = null!;
    private StatistikaService _servis = null!;

    [TestInitialize]
    public void Postavi()
    {
        _repo = new Mock<IMjesecnaStatistikaRepository>();
        _repo.Setup(r => r.DohvatiFiltrirano(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<KoloneStatistike>(), It.IsAny<Sortiranje>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MjesecnaStatistikaKompozicija>());
        _repo.Setup(r => r.DohvatiMeta(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PraznaMeta());
        _servis = new StatistikaService(_repo.Object);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DohvatiRangListu_ZadanMjesec_ProsljedjujeFilterRepozitoriju()
    {
        // Arrange
        var filter = new StatistikaFilterDTO
        {
            Godina = 2025, Mjesec = 6, KategorijaId = 2, MjestoId = 3,
            AktivnaKolona = KoloneStatistike.Poslovi, Smjer = Sortiranje.Ascending
        };

        // Act
        await _servis.DohvatiRangListu(filter);

        // Assert
        _repo.Verify(r => r.DohvatiFiltrirano(2025, 6, 2, 3, KoloneStatistike.Poslovi, Sortiranje.Ascending,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DohvatiRangListu_BezMjeseca_KoristiTekuciMjesec()
    {
        // Arrange
        var sada = DateTime.UtcNow;

        // Act
        await _servis.DohvatiRangListu(new StatistikaFilterDTO());

        // Assert
        _repo.Verify(r => r.DohvatiMeta(sada.Year, sada.Month, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DohvatiRangListu_BezMjeseca_UpisujeTekuciMjesecUFilter()
    {
        // Arrange
        var sada = DateTime.UtcNow;

        // Act
        var rezultat = await _servis.DohvatiRangListu(new StatistikaFilterDTO());

        // Assert
        Assert.AreEqual(sada.Year, rezultat.Filter.Godina);
        Assert.AreEqual(sada.Month, rezultat.Filter.Mjesec);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DohvatiRangListu_RepozitorijVracaMeta_VracaMetaPodatke()
    {
        // Arrange
        var meta = PraznaMeta();
        _repo.Setup(r => r.DohvatiMeta(Godina, Mjesec, It.IsAny<CancellationToken>())).ReturnsAsync(meta);

        // Act
        var rezultat = await _servis.DohvatiRangListu(new StatistikaFilterDTO { Godina = Godina, Mjesec = Mjesec });

        // Assert
        Assert.AreSame(meta, rezultat.MetaPodaci);
    }

    [TestMethod]
    [Owner("Aid Mustafić")]
    public async Task DohvatiRangListu_JedanRed_MapiraSvaPoljaReda()
    {
        // Arrange
        var red = Red("m1", rang: 2, ocjena: 4.7m, poslovi: 5);
        _repo.Setup(r => r.DohvatiFiltrirano(Godina, Mjesec, null, null, KoloneStatistike.Ocjena, Sortiranje.Descending,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MjesecnaStatistikaKompozicija> { red });

        // Act
        var rezultat = await _servis.DohvatiRangListu(new StatistikaFilterDTO { Godina = Godina, Mjesec = Mjesec });

        // Assert
        var dto = rezultat.Redovi.Single();
        Assert.AreEqual(2, dto.Rang);
        Assert.AreEqual("m1", dto.IzvrsilacID);
        Assert.AreEqual(red.DisplayName, dto.DisplayName);
        Assert.AreEqual(red.Slika, dto.Slika);
        Assert.AreEqual("Vodoinstalacije", dto.KategorijaNaziv);
        Assert.AreEqual("Sarajevo", dto.MjestoNaziv);
        Assert.AreEqual(4.7m, dto.ProsjecnaOcjena);
        Assert.AreEqual(5, dto.BrojPoslova);
        Assert.AreEqual(KorisnickeUloge.Majstor, dto.TipKorisnika);
        Assert.AreEqual("m1", dto.Username);
    }
}
