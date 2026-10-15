using Microsoft.AspNetCore.Mvc;
using MunicipalElections.Web.Authorization;

namespace MunicipalElections.Web.Areas.Admin.Controllers;

[Area("Admin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class HomeController(AdminAccessService accessService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var access = await accessService.GetAccessAsync(User);
        return access is null ? Forbid() : View(access);
    }
}
