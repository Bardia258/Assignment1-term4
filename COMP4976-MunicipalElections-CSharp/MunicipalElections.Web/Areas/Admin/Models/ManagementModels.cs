using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using MunicipalElections.Web.Authorization;

namespace MunicipalElections.Web.Areas.Admin.Models;

public class MunicipalityInput : IValidatableObject
{
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Province { get; set; } = string.Empty;
    [Required, DataType(DataType.Date)] public DateTime? ElectionDate { get; set; }
    [StringLength(2000)] public string? Description { get; set; }
    [Display(Name = "Logo")] public IFormFile? Image { get; set; }
    public string? Revision { get; set; }
    [BindNever, ValidateNever] public string? CurrentImage { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (ElectionDate.HasValue && (ElectionDate.Value.Year < 2000 || ElectionDate.Value.Year > 2100))
            yield return new ValidationResult("Choose an election date between 2000 and 2100.", new[] { nameof(ElectionDate) });
    }
}

public class PositionInput
{
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Range(1, int.MaxValue), Display(Name = "Available seats")] public int SeatsAvailable { get; set; } = 1;
    [Range(1, int.MaxValue), Display(Name = "Municipality")] public int MunicipalityId { get; set; }
    public string? Revision { get; set; }
    [BindNever, ValidateNever] public List<SelectListItem> Municipalities { get; set; } = new();
}

public class CandidateInput
{
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(4000), Display(Name = "Profile / biography")] public string Profile { get; set; } = string.Empty;
    [Range(1, int.MaxValue), Display(Name = "Municipality")] public int MunicipalityId { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Position")] public int PositionId { get; set; }
    [Url, StringLength(2048)] public string? Website { get; set; }
    [EmailAddress, StringLength(254)] public string? Email { get; set; }
    [Display(Name = "Photograph")] public IFormFile? Image { get; set; }
    public string? Revision { get; set; }
    [BindNever, ValidateNever] public string? CurrentImage { get; set; }
    [BindNever, ValidateNever] public List<SelectListItem> Municipalities { get; set; } = new();
    [BindNever, ValidateNever] public List<SelectListItem> Positions { get; set; } = new();
}

public sealed record DashboardModel(AdminAccess Access, int MunicipalityCount, int PositionCount, int CandidateCount);
