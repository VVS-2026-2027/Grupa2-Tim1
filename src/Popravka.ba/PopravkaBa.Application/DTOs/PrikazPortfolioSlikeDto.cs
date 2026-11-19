

namespace PopravkaBa.Application.DTOs
{
    public class PrikazPortfolioSlikeDto
    {
        public int SlikaID { get; set; }
        
        public int NarednaSlikaID { get; set; }
        public int PrethodnaSlikaID { get; set; }
        public int TrenutnaPozicija { get; set; }
        public int UkupanPortfolio { get; set; }

        public string URL { get; set; }
        public string? Opis { get; set; }
    }
}
