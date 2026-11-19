

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
            var slike = izvrsilac!.SlikePortfolija!.ToList();

            var i = slike.FindIndex(s => s.PortfolioSlikaID == slikaID);

            return new PrikazPortfolioSlikeDto
            {
                SlikaID = slike[i].PortfolioSlikaID,
                NarednaSlikaID = slike[i + 1].PortfolioSlikaID,
                PrethodnaSlikaID = slike[i - 1].PortfolioSlikaID,
                TrenutnaPozicija = i + 1,
                UkupanPortfolio = slike.Count
            };

        }
    }
}
