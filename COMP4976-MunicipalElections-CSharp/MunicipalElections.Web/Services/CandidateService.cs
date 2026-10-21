using Microsoft.EntityFrameworkCore;
using MunicipalElections.Web.Data;
using MunicipalElections.Web.Models;
using MunicipalElections.Web.ViewModels;

namespace MunicipalElections.Web.Services;

public class CandidateService(ApplicationDbContext context, IImageStorage images) : ICandidateService
{
    private IQueryable<Candidate> Query => context.Candidates.AsNoTracking().Include(c => c.Position).Include(c => c.Municipality);
    private CandidateViewModel Map(Candidate c) => new()
    {
        Id = c.CandidateId, Name = c.Name, Profile = c.Profile, Bio = c.Profile,
        PhotoUrl = images.Url(c.PhotoFileName, "candidates") ?? string.Empty,
        Website = c.Website ?? string.Empty, Email = c.Email ?? string.Empty,
        PositionId = c.PositionId, PositionTitle = c.Position.Name,
        MunicipalityId = c.MunicipalityId, MunicipalityName = c.Municipality.Name,
        MunicipalityLogoUrl = images.Url(c.Municipality.LogoFileName, "logos") ?? string.Empty,
        MunicipalityElectionDate = c.Municipality.ElectionDate,
        DateCreated = c.DateCreated
    };
    public async Task<IEnumerable<CandidateViewModel>> GetCandidatesByMunicipalityAsync(int municipalityId) =>
        (await Query.Where(c => c.MunicipalityId == municipalityId).OrderBy(c => c.Name).ToListAsync()).Select(Map);
    public async Task<CandidateViewModel?> GetCandidateByIdAsync(int id)
    {
        var candidate = await Query.SingleOrDefaultAsync(c => c.CandidateId == id);
        return candidate is null ? null : Map(candidate);
    }
    public async Task<IEnumerable<CandidateViewModel>> SearchCandidatesAsync(string searchString, int? municipalityId, int? positionId)
    {
        var query = Query;
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var term = searchString.Trim().ToUpperInvariant();
            query = query.Where(c => c.Name.ToUpper().Contains(term));
        }
        if (municipalityId.HasValue) query = query.Where(c => c.MunicipalityId == municipalityId.Value);
        if (positionId.HasValue) query = query.Where(c => c.PositionId == positionId.Value);
        return (await query.OrderBy(c => c.Name).ToListAsync()).Select(Map);
    }
}
