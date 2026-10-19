using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace MunicipalElections.Web.Authorization;

public static class AdminPolicies
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireSuperAdmin = "RequireSuperAdmin";
    public const string ManageMunicipality = "ManageMunicipality";

    public static IServiceCollection AddAdminAuthorization(this IServiceCollection services)
    {
        services.AddScoped<AdminAccessService>();
        services.AddScoped<IAuthorizationHandler, AdminAuthorizationHandler>();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(RequireAdmin, policy => policy.RequireAuthenticatedUser()
                .AddRequirements(new AdminRequirement(AdminPermission.Administration)));
            options.AddPolicy(RequireSuperAdmin, policy => policy.RequireAuthenticatedUser()
                .AddRequirements(new AdminRequirement(AdminPermission.SystemWide)));
            options.AddPolicy(ManageMunicipality, policy => policy.RequireAuthenticatedUser()
                .AddRequirements(new AdminRequirement(AdminPermission.Municipality)));
        });
        return services;
    }
}

public enum AdminPermission { Administration, SystemWide, Municipality }
public sealed record AdminRequirement(AdminPermission Permission) : IAuthorizationRequirement;

public sealed class AdminAuthorizationHandler(AdminAccessService accessService) : AuthorizationHandler<AdminRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        var access = await accessService.GetAccessAsync(context.User);
        if (access is null) return;
        var allowed = requirement.Permission switch
        {
            AdminPermission.Administration => true,
            AdminPermission.SystemWide => access.IsSuperAdmin,
            AdminPermission.Municipality => context.Resource is int municipalityId
                && await accessService.CanManageMunicipalityAsync(context.User, municipalityId),
            _ => false
        };
        if (allowed) context.Succeed(requirement);
    }
}

// Protect all current and future MVC controllers in the Admin area by default.
// Only the two login actions opt out using AllowAnonymous.
public sealed class AdminAreaAuthorizationConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        if (controller.Attributes.OfType<AreaAttribute>().Any(a => a.RouteValue == "Admin"))
            controller.Filters.Add(new AuthorizeFilter(AdminPolicies.RequireAdmin));
    }
}
