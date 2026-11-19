using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PopravkaBa.Application.Services.Interface;

namespace PopravkaBa.Web.Controllers
{
    public class PortfolioController : Controller
    {
        private readonly IPortfolioService _portfolioService;

        public PortfolioController(IPortfolioService portfolio) =>
            _portfolioService = portfolio;


        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> PortfolioSlika(string id, int slikaId)
        {
            try
            {
                return Json(await _portfolioService.DajPrikazSlike(id, slikaId));
            }
            catch (KeyNotFoundException)
            {
                // Bez poruke: ne vidi da li ne postoji majstor ili slika (SEC-9 sa checkliste)
                return NotFound();
            }
            catch (ArgumentException)
            {
                // Neispravan zahtjev, npr. prazan id majstora
                return BadRequest();
            }
        }
    }
}
