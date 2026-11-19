

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
    }
}
