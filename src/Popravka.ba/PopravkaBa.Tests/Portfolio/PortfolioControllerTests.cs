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
}
