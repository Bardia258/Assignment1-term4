using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MunicipalElections.Web.Data;
using MunicipalElections.Web.ViewModels;

namespace MunicipalElections.Web.Services
{
    public class MunicipalityService : IMunicipalityService
    {
        private readonly ApplicationDbContext _context;

        public MunicipalityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MunicipalityViewModel>> GetAllMunicipalitiesAsync()
        {
            // This queries the real database table named "Municipalities"
            return await _context.Municipalities.Select(m => new MunicipalityViewModel
            {
                Id = m.MunicipalityId, // Check the Models folder if his ID is named differently
                Name = m.Name,
                Province = m.Province,
                ElectionDate = m.ElectionDate,
                Description = m.Description,
                LogoUrl = m.LogoFileName // Change to LogoUrl if his model has that
            }).ToListAsync();
        }

        public async Task<MunicipalityViewModel?> GetMunicipalityByIdAsync(int id)
        {
            var m = await _context.Municipalities.FindAsync(id);
            if (m == null) return null;
            
            return new MunicipalityViewModel
            {
                Id = m.MunicipalityId,
                Name = m.Name,
                Province = m.Province,
                ElectionDate = m.ElectionDate,
                Description = m.Description,
                LogoUrl = m.LogoFileName
            };
        }

        public async Task<IEnumerable<PositionViewModel>> GetAllPositionsAsync()
        {
            return await _context.Positions.Select(p => new PositionViewModel
            {
                Id = p.PositionId,
                Name = p.Name,
                SeatsaVAILABLE = p.SeatsAvailable
            }).ToListAsync();
        }

        
    }
}  