using Microsoft.AspNetCore.Mvc;
using MunicipalElections.Web.Authorization;
using MunicipalElections.Web.Data;
using MunicipalElections.Web.Areas.Admin.Models;
using Microsoft.EntityFrameworkCore;

namespace MunicipalElections.Web.Areas.Admin.Controllers;

[Area("Admin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class HomeController(AdminAccessService accessService, ApplicationDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var access = await accessService.GetAccessAsync(User);
        if (access is null) return Forbid();
        var municipalityCount = await db.Municipalities.CountAsync(m => access.IsSuperAdmin || m.MunicipalityId == access.MunicipalityId);
        var positionCount = await db.Positions.CountAsync(p => access.IsSuperAdmin || p.MunicipalityId == access.MunicipalityId);
        var candidateCount = await db.Candidates.CountAsync(c => access.IsSuperAdmin || c.MunicipalityId == access.MunicipalityId);
        return View(new DashboardModel(access, municipalityCount, positionCount, candidateCount));
    }
}
