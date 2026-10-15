using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MunicipalElections.Web.Authorization;
using MunicipalElections.Web.Models;

namespace MunicipalElections.Web.Data;

public static class IdentitySeeder
{
    public const string SeedClaimType = "mems:development-account";

    public static async Task SeedAsync(IServiceProvider services, IHostEnvironment environment)
    {
        // Defense in depth: callers cannot accidentally enable classroom credentials in Production.
        if (!environment.IsDevelopment()) return;

        var db = services.GetRequiredService<ApplicationDbContext>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        await using var transaction = await db.Database.BeginTransactionAsync();

        foreach (var (roleName, municipalityKey) in AdminRoles.MunicipalityGroups)
        {
            if (!await db.Municipalities.AnyAsync(m => m.SeedKey == municipalityKey))
                throw new InvalidOperationException($"Cannot seed '{roleName}': its municipality is missing. Run domain seeding first.");
        }

        foreach (var role in AdminRoles.MunicipalityGroups.Keys.Prepend(AdminRoles.SuperAdmin))
        {
            if (!await roles.RoleExistsAsync(role))
                RequireSuccess(await roles.CreateAsync(new IdentityRole(role)), $"create role '{role}'");
        }

        var pittMeadowsId = await db.Municipalities.Where(m => m.SeedKey == "demo:pitt-meadows")
            .Select(m => m.MunicipalityId).SingleAsync();
        await SeedUserAsync("super", AdminRoles.SuperAdmin, null);
        await SeedUserAsync("pm", AdminRoles.PittMeadowsAdmin, pittMeadowsId);
        await transaction.CommitAsync();

        async Task SeedUserAsync(string username, string role, int? municipalityId)
        {
            // Ownership is recorded only by this seeder in the SAME transaction as creation.
            // A matching username/email is never enough to adopt an existing public account.
            var seededIds = await db.UserClaims.Where(c => c.ClaimType == SeedClaimType && c.ClaimValue == username)
                .Select(c => c.UserId).Distinct().ToListAsync();
            if (seededIds.Count > 1)
                throw new InvalidOperationException($"Development account seed conflict for '{username}': multiple ownership markers.");
            var existing = await users.FindByNameAsync(username);
            if (existing is not null && !seededIds.Contains(existing.Id))
                throw new InvalidOperationException($"Development account seed conflict: username '{username}' belongs to an unrecognized account. No privileges or account fields were changed. Resolve the conflict explicitly.");
            if (seededIds.Count == 1)
            {
                // Preserve renamed accounts, changed passwords, assignments, roles, and profile edits.
                if (!await db.Users.AnyAsync(u => u.Id == seededIds[0]))
                    throw new InvalidOperationException($"Development account seed conflict for '{username}': missing marked user.");
                return;
            }

            var email = $"development-{username}@example.com";
            if (await users.FindByEmailAsync(email) is not null)
                throw new InvalidOperationException($"Development account seed conflict for '{username}': sample email is already in use.");
            var user = new ApplicationUser
            {
                UserName = username, Email = email, EmailConfirmed = true,
                MunicipalityId = municipalityId, LockoutEnabled = true
            };
            // Fixed classroom credential, intentionally restricted to Development above.
            RequireSuccess(await users.CreateAsync(user, "P@$$w0rd"), $"create account '{username}'");
            RequireSuccess(await users.AddClaimAsync(user, new Claim(SeedClaimType, username)), $"mark account '{username}'");
            RequireSuccess(await users.AddToRoleAsync(user, role), $"assign role for '{username}'");
        }
    }

    private static void RequireSuccess(IdentityResult result, string operation)
    {
        // Error codes are useful without logging credentials, password hashes, or user input.
        if (!result.Succeeded)
            throw new InvalidOperationException($"Identity seeding could not {operation}: {string.Join(", ", result.Errors.Select(e => e.Code))}.");
    }
}
