using Microsoft.AspNetCore.Mvc;
using MunicipalElections.Web.Services;
using MunicipalElections.Web.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MunicipalElections.Web.Controllers
{
    public class VotingGuideController : Controller
    {
        private readonly ICandidateService _candidateService;

        public VotingGuideController(ICandidateService candidateService)
        {
            _candidateService = candidateService;
        }

        public async Task<IActionResult> Index()
        {
            // Get the list of IDs from the session (or an empty list if nothing is there)
            var guideIds = HttpContext.Session.Get<List<int>>("VotingGuide") ?? new List<int>();
            
            var candidates = new List<CandidateViewModel>();
            foreach (var id in guideIds)
            {
                var candidate = await _candidateService.GetCandidateByIdAsync(id);
                if (candidate != null)
                {
                    candidates.Add(candidate);
                }
            }
            
            return View(candidates);
        }

        public async Task<IActionResult> Add(int id)
        {
            if (await _candidateService.GetCandidateByIdAsync(id) is null) return NotFound();
            var guideIds = HttpContext.Session.Get<List<int>>("VotingGuide") ?? new List<int>();
            
            // Add the ID if it's not already in the list
            if (!guideIds.Contains(id))
            {
                guideIds.Add(id);
                HttpContext.Session.Set("VotingGuide", guideIds);
                TempData["Message"] = "Candidate added to your guide!";
            }

            // Send the user back to the page they came from
            var referer = Request.Headers["Referer"].ToString();
            if (Uri.TryCreate(referer, UriKind.Absolute, out var uri)
                && string.Equals(uri.Authority, Request.Host.Value, StringComparison.OrdinalIgnoreCase)
                && string.Equals(uri.Scheme, Request.Scheme, StringComparison.OrdinalIgnoreCase))
                referer = uri.PathAndQuery;
            return Url.IsLocalUrl(referer) ? LocalRedirect(referer) : RedirectToAction("Index", "Candidates");
        }

        public IActionResult Remove(int id)
        {
            var guideIds = HttpContext.Session.Get<List<int>>("VotingGuide") ?? new List<int>();
            
            if (guideIds.Contains(id))
            {
                guideIds.Remove(id);
                HttpContext.Session.Set("VotingGuide", guideIds);
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Print()
        {
            // Get the list of IDs from the session (or an empty list if nothing is there)
            var guideIds = HttpContext.Session.Get<List<int>>("VotingGuide") ?? new List<int>();

            var candidates = new List<CandidateViewModel>();
            foreach (var id in guideIds)
            {
                var candidate = await _candidateService.GetCandidateByIdAsync(id);
                if (candidate != null)
            {
            candidates.Add(candidate);
        }
        }
    
        // Pass the current date to the view
        ViewBag.PrintDate = DateTime.Now.ToString("MMMM dd, yyyy");
    
        return View(candidates);
        }
    }
}
