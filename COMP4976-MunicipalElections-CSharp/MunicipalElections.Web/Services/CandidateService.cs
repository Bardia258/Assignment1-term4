using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MunicipalElections.Web.Data;
using MunicipalElections.Web.ViewModels;

namespace MunicipalElections.Web.Services
{
    public class CandidateService : ICandidateService
    {
        private readonly ApplicationDbContext _context;

        public CandidateService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CandidateViewModel>> GetCandidatesByMunicipalityAsync(int municipalityId)
        {
            return await _context.Candidates
                .Where(c => c.MunicipalityId == municipalityId)
                .Select(c => new CandidateViewModel
                {
                    Id = c.CandidateId,
                    Name = c.Name,
                    Profile = c.Profile,
                    PhotoUrl = c.PhotoFileName,
                    Website = c.Website,
                    Email = c.Email,
                    PositionId = c.PositionId,
                    MunicipalityId = c.MunicipalityId
                }).ToListAsync();
        }

        public async Task<CandidateViewModel?> GetCandidateByIdAsync(int id)
{
    return await _context.Candidates
        .Where(c => c.CandidateId == id)
        .Select(c => new CandidateViewModel
        {
            Id = c.CandidateId,
            Name = c.Name,
            Profile = c.Profile,
            PhotoUrl = c.PhotoFileName,
            Website = c.Website,
            Email = c.Email,
            PositionId = c.PositionId,
            // Fetch the Position Name from the Positions table
            PositionTitle = _context.Positions.Where(p => p.PositionId == c.PositionId).Select(p => p.Name).FirstOrDefault() ?? "Candidate",
            MunicipalityId = c.MunicipalityId,
            // Fetch the Municipality Name from the Municipalities table
            MunicipalityName = _context.Municipalities.Where(m => m.MunicipalityId == c.MunicipalityId).Select(m => m.Name).FirstOrDefault() ?? "Unknown"
        }).FirstOrDefaultAsync();
}

        // NEW: Implement the search and filter method
        public async Task<IEnumerable<CandidateViewModel>> SearchCandidatesAsync(string searchString, int? municipalityId, int? positionId)
        {
            // Start with all candidates
            var query = _context.Candidates.AsQueryable();

            // Apply filters if they are provided
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.Name.Contains(searchString));
            }
            if (municipalityId.HasValue)
            {
                query = query.Where(c => c.MunicipalityId == municipalityId.Value);
            }
            if (positionId.HasValue)
            {
                query = query.Where(c => c.PositionId == positionId.Value);
            }
            
            return await query.Select(c => new CandidateViewModel
            {
                Id = c.CandidateId,
                Name = c.Name,
                Profile = c.Profile,
                PhotoUrl = c.PhotoFileName,
                Website = c.Website,
                Email = c.Email,
                PositionId = c.PositionId,
                PositionTitle = _context.Positions.Where(p => p.PositionId == c.PositionId).Select(p => p.Name).FirstOrDefault(),
                MunicipalityId = c.MunicipalityId,
                MunicipalityName = _context.Municipalities.Where(m => m.MunicipalityId == c.MunicipalityId).Select(m => m.Name).FirstOrDefault()
            }).ToListAsync();
        }
    }
}