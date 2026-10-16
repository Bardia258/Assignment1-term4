using Microsoft.AspNetCore.Identity;

namespace MunicipalElections.Web.Models;

// One persisted Identity administrator role per municipality, independent of display names.
public class MunicipalityAdminGroup
{
    public int MunicipalityId { get; set; }
    public Municipality Municipality { get; set; } = null!;
    public string RoleId { get; set; } = string.Empty;
    public IdentityRole Role { get; set; } = null!;
}
