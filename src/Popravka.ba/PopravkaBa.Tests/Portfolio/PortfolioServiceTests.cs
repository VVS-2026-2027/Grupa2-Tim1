


using Amazon.S3.Model;
using PopravkaBa.Application.Services.Implementation;
using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.Portfolio;

[TestClass]
public class PortfolioServiceTests
{
    private Mock<IIzvrsilacUslugeRepository> _izvrsilacRepository = null!;
    private PortfolioService _portfolioService = null!;

    [TestInitialize]
    public void Postavi()
    {
        _izvrsilacRepository = new Mock<IIzvrsilacUslugeRepository>();
        _portfolioService = new PortfolioService(_izvrsilacRepository.Object);
    }

    [TestMethod]
    public async Task DajPrikazSlike_SrednjaSlika_VracaSlikuIPokazujeNaOstale()
    {
        // Arrange
        _izvrsilacRepository.StubMajstorSaSlikama(0, 1, 2);
        // Act
        var dto = await _portfolioService.DajPrikazSlike("m1", 1);
        // Assert
        Assert.AreEqual(1, dto.SlikaID);
        Assert.AreEqual(0, dto.PrethodnaSlikaID);
        Assert.AreEqual(2, dto.NarednaSlikaID);
        Assert.AreEqual(2, dto.TrenutnaPozicija);
        Assert.AreEqual(3, dto.UkupanPortfolio);
    }

    [TestMethod]
    public async Task DajPrikazSlike_PosljednjaSlika_SljedecuPrikazePrvu()
    {
        // Arrange
        _izvrsilacRepository.StubMajstorSaSlikama(10, 20, 30);
        // Act
        var dto = await _portfolioService.DajPrikazSlike("m1", 30);
        // Assert
        Assert.AreEqual(10, dto.NarednaSlikaID);
    }

    [TestMethod]
    public async Task DajPrikazSlike_PrvaSlika_PrethodnaPokazujeZadnju()
    {
        // Arrange
        _izvrsilacRepository.StubMajstorSaSlikama(10, 20, 30);
        // Act
        var dto = await _portfolioService.DajPrikazSlike("m1", 10);
        // Assert
        Assert.AreEqual(30, dto.PrethodnaSlikaID);
    }

    [TestMethod]
    public async Task DajPrikazSlike_SlikeNisuPoRedu_RedoslijedJePoId()
    {
        // Arrange: repozitorij vraća slike izmiješane
        _izvrsilacRepository.StubMajstorSaSlikama(40, 10, 30, 20);

        // Act
        var dto = await _portfolioService.DajPrikazSlike("m1", 20);

        // Assert
        Assert.AreEqual(10, dto.PrethodnaSlikaID);
        Assert.AreEqual(30, dto.NarednaSlikaID);
        Assert.AreEqual(2, dto.TrenutnaPozicija);
    }

    [TestMethod]
    public async Task DajPrikazSlike_JednaSlika_StrelicePokazujuNaSebe()
    {
        // Arrange
        _izvrsilacRepository.StubMajstorSaSlikama(10);

        // Act
        var dto = await _portfolioService.DajPrikazSlike("m1", 10);

        // Assert
        Assert.AreEqual(10, dto.NarednaSlikaID);
        Assert.AreEqual(10, dto.PrethodnaSlikaID);
    }

    [TestMethod]
    public async Task DajPrikazSlike_DvijeSlike_ObjeStrelicePokazujuNaDrugu()
    {
        // Arrange
        _izvrsilacRepository.StubMajstorSaSlikama(10,20);

        // Act
        var prikaz = await _portfolioService.DajPrikazSlike("m1", 10);

        // Assert
        Assert.AreEqual(20, prikaz.PrethodnaSlikaID);
        Assert.AreEqual(20, prikaz.NarednaSlikaID);
    }

    [TestMethod]
    public async Task DajPrikazSlike_NepostojecaSlika_BacaKeyNotFoundException()
    {
        // Arrange
        _izvrsilacRepository.StubMajstorSaSlikama(10, 20);

        // Act and Assert
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _portfolioService.DajPrikazSlike("m1", 99));
    }

    [TestMethod]
    public async Task DajPrikazSlike_SlikaDrugogMajstora_BacaKeyNotFoundException()
    {
        // Arrange: m1 ima slike 10 i 20, a slika 50 stvarno postoji, ali pripada majstoru m2
        _izvrsilacRepository.StubMajstorSaSlikama("m1", 10, 20);
        _izvrsilacRepository.StubMajstorSaSlikama("m2", 50);

        // Act + Assert
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _portfolioService.DajPrikazSlike("m1", 50));
        _izvrsilacRepository.Verify(r => r.DajProfilPoIdAsync("m2"), Times.Never);
    }

    [TestMethod]
    public async Task DajPrikazSlike_NepostojeciMajstor_BacaKeyNotFoundException()
    {
        // Arrange: za m99 nema Setup-a, pa mock vraća null

        // Act + Assert
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _portfolioService.DajPrikazSlike("m99", 10));
    }

    [TestMethod]
    public async Task DajPrikazSlike_PortfolioJeNull_BacaKeyNotFoundException()
    {
        // Arrange: majstor postoji, ali nema listu slika
        _izvrsilacRepository.Setup(r => r.DajProfilPoIdAsync("m1"))
            .ReturnsAsync(new Majstor { Id = "m1", SlikePortfolija = null });

        // Act + Assert
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _portfolioService.DajPrikazSlike("m1", 10));
    }

    [TestMethod]
    public async Task DajPrikazSlike_PraznaListaSlika_BacaKeyNotFoundException()
    {
        // Arrange: majstor postoji, ali nije dodao nijednu sliku
        _izvrsilacRepository.StubMajstorSaSlikama();

        // Act + Assert
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _portfolioService.DajPrikazSlike("m1", 10));
    }

    // ThrowsAsync, a ne ThrowsExactlyAsync: za null stiže ArgumentNullException, podtip ArgumentException
    [TestMethod]
    public async Task DajPrikazSlike_PrazanIdMajstora_BacaArgumentExceptionBezPozivaRepozitorija()
    {
        // Arrange: ništa, id majstora je prazan

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _portfolioService.DajPrikazSlike("", 10));
        _izvrsilacRepository.Verify(r => r.DajProfilPoIdAsync(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task DajPrikazSlike_IspravanZahtjev_RepozitorijSePozivaJednom()
    {
        // Arrange
        _izvrsilacRepository.StubMajstorSaSlikama(10, 20, 30);

        // Act
        await _portfolioService.DajPrikazSlike("m1", 20);

        // Assert
        _izvrsilacRepository.Verify(r => r.DajProfilPoIdAsync("m1"), Times.Once);
    }

    // Data-driven: slučajevi navigacije iz CSV-a. Id-evi slika su odvojeni sa ";", jer "," odvaja kolone.
    public static IEnumerable<object[]> SlucajeviNavigacije =>
        DataDrivenInputParser.CitajCsv<string, int, int, int, int>("TestData/csv/PortfolioService.csv");

    [TestMethod]
    [DynamicData(nameof(SlucajeviNavigacije))]
    public async Task DajPrikazSlike_PodaciIzCsv_VracaOcekivaneSusjede(
        string slike, int trazena, int prethodna, int naredna, int pozicija)
    {
        // Arrange
        _izvrsilacRepository.StubMajstorSaSlikama(slike.Split(';').Select(int.Parse).ToArray());

        // Act
        var prikaz = await _portfolioService.DajPrikazSlike("m1", trazena);

        // Assert
        Assert.AreEqual(prethodna, prikaz.PrethodnaSlikaID);
        Assert.AreEqual(naredna, prikaz.NarednaSlikaID);
        Assert.AreEqual(pozicija, prikaz.TrenutnaPozicija);
    }
}
