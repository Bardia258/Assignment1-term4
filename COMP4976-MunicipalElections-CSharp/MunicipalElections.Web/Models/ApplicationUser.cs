using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace MunicipalElections.Web.Models;

public class ApplicationUser : IdentityUser
{
    [StringLength(200)]
    public string? FullName { get; set; }

    public int? MunicipalityId { get; set; }
    public Municipality? Municipality { get; set; }

    public ICollection<SavedCandidate> SavedCandidates { get; set; } = new List<SavedCandidate>();
}
