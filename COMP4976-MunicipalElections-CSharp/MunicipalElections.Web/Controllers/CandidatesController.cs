using Microsoft.AspNetCore.Mvc;
using MunicipalElections.Web.Services;
using System.Threading.Tasks;

namespace MunicipalElections.Web.Controllers
{
    public class CandidatesController : Controller
    {
        private readonly ICandidateService _candidateService;
        private readonly IMunicipalityService _municipalityService;

        public CandidatesController(ICandidateService candidateService, IMunicipalityService municipalityService)
        {
            _candidateService = candidateService;
            _municipalityService = municipalityService;
        }

        public async Task<IActionResult> Index(string searchString, int? municipalityId, int? positionId)
        {
            // Fetch the filtered candidates
            var candidates = await _candidateService.SearchCandidatesAsync(searchString, municipalityId, positionId);
            
            // Pass the current filter values to the View so they stay selected in the dropdowns
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentMunicipality = municipalityId;
            ViewBag.CurrentPosition = positionId;

            // Fetch dropdown data
            ViewBag.Municipalities = await _municipalityService.GetAllMunicipalitiesAsync();
            ViewBag.Positions = await _municipalityService.GetAllPositionsAsync();

            return View(candidates);
        }

        public async Task<IActionResult> Details(int id)
        {
            var candidate = await _candidateService.GetCandidateByIdAsync(id);
            if (candidate == null)
            {
                return NotFound();
            }
            return View(candidate);
        }
    }
}