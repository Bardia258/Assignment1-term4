using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MunicipalElections.Web.Authorization;
using MunicipalElections.Web.Data;
using MunicipalElections.Web.Services;

namespace MunicipalElections.Web.Areas.Admin.Controllers;

[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[RequestSizeLimit(3 * 1024 * 1024)]
public abstract class ManagementController(ApplicationDbContext db, AdminAccessService access, IImageStorage images) : Controller
{
    protected ApplicationDbContext Db => db;
    protected AdminAccessService Access => access;
    protected IImageStorage Images => images;

    protected async Task<List<SelectListItem>> MunicipalityOptionsAsync()
    {
        var admin = await access.GetAccessAsync(User);
        return await db.Municipalities.AsNoTracking()
            .Where(m => admin != null && (admin.IsSuperAdmin || m.MunicipalityId == admin.MunicipalityId))
            .OrderBy(m => m.Name).Select(m => new SelectListItem(m.Name, m.MunicipalityId.ToString())).ToListAsync();
    }

    protected async Task<bool> PersistAsync(object entity, string? revision = null, IFormFile? image = null,
        string category = "candidates", Action<string>? assignImage = null, string? oldImage = null,
        Func<Task>? afterSave = null)
    {
        string? newImage = null;
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            if (db.Entry(entity).State != EntityState.Added)
            {
                if (string.IsNullOrWhiteSpace(revision)) throw new DbUpdateConcurrencyException();
                db.Entry(entity).Property("Revision").OriginalValue = revision;
            }
            if (image is not null)
            {
                newImage = await images.SaveAsync(image, category);
                assignImage!(newImage);
            }
            await db.SaveChangesAsync();
            if (afterSave is not null) await afterSave();
            await transaction.CommitAsync();
        }
        catch (Exception error) when (error is ValidationException or DbUpdateException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            await transaction.RollbackAsync();
            db.ChangeTracker.Clear();
            if (newImage is not null) await images.DeleteIfUnreferencedAsync(newImage, category);
            if (error is ValidationException) ModelState.AddModelError("Image", error.Message);
            else if (error is DbUpdateConcurrencyException)
            {
                Response.StatusCode = StatusCodes.Status409Conflict;
                ModelState.AddModelError(string.Empty, "This record changed or was deleted. Reload the page before trying again.");
            }
            else ModelState.AddModelError(string.Empty, "Unable to save. Check for duplicate names or related records, then reload and try again.");
            return false;
        }
        if (image is not null || db.Entry(entity).State == EntityState.Detached)
            await images.DeleteIfUnreferencedAsync(oldImage, category);
        TempData["Success"] = "Changes saved successfully.";
        return true;
    }
}
