using Microsoft.AspNetCore.Identity;

namespace MunicipalElections.Web.Models;

public class ApplicationUser : IdentityUser
{
    public int? MunicipalityId { get; set; }
    public Municipality? Municipality { get; set; }
}
