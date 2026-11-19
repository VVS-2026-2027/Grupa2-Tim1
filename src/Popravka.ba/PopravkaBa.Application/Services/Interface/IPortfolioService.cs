

using PopravkaBa.Application.DTOs;
using System.Globalization;

namespace PopravkaBa.Application.Services.Interface
{
    public interface IPortfolioService
    {
        Task<PrikazPortfolioSlikeDto> DajPrikazSlike(string izvrsilacID, int slikaID);
    }
}
