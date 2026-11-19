

using PopravkaBa.Application.DTOs;
using PopravkaBa.Application.Services.Interface;
using PopravkaBa.Domain.Interfaces;

namespace PopravkaBa.Application.Services.Implementation
{
    public class PortfolioService : IPortfolioService
    {
        private readonly IIzvrsilacUslugeRepository _izvrsilacRepository;

        public PortfolioService(IIzvrsilacUslugeRepository repo)
        {
            _izvrsilacRepository = repo;
        }
         
        public async Task<PrikazPortfolioSlikeDto> DajPrikazSlike(string izvrsilacID, int slikaID)
        {
            var izvrsilac = await _izvrsilacRepository.DajProfilPoIdAsync(izvrsilacID);
            var slike = izvrsilac!.SlikePortfolija!.OrderBy(s => s.PortfolioSlikaID).ToList();

            var i = slike.FindIndex(s => s.PortfolioSlikaID == slikaID);
            var n = slike.Count;
            return new PrikazPortfolioSlikeDto
            {
                SlikaID = slike[i].PortfolioSlikaID,
                NarednaSlikaID = slike[(i + 1) % n].PortfolioSlikaID,
                PrethodnaSlikaID = slike[(i - 1 + n) % n].PortfolioSlikaID,
                TrenutnaPozicija = i + 1,
                UkupanPortfolio = n,
                Opis = slike[i].Opis,
                URL = slike[i].URL
            };

        }

        [TestMethod]
        public async Task DajPrikazSlike_JednaSlika_StrelicePokazujuNaNju()
        {
            // Arrange
            MajstorImaSlike(10);

            // Act
            var prikaz = await _servis.DajPrikazSlike("m1", 10);

            // Assert
            Assert.AreEqual(10, prikaz.PrethodnaId);
            Assert.AreEqual(10, prikaz.SljedecaId);
        }

        [TestMethod]
        public async Task DajPrikazSlike_DvijeSlike_ObjeStrelicePokazujuNaDrugu()
        {
            // Arrange
            MajstorImaSlike(10, 20);

            // Act
            var prikaz = await _servis.DajPrikazSlike("m1", 10);

            // Assert
            Assert.AreEqual(20, prikaz.PrethodnaId);
            Assert.AreEqual(20, prikaz.SljedecaId);
        }
    }
}
