using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PopravkaBa.Application.Services.Interface;

namespace PopravkaBa.Web.Controllers
{
    public class PortfolioController : Controller
    {
        private readonly IPortfolioService _portfolioService;

        public PortfolioController(IPortfolioService portfolio) {
            _portfolioService = portfolio;
        }

        [AllowAnonymous]
        [HttpGet("/portfolio/image/")]
        public async Task<IActionResult> PortfolioSlika(string ID, int slikaID)
        {
            var dto = await _portfolioService.DajPrikazSlike(ID, slikaID);
            return Json(dto);
        }
    }
}
