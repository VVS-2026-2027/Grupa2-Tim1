


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
            SlikePortfolija = slikeID.Select(id => new PortfolioSlika { PortfolioSlikaID = id, IzvrsilacID = "m1", URL = $"slika{id}.png" }).ToList()
        });
    
}
