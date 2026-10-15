using System.ComponentModel.DataAnnotations;

namespace MunicipalElections.Web.Models;

public class SavedCandidate
{
    public int SavedCandidateId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public int CandidateId { get; set; }
    public DateTime DateSaved { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public Candidate Candidate { get; set; } = null!;
}
