using System.ComponentModel.DataAnnotations;

namespace MunicipalElections.Web.Models;

public class Position
{
    public int PositionId { get; set; }

    [ConcurrencyCheck]
    public string Revision { get; set; } = Guid.NewGuid().ToString("N");

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int SeatsAvailable { get; set; }

    public int MunicipalityId { get; set; }
    public Municipality Municipality { get; set; } = null!;

    [ScaffoldColumn(false), StringLength(100)]
    public string? SeedKey { get; set; }

    public ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();
}
