using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using MunicipalElections.Web.Models;
using MunicipalElections.Web.Services;
using System.Diagnostics;
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

        [AllowAnonymous, IgnoreAntiforgeryToken]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            var originalPath = HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Path;
            Response.StatusCode = StatusCodes.Status500InternalServerError;

            // Exception re-execution preserves the original HTTP method. Handle API
            // failures as JSON even when the client requests an HTML response.
            if (originalPath is not null && new PathString(originalPath).StartsWithSegments("/api"))
                return Problem(statusCode: StatusCodes.Status500InternalServerError,
                    title: "An unexpected error occurred.");

            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}
