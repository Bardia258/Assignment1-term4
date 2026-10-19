using System.ComponentModel.DataAnnotations;

namespace MunicipalElections.Web.Models;

public class Candidate
{
    public int CandidateId { get; set; }

    [ConcurrencyCheck]
    public string Revision { get; set; } = Guid.NewGuid().ToString("N");

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    public string Profile { get; set; } = string.Empty;

    [StringLength(255)]
    public string? PhotoFileName { get; set; }

    [Url, StringLength(2048)]
    public string? Website { get; set; }

    [EmailAddress, StringLength(254)]
    public string? Email { get; set; }

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    public int PositionId { get; set; }
    public int MunicipalityId { get; set; }
    public Position Position { get; set; } = null!;
    public Municipality Municipality { get; set; } = null!;

    [ScaffoldColumn(false), StringLength(100)]
    public string? SeedKey { get; set; }
}
