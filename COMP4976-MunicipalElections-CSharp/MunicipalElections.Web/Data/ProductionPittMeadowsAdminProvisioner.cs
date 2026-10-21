using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MunicipalElections.Web.Authorization;
using MunicipalElections.Web.Models;

namespace MunicipalElections.Web.Data;

public static class ProductionPittMeadowsAdminProvisioner
{
    public const string OwnershipClaimType = "municipal-elections:production-pitt-meadows-admin";

    public static async Task<bool> ProvisionAsync(IServiceProvider services, IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var username = configuration["BootstrapAdmin:Username"]?.Trim();
        var email = configuration["BootstrapAdmin:Email"]?.Trim();
        if (!string.Equals(username, "pm", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(email) || email.Length > 254 || !new EmailAddressAttribute().IsValid(email))
            throw new DatabaseInitializationException("Set BootstrapAdmin:Username to pm and configure a valid BootstrapAdmin:Email.");

        var db = services.GetRequiredService<ApplicationDbContext>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        var access = services.GetRequiredService<AdminAccessService>();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Find the existing municipality; never create or modify election records.
        var municipalityIds = await db.Municipalities.AsNoTracking()
            .Where(m => m.SeedKey == "demo:pitt-meadows" || (m.Name == "Pitt Meadows" && m.Province == "BC"))
            .Select(m => m.MunicipalityId).ToListAsync(cancellationToken);
        if (municipalityIds.Count != 1)
            throw new DatabaseInitializationException("Exactly one existing Pitt Meadows municipality is required. No records were changed.");
        var municipalityId = municipalityIds[0];

        var claims = await db.UserClaims.Where(c => c.ClaimType == OwnershipClaimType)
            .Select(c => new { c.UserId, c.ClaimValue }).ToListAsync(cancellationToken);
        if (claims.Count > 0)
        {
            if (claims.Count != 1 || claims[0].UserId != claims[0].ClaimValue)
                throw new DatabaseInitializationException("Pitt Meadows administrator ownership is inconsistent. No accounts were changed.");
            var owner = await users.FindByIdAsync(claims[0].UserId);
            if (owner is null || owner.NormalizedUserName != users.NormalizeName(username)
                || owner.NormalizedEmail != users.NormalizeEmail(email) || !await HasExpectedAccessAsync(owner))
                throw new DatabaseInitializationException("The provisioned Pitt Meadows administrator no longer matches the configuration or permissions. No accounts were changed.");

            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        if (await users.FindByNameAsync(username!) is not null || await users.FindByEmailAsync(email) is not null)
            throw new DatabaseInitializationException("The configured username or email belongs to an existing account. Refusing to adopt or promote it.");
        var password = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(password) || password.Length > 128)
            throw new DatabaseInitializationException("Configure BootstrapAdmin:Password. It must meet the application's Identity password rules.");

        var role = await roles.FindByNameAsync(AdminRoles.PittMeadowsAdmin);
        var mapping = await db.MunicipalityAdminGroups.FindAsync([municipalityId], cancellationToken);
        if ((role is not null && (role.Name != AdminRoles.PittMeadowsAdmin
                || await db.MunicipalityAdminGroups.AnyAsync(g => g.RoleId == role.Id && g.MunicipalityId != municipalityId, cancellationToken)))
            || (mapping is not null && mapping.RoleId != role?.Id))
            throw new DatabaseInitializationException("An existing municipality role mapping conflicts with Pitt Meadows. No permissions were changed.");

        if (role is null)
        {
            role = new IdentityRole(AdminRoles.PittMeadowsAdmin);
            RequireSuccess(await roles.CreateAsync(role));
        }
        if (mapping is null)
        {
            db.MunicipalityAdminGroups.Add(new MunicipalityAdminGroup { MunicipalityId = municipalityId, RoleId = role.Id });
            await db.SaveChangesAsync(cancellationToken);
        }

        var user = new ApplicationUser
        {
            UserName = username, Email = email, MunicipalityId = municipalityId, LockoutEnabled = true
        };
        // Identity validates and hashes the supplied password. No classroom password
        // is embedded here, and this command never assigns the Super Admin role.
        RequireSuccess(await users.CreateAsync(user, password));
        RequireSuccess(await users.AddToRoleAsync(user, AdminRoles.PittMeadowsAdmin));
        RequireSuccess(await users.AddClaimAsync(user, new Claim(OwnershipClaimType, user.Id)));
        if (!await HasExpectedAccessAsync(user))
            throw new DatabaseInitializationException("Pitt Meadows authorization verification failed. Account creation was rolled back.");

        await transaction.CommitAsync(cancellationToken);
        return true;

        async Task<bool> HasExpectedAccessAsync(ApplicationUser user)
        {
            var membership = await users.GetRolesAsync(user);
            var permissions = await access.GetAccessForUserAsync(user.Id);
            return membership.Count == 1 && membership[0] == AdminRoles.PittMeadowsAdmin
                && permissions is { IsSuperAdmin: false } && permissions.MunicipalityId == municipalityId;
        }
    }

    private static void RequireSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new DatabaseInitializationException("Identity rejected Pitt Meadows administrator provisioning. Check the configuration and Identity requirements. No accounts were changed.");
    }
}
