


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

    private void StubMajstorSaSlikama(params int[] slikeID) =>
        _izvrsilacRepository.Setup(r => r.DajProfilPoIdAsync("m1")).ReturnsAsync(new Majstor
        {
            Id = "m1",
            SlikePortfolija = slikeID.Select(id => new PortfolioSlika 
            { 
                PortfolioSlikaID = id, 
                IzvrsilacID = "m1", 
                URL = $"slika{id}.png" }).ToList()
        });
    [TestMethod]
    public async Task DajPrikazSlike_SrednjaSlika_VracaSlikuIPokazujeNaOstale()
    {
        // Arrange
        StubMajstorSaSlikama(0, 1, 2);
        // Act
        var slikaViewDTO = await _portfolioService.DajPrikazSlike("m1", 1);
        // Assert
        Assert.AreEqual(1, slikaViewDTO.SlikaID);
        Assert.AreEqual(0, slikaViewDTO.PrethodnaSlikaID);
        Assert.AreEqual(2, slikaViewDTO.NarednaSlikaID);
        Assert.AreEqual(2, slikaViewDTO.TrenutnaPozicija);
        Assert.AreEqual(3, slikaViewDTO.UkupanPortfolio);

    }
}
