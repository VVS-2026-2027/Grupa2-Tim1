

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
            var izvrsilac = await _izvrsilacRepository.DajProfilPoIdAsync(izvrsilacID)
                ?? throw new KeyNotFoundException($"Majstor {izvrsilacID} ne postoji.");
            var slike = izvrsilac.SlikePortfolija!.OrderBy(s => s.PortfolioSlikaID).ToList();

            var i = slike.FindIndex(s => s.PortfolioSlikaID == slikaID);
            if (i < 0) throw new KeyNotFoundException($"Slika {slikaID} nije u portfoliju traženog majstora.");
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


    }
}
