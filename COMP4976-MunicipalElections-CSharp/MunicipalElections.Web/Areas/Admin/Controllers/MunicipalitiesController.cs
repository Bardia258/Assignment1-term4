using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MunicipalElections.Web.Areas.Admin.Models;
using MunicipalElections.Web.Authorization;
using MunicipalElections.Web.Data;
using MunicipalElections.Web.Models;
using MunicipalElections.Web.Services;

namespace MunicipalElections.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AdminPolicies.RequireSuperAdmin)]
public sealed class MunicipalitiesController(ApplicationDbContext db, AdminAccessService access, IImageStorage images,
    RoleManager<IdentityRole> roles) : ManagementController(db, access, images)
{
    public async Task<IActionResult> Index() => View(await Db.Municipalities.AsNoTracking().OrderBy(m => m.Name).ToListAsync());
    public async Task<IActionResult> Details([FromRoute] int id)
    {
        var item = await Db.Municipalities.AsNoTracking().SingleOrDefaultAsync(m => m.MunicipalityId == id);
        return item is null ? NotFound() : View(item);
    }
    [HttpGet] public IActionResult Create() => View(new MunicipalityInput { ElectionDate = DateTime.Today });
    [HttpPost]
    public async Task<IActionResult> Create(MunicipalityInput model)
    {
        await ValidateAsync(model);
        if (!ModelState.IsValid) return View(model);
        var item = new Municipality { Name = model.Name.Trim(), Province = model.Province.Trim(),
            Description = model.Description?.Trim() ?? string.Empty, ElectionDate = model.ElectionDate!.Value.Date };
        Db.Add(item);
        if (!await PersistAsync(item, image: model.Image, category: "logos", assignImage: name => item.LogoFileName = name,
            afterSave: async () =>
            {
                // Random role name is stable across municipality renames and avoids role-name collisions.
                var role = new IdentityRole($"Municipality Admin {Guid.NewGuid():N}");
                var result = await roles.CreateAsync(role);
                if (!result.Succeeded) throw new InvalidOperationException("Unable to create administrator group.");
                Db.MunicipalityAdminGroups.Add(new MunicipalityAdminGroup { MunicipalityId = item.MunicipalityId, RoleId = role.Id });
                await Db.SaveChangesAsync();
            })) return View(model);
        return RedirectToAction(nameof(Details), "Municipalities", new { area = "Admin", id = item.MunicipalityId });
    }
    [HttpGet]
    public async Task<IActionResult> Edit([FromRoute] int id)
    {
        var item = await Db.Municipalities.FindAsync(id);
        return item is null ? NotFound() : View(new MunicipalityInput { Name = item.Name, Province = item.Province,
            ElectionDate = item.ElectionDate, Description = item.Description, CurrentImage = item.LogoFileName, Revision = item.Revision });
    }
    [HttpPost]
    public async Task<IActionResult> Edit([FromRoute] int id, MunicipalityInput model)
    {
        var item = await Db.Municipalities.FindAsync(id);
        if (item is null) return NotFound();
        await ValidateAsync(model, id);
        model.CurrentImage = item.LogoFileName;
        if (!ModelState.IsValid) return View(model);
        var oldImage = item.LogoFileName;
        item.Name = model.Name.Trim(); item.Province = model.Province.Trim(); item.ElectionDate = model.ElectionDate!.Value.Date;
        item.Description = model.Description?.Trim() ?? string.Empty;
        if (!await PersistAsync(item, model.Revision, model.Image, "logos", name => item.LogoFileName = name, oldImage)) return View(model);
        return RedirectToAction(nameof(Details), "Municipalities", new { area = "Admin", id });
    }
    [HttpGet]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var item = await Db.Municipalities.FindAsync(id);
        return item is null ? NotFound() : View(item);
    }
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed([FromRoute] int id, string? revision)
    {
        var item = await Db.Municipalities.FindAsync(id);
        if (item is null) return NotFound();
        var group = await Db.MunicipalityAdminGroups.SingleOrDefaultAsync(g => g.MunicipalityId == id);
        if (await Db.Positions.AnyAsync(p => p.MunicipalityId == id) || await Db.Candidates.AnyAsync(c => c.MunicipalityId == id)
            || await Db.Users.AnyAsync(u => u.MunicipalityId == id)
            || (group != null && await Db.UserRoles.AnyAsync(r => r.RoleId == group.RoleId)))
        {
            ModelState.AddModelError(string.Empty, "Remove or reassign related positions, candidates, and administrator assignments before deleting this municipality.");
            return View("Delete", item);
        }
        // Unmap the empty group. Retain its unassigned Identity role for audit/history;
        // without the mapping it grants no municipality permissions.
        if (group is not null) Db.Remove(group);
        Db.Remove(item);
        if (!await PersistAsync(item, revision, category: "logos", oldImage: item.LogoFileName)) return View("Delete", item);
        return RedirectToAction(nameof(Index), "Municipalities", new { area = "Admin" });
    }
    private async Task ValidateAsync(MunicipalityInput model, int id = 0)
    {
        var name = (model.Name ?? string.Empty).Trim();
        var province = (model.Province ?? string.Empty).Trim();
        if (await Db.Municipalities.AnyAsync(m => m.MunicipalityId != id && m.Name == name && m.Province == province))
            ModelState.AddModelError(nameof(model.Name), "A municipality with this name and province already exists (case-insensitive).");
    }
}
