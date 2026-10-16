using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MunicipalElections.Web.Areas.Admin.Models;
using MunicipalElections.Web.Authorization;
using MunicipalElections.Web.Data;
using MunicipalElections.Web.Models;
using MunicipalElections.Web.Services;

namespace MunicipalElections.Web.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class CandidatesController(ApplicationDbContext db, AdminAccessService access, IImageStorage images)
    : ManagementController(db, access, images)
{
    public async Task<IActionResult> Index(string? search, int? municipalityId)
    {
        var admin = (await Access.GetAccessAsync(User))!;
        var query = Db.Candidates.AsNoTracking().Include(c => c.Municipality).Include(c => c.Position)
            .Where(c => admin.IsSuperAdmin || c.MunicipalityId == admin.MunicipalityId);
        if (municipalityId.HasValue)
        {
            if (!await Access.CanManageMunicipalityAsync(User, municipalityId.Value)) return Forbid();
            query = query.Where(c => c.MunicipalityId == municipalityId);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(c => c.Name.ToUpper().Contains(term));
        }
        ViewBag.Search = search;
        var options = await MunicipalityOptionsAsync();
        foreach (var option in options) option.Selected = option.Value == municipalityId?.ToString();
        ViewBag.Municipalities = options;
        ViewBag.MunicipalityId = municipalityId;
        return View(await query.OrderBy(c => c.Municipality.Name).ThenBy(c => c.Name).ToListAsync());
    }
    public async Task<IActionResult> Details([FromRoute] int id)
    {
        var item = await Db.Candidates.Include(c => c.Municipality).Include(c => c.Position).SingleOrDefaultAsync(c => c.CandidateId == id);
        if (item is null) return NotFound();
        return !await Access.CanManageCandidateAsync(User, id) ? Forbid() : View(item);
    }
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var admin = (await Access.GetAccessAsync(User))!;
        var model = new CandidateInput { MunicipalityId = admin.MunicipalityId ?? 0 };
        await OptionsAsync(model);
        return View(model);
    }
    [HttpPost]
    public async Task<IActionResult> Create(CandidateInput model)
    {
        if (model.MunicipalityId > 0 && !await Access.CanManageMunicipalityAsync(User, model.MunicipalityId)) return Forbid();
        await ValidateAsync(model);
        if (ModelState.IsValid)
        {
            var item = new Candidate();
            Copy(model, item);
            Db.Add(item);
            if (await PersistAsync(item, image: model.Image, assignImage: name => item.PhotoFileName = name))
                return RedirectToAction(nameof(Details), new { id = item.CandidateId });
        }
        await OptionsAsync(model);
        return View(model);
    }
    [HttpGet]
    public async Task<IActionResult> Edit([FromRoute] int id)
    {
        var item = await Db.Candidates.FindAsync(id);
        if (item is null) return NotFound();
        if (!await Access.CanManageCandidateAsync(User, id)) return Forbid();
        var model = new CandidateInput { Name = item.Name, Profile = item.Profile, Website = item.Website, Email = item.Email,
            MunicipalityId = item.MunicipalityId, PositionId = item.PositionId, CurrentImage = item.PhotoFileName, Revision = item.Revision };
        await OptionsAsync(model);
        return View(model);
    }
    [HttpPost]
    public async Task<IActionResult> Edit([FromRoute] int id, CandidateInput model)
    {
        var item = await Db.Candidates.FindAsync(id);
        if (item is null) return NotFound();
        if (!await Access.CanManageCandidateAsync(User, id)) return Forbid();
        if (model.MunicipalityId > 0 && !await Access.CanManageMunicipalityAsync(User, model.MunicipalityId)) return Forbid();
        model.CurrentImage = item.PhotoFileName;
        await ValidateAsync(model);
        if (ModelState.IsValid)
        {
            var oldImage = item.PhotoFileName;
            Copy(model, item); // DateCreated, SeedKey, and image filename are never bound from input.
            if (await PersistAsync(item, model.Revision, model.Image, assignImage: name => item.PhotoFileName = name, oldImage: oldImage))
                return RedirectToAction(nameof(Details), new { id });
        }
        await OptionsAsync(model);
        return View(model);
    }
    [HttpGet]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var item = await Db.Candidates.Include(c => c.Municipality).Include(c => c.Position).SingleOrDefaultAsync(c => c.CandidateId == id);
        if (item is null) return NotFound();
        return !await Access.CanManageCandidateAsync(User, id) ? Forbid() : View(item);
    }
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed([FromRoute] int id, string? revision)
    {
        var item = await Db.Candidates.Include(c => c.Municipality).Include(c => c.Position).SingleOrDefaultAsync(c => c.CandidateId == id);
        if (item is null) return NotFound();
        if (!await Access.CanManageCandidateAsync(User, id)) return Forbid();
        Db.Remove(item);
        if (!await PersistAsync(item, revision, oldImage: item.PhotoFileName)) return View("Delete", item);
        return RedirectToAction(nameof(Index));
    }
    [HttpGet]
    public async Task<IActionResult> PositionOptions(int municipalityId)
    {
        if (!await Access.CanManageMunicipalityAsync(User, municipalityId)) return Forbid();
        // Authorized MVC helper for the dependent position dropdown.
        return Json(await Db.Positions.AsNoTracking().Where(p => p.MunicipalityId == municipalityId)
            .OrderBy(p => p.Name).Select(p => new { id = p.PositionId, name = p.Name }).ToListAsync());
    }
    private async Task ValidateAsync(CandidateInput model)
    {
        if (model.MunicipalityId > 0 && model.PositionId > 0 && !await Access.CanAssignCandidateAsync(User, model.MunicipalityId, model.PositionId))
            ModelState.AddModelError(nameof(model.PositionId), "Choose a valid position belonging to the selected municipality.");
    }
    private async Task OptionsAsync(CandidateInput model)
    {
        model.Municipalities = await MunicipalityOptionsAsync();
        model.Positions = await Db.Positions.AsNoTracking().Where(p => p.MunicipalityId == model.MunicipalityId)
            .OrderBy(p => p.Name).Select(p => new SelectListItem(p.Name, p.PositionId.ToString())).ToListAsync();
    }
    private static void Copy(CandidateInput model, Candidate item)
    {
        item.Name = model.Name.Trim(); item.Profile = model.Profile.Trim();
        item.MunicipalityId = model.MunicipalityId; item.PositionId = model.PositionId;
        item.Website = string.IsNullOrWhiteSpace(model.Website) ? null : model.Website.Trim();
        item.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
    }
}
