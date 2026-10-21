namespace MunicipalElections.Web.Models;

public class SavedCandidate
{
    public int SavedCandidateId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public int CandidateId { get; set; }
    public Candidate Candidate { get; set; } = null!;
    public DateTime DateSaved { get; set; } = DateTime.UtcNow;
}
