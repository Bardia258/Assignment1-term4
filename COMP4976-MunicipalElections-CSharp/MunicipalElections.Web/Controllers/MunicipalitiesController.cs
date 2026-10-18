using Microsoft.AspNetCore.Mvc;
using MunicipalElections.Web.Services;
using System.Threading.Tasks;

namespace MunicipalElections.Web.Controllers
{
    public class MunicipalitiesController : Controller
    {
        private readonly IMunicipalityService _municipalityService;
        private readonly ICandidateService _candidateService;

        public MunicipalitiesController(IMunicipalityService municipalityService, ICandidateService candidateService)
        {
            _municipalityService = municipalityService;
            _candidateService = candidateService;
        }

        public async Task<IActionResult> Index()
        {
            var municipalities = await _municipalityService.GetAllMunicipalitiesAsync();
            return View(municipalities);
        }

        public async Task<IActionResult> Details(int id)
        {
            var municipality = await _municipalityService.GetMunicipalityByIdAsync(id);
            if (municipality == null) return NotFound();

            var candidates = await _candidateService.GetCandidatesByMunicipalityAsync(id);
            ViewBag.Candidates = candidates;

            return View(municipality);
        }
    }
}