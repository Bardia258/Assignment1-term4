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
                    Id = c.CandidateId, // Check his Models folder for exact property names
                    Name = c.Name,
                    PhotoUrl = c.PhotoFileName, 
                    PositionTitle = "Candidate", // You may need to join the Positions table if he has one
                    MunicipalityId = c.MunicipalityId,
                    MunicipalityName = "Municipality Name" // We can optimize this later
                }).ToListAsync();
        }

        public async Task<CandidateViewModel?> GetCandidateByIdAsync(int id)
        {
            var c = await _context.Candidates.FindAsync(id);
            if (c == null) return null;

            return new CandidateViewModel
            {
                Id = c.CandidateId,
                Name = c.Name,
                PhotoUrl = c.PhotoFileName,
                PositionTitle = "Candidate",
                MunicipalityId = c.MunicipalityId,
                MunicipalityName = "Municipality Name"
            };
        }
    }
}