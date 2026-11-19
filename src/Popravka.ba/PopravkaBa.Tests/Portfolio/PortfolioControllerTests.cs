using Microsoft.AspNetCore.Mvc;
using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Interface;
using PopravkaBa.Web.Controllers;

namespace PopravkaBa.Tests.Portfolio;

[TestClass]
public class PortfolioControllerTests
{
    private Mock<IPortfolioService> _portfolioService = null!;
    private PortfolioController _kontroler = null!;

    [TestInitialize]
    public void Postavi()
    {
        _portfolioService = new Mock<IPortfolioService>();
        _kontroler = new PortfolioController(_portfolioService.Object);
    }

    [TestMethod]
    public async Task Slika_PostojecaSlika_VracaJsonSaPrikazomSlike()
    {
        // Arrange
        var prikaz = new PrikazPortfolioSlikeDto { SlikaID = 20, PrethodnaSlikaID = 10, NarednaSlikaID = 30 };
        _portfolioService.Setup(p => p.DajPrikazSlike("m1", 20)).ReturnsAsync(prikaz);

        // Act
        var rezultat = await _kontroler.PortfolioSlika("m1", 20);

        // Assert
        var json = Assert.IsInstanceOfType<JsonResult>(rezultat);
        Assert.AreSame(prikaz, json.Value);
    }

    // SEC-9 iz checkliste, prevencija side-channel attacka
    public static IEnumerable<object[]> PorukeServisa => new[]
    {
    new object[] { "Majstor m99 ne postoji." },
    new object[] { "Slika 99 nije u portfoliju traženog majstora." },
};

    [TestMethod]
    [DynamicData(nameof(PorukeServisa))]
    public async Task PortfolioSlika_MajstorIliSlikaNePostoji_VracaIstiNotFoundBezPoruke(string poruka)
    {
        // Arrange
        _portfolioService.Setup(p => p.DajPrikazSlike(It.IsAny<string>(), It.IsAny<int>()))
            .ThrowsAsync(new KeyNotFoundException(poruka));

        // Act
        var rezultat = await _kontroler.PortfolioSlika("m99", 99);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task PortfolioSlika_PrazanIdMajstora_VracaBadRequest()
    {
        // Arrange
        _portfolioService.Setup(p => p.DajPrikazSlike("", 20))
            .ThrowsAsync(new ArgumentException("Id majstora je prazan."));

        // Act
        var rezultat = await _kontroler.PortfolioSlika("", 20);

        // Assert
        Assert.IsInstanceOfType<BadRequestResult>(rezultat);
    }
}
