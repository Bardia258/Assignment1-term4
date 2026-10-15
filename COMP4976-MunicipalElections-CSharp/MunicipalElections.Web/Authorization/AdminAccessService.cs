using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MunicipalElections.Web.Data;

namespace MunicipalElections.Web.Authorization;

public sealed record AdminAccess(string Username, string Role, int? MunicipalityId, string? MunicipalityName)
{
    public bool IsSuperAdmin => Role == AdminRoles.SuperAdmin;
}

public sealed class AdminAccessService(ApplicationDbContext db)
{
    public Task<AdminAccess?> GetAccessAsync(ClaimsPrincipal principal) =>
        principal.Identity?.IsAuthenticated == true
            ? GetAccessForUserAsync(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty)
            : Task.FromResult<AdminAccess?>(null);

    // Also used to check admin eligibility before SignInManager issues a login cookie.
    public async Task<AdminAccess?> GetAccessForUserAsync(string userId)
    {
        var user = await db.Users.AsNoTracking().Where(u => u.Id == userId)
            .Select(u => new { u.UserName, u.MunicipalityId }).SingleOrDefaultAsync();
        if (user is null) return null;

        // Query current membership on every authorization decision: stale cookie role
        // claims cannot retain permissions after an administrator changes an assignment.
        var roles = await (from membership in db.UserRoles.AsNoTracking()
                           join role in db.Roles.AsNoTracking() on membership.RoleId equals role.Id
                           where membership.UserId == userId
                           select role.Name).ToListAsync();
        var municipalityRoles = roles.Where(r => r is not null && AdminRoles.MunicipalityGroups.ContainsKey(r)).ToList();
        if (roles.Contains(AdminRoles.SuperAdmin))
        {
            return user.MunicipalityId is null && municipalityRoles.Count == 0
                ? new AdminAccess(user.UserName ?? string.Empty, AdminRoles.SuperAdmin, null, null)
                : null;
        }

        if (municipalityRoles.Count != 1 || user.MunicipalityId is null) return null;
        var roleName = municipalityRoles[0]!;
        var seedKey = AdminRoles.MunicipalityGroups[roleName];
        var municipality = await db.Municipalities.AsNoTracking()
            .Where(m => m.MunicipalityId == user.MunicipalityId && m.SeedKey == seedKey)
            .Select(m => new { m.MunicipalityId, m.Name }).SingleOrDefaultAsync();
        return municipality is null ? null
            : new AdminAccess(user.UserName ?? string.Empty, roleName, municipality.MunicipalityId, municipality.Name);
    }

    // This scopes positions/candidates. Municipality CREATE/EDIT/DELETE requires
    // the separate RequireSuperAdmin policy, even for a municipality's own admin.
    public async Task<bool> CanManageMunicipalityAsync(ClaimsPrincipal principal, int municipalityId)
    {
        var access = await GetAccessAsync(principal);
        return access is not null && (access.IsSuperAdmin || access.MunicipalityId == municipalityId)
            && await db.Municipalities.AnyAsync(m => m.MunicipalityId == municipalityId);
    }

    // For edit/delete, derive ownership from the stored resource, never a posted MunicipalityId.
    public async Task<bool> CanManageCandidateAsync(ClaimsPrincipal principal, int candidateId)
    {
        var municipalityId = await db.Candidates.Where(c => c.CandidateId == candidateId)
            .Select(c => (int?)c.MunicipalityId).SingleOrDefaultAsync();
        return municipalityId.HasValue && await CanManageMunicipalityAsync(principal, municipalityId.Value);
    }

    public async Task<bool> CanManagePositionAsync(ClaimsPrincipal principal, int positionId)
    {
        var municipalityId = await db.Positions.Where(p => p.PositionId == positionId)
            .Select(p => (int?)p.MunicipalityId).SingleOrDefaultAsync();
        return municipalityId.HasValue && await CanManageMunicipalityAsync(principal, municipalityId.Value);
    }

    // For create/reassignment, authorize the destination AND validate its position.
    public async Task<bool> CanAssignCandidateAsync(ClaimsPrincipal principal, int municipalityId, int positionId) =>
        await CanManageMunicipalityAsync(principal, municipalityId)
        && await db.Positions.AnyAsync(p => p.PositionId == positionId && p.MunicipalityId == municipalityId);
}
