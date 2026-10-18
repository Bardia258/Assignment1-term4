using Microsoft.AspNetCore.Mvc;
using MunicipalElections.Web.Services;
using System.Threading.Tasks;

namespace MunicipalElections.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMunicipalityService _municipalityService;

        public HomeController(IMunicipalityService municipalityService)
        {
            _municipalityService = municipalityService;
        }

        public async Task<IActionResult> Index()
        {
            var municipalities = await _municipalityService.GetAllMunicipalitiesAsync();
            return View(municipalities);
        }
    }
}