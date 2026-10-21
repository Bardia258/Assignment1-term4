using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MunicipalElections.Web.Authorization;
using MunicipalElections.Web.Models;

namespace MunicipalElections.Web.Data;

public static class ProductionAdminProvisioner
{
    // Ownership is committed with account creation. A public account with a matching
    // username/email must never be adopted or promoted by this command.
    public const string OwnershipClaimType = "municipal-elections:production-super-admin";

    public static async Task<bool> ProvisionAsync(IServiceProvider services, IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var username = configuration["BootstrapAdmin:Username"]?.Trim();
        var email = configuration["BootstrapAdmin:Email"]?.Trim();
        if (string.IsNullOrWhiteSpace(username) || username.Length > 100
            || string.IsNullOrWhiteSpace(email) || email.Length > 254 || !new EmailAddressAttribute().IsValid(email))
            throw new DatabaseInitializationException("Configure a valid BootstrapAdmin:Username and BootstrapAdmin:Email.");

        var db = services.GetRequiredService<ApplicationDbContext>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        var access = services.GetRequiredService<AdminAccessService>();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var claims = await db.UserClaims.Where(c => c.ClaimType == OwnershipClaimType)
            .Select(c => new { c.UserId, c.ClaimValue }).ToListAsync(cancellationToken);
        if (claims.Count > 0)
        {
            if (claims.Count != 1 || claims[0].ClaimValue != claims[0].UserId)
                throw new DatabaseInitializationException("Production administrator ownership is inconsistent. No accounts were changed.");
            var owner = await users.FindByIdAsync(claims[0].UserId);
            if (owner is null || owner.NormalizedUserName != users.NormalizeName(username)
                || owner.NormalizedEmail != users.NormalizeEmail(email)
                || (await access.GetAccessForUserAsync(owner.Id))?.IsSuperAdmin != true)
                throw new DatabaseInitializationException("The provisioned administrator no longer matches the configuration or permissions. No accounts were changed.");

            // Do not reset passwords or restore permissions, even if configuration changed.
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        if (await users.FindByNameAsync(username) is not null || await users.FindByEmailAsync(email) is not null)
            throw new DatabaseInitializationException("The configured username or email belongs to an existing account. Refusing to adopt or promote it.");

        var password = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(password) || password.Length is < 16 or > 128)
            throw new DatabaseInitializationException("Configure BootstrapAdmin:Password with a unique production password of 16 to 128 characters that meets Identity password rules.");

        var role = await roles.FindByNameAsync(AdminRoles.SuperAdmin);
        if (role is null)
        {
            role = new IdentityRole(AdminRoles.SuperAdmin);
            RequireSuccess(await roles.CreateAsync(role));
        }
        if (await db.MunicipalityAdminGroups.AnyAsync(g => g.RoleId == role.Id, cancellationToken))
            throw new DatabaseInitializationException("The Super Admin role has a municipality mapping. Refusing to change existing permissions.");

        var user = new ApplicationUser
        {
            UserName = username, Email = email, MunicipalityId = null, LockoutEnabled = true
        };
        RequireSuccess(await users.CreateAsync(user, password));
        RequireSuccess(await users.AddToRoleAsync(user, AdminRoles.SuperAdmin));
        RequireSuccess(await users.AddClaimAsync(user, new Claim(OwnershipClaimType, user.Id)));

        if ((await access.GetAccessForUserAsync(user.Id))?.IsSuperAdmin != true)
            throw new DatabaseInitializationException("Administrator authorization verification failed. Account creation was rolled back.");

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static void RequireSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new DatabaseInitializationException("Identity rejected administrator provisioning. Check the configuration and Identity requirements. No accounts were changed.");
    }
}
