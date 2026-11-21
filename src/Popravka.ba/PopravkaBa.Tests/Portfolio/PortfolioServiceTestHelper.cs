using PopravkaBa.Domain.Interfaces;
using PopravkaBa.Domain.Models;

namespace PopravkaBa.Tests.Portfolio;

// Pomoćne metode za PortfolioServiceTests
internal static class PortfolioServiceTestHelper
{
    // Stub: majstor sa datim id-em ima slike sa ovim id-evima, u ovom redoslijedu
    internal static void StubMajstorSaSlikama(this Mock<IIzvrsilacUslugeRepository> repo, string majstorId, params int[] ids) =>
        repo.Setup(r => r.DajProfilPoIdAsync(majstorId)).ReturnsAsync(new Majstor
        {
            Id = majstorId,
            SlikePortfolija = ids.Select(id => new PortfolioSlika
            { PortfolioSlikaID = id, IzvrsilacID = majstorId, URL = $"slika{id}.png" }).ToList()
        });

    // Kraći oblik za većinu testova, gdje je majstor uvijek "m1"
    internal static void StubMajstorSaSlikama(this Mock<IIzvrsilacUslugeRepository> repo, params int[] ids) =>
        repo.StubMajstorSaSlikama("m1", ids);
}
