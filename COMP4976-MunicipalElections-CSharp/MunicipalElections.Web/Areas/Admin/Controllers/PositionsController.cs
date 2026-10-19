using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MunicipalElections.Web.Areas.Admin.Models;
using MunicipalElections.Web.Authorization;
using MunicipalElections.Web.Data;
using MunicipalElections.Web.Models;
using MunicipalElections.Web.Services;

namespace MunicipalElections.Web.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class PositionsController(ApplicationDbContext db, AdminAccessService access, IImageStorage images)
    : ManagementController(db, access, images)
{
    public async Task<IActionResult> Index()
    {
        var admin = (await Access.GetAccessAsync(User))!;
        return View(await Db.Positions.AsNoTracking().Include(p => p.Municipality)
            .Where(p => admin.IsSuperAdmin || p.MunicipalityId == admin.MunicipalityId).OrderBy(p => p.Municipality.Name).ThenBy(p => p.Name).ToListAsync());
    }
    public async Task<IActionResult> Details([FromRoute] int id)
    {
        var item = await Db.Positions.Include(p => p.Municipality).SingleOrDefaultAsync(p => p.PositionId == id);
        if (item is null) return NotFound();
        return !await Access.CanManagePositionAsync(User, id) ? Forbid() : View(item);
    }
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var admin = (await Access.GetAccessAsync(User))!;
        return View(new PositionInput { MunicipalityId = admin.MunicipalityId ?? 0, Municipalities = await MunicipalityOptionsAsync() });
    }
    [HttpPost]
    public async Task<IActionResult> Create(PositionInput model)
    {
        if (model.MunicipalityId > 0 && !await Access.CanManageMunicipalityAsync(User, model.MunicipalityId)) return Forbid();
        await ValidateAsync(model);
        if (ModelState.IsValid)
        {
            var item = new Position { Name = model.Name.Trim(), MunicipalityId = model.MunicipalityId, SeatsAvailable = model.SeatsAvailable };
            Db.Add(item);
            if (await PersistAsync(item)) return RedirectToAction(nameof(Details), new { id = item.PositionId });
        }
        model.Municipalities = await MunicipalityOptionsAsync();
        return View(model);
    }
    [HttpGet]
    public async Task<IActionResult> Edit([FromRoute] int id)
    {
        var item = await Db.Positions.FindAsync(id);
        if (item is null) return NotFound();
        if (!await Access.CanManagePositionAsync(User, id)) return Forbid();
        return View(new PositionInput { Name = item.Name, SeatsAvailable = item.SeatsAvailable, MunicipalityId = item.MunicipalityId,
            Revision = item.Revision, Municipalities = await MunicipalityOptionsAsync() });
    }
    [HttpPost]
    public async Task<IActionResult> Edit([FromRoute] int id, PositionInput model)
    {
        var item = await Db.Positions.FindAsync(id);
        if (item is null) return NotFound();
        if (!await Access.CanManagePositionAsync(User, id)) return Forbid();
        if (model.MunicipalityId != item.MunicipalityId) return Forbid();
        await ValidateAsync(model, id);
        if (ModelState.IsValid)
        {
            item.Name = model.Name.Trim(); item.SeatsAvailable = model.SeatsAvailable;
            if (await PersistAsync(item, model.Revision)) return RedirectToAction(nameof(Details), new { id });
        }
        model.Municipalities = await MunicipalityOptionsAsync();
        return View(model);
    }
    [HttpGet]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var item = await Db.Positions.Include(p => p.Municipality).SingleOrDefaultAsync(p => p.PositionId == id);
        if (item is null) return NotFound();
        return !await Access.CanManagePositionAsync(User, id) ? Forbid() : View(item);
    }
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed([FromRoute] int id, string? revision)
    {
        var item = await Db.Positions.Include(p => p.Municipality).SingleOrDefaultAsync(p => p.PositionId == id);
        if (item is null) return NotFound();
        if (!await Access.CanManagePositionAsync(User, id)) return Forbid();
        if (await Db.Candidates.AnyAsync(c => c.PositionId == id))
        {
            ModelState.AddModelError(string.Empty, "This position has candidates. Reassign or remove them before deleting it.");
            return View("Delete", item);
        }
        Db.Remove(item);
        if (!await PersistAsync(item, revision)) return View("Delete", item);
        return RedirectToAction(nameof(Index));
    }
    private async Task ValidateAsync(PositionInput model, int id = 0)
    {
        var name = (model.Name ?? string.Empty).Trim();
        if (await Db.Positions.AnyAsync(p => p.PositionId != id && p.MunicipalityId == model.MunicipalityId && p.Name == name))
            ModelState.AddModelError(nameof(model.Name), "This municipality already has a position with that name (case-insensitive).");
    }
}
