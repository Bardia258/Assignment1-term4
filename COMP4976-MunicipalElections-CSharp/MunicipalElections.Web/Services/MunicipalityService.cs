using Microsoft.EntityFrameworkCore;
using MunicipalElections.Web.Data;
using MunicipalElections.Web.Models;
using MunicipalElections.Web.ViewModels;

namespace MunicipalElections.Web.Services;

public class MunicipalityService(ApplicationDbContext context, IImageStorage images) : IMunicipalityService
{
    private MunicipalityViewModel Map(Municipality m) => new()
    {
        Id = m.MunicipalityId, Name = m.Name, Province = m.Province, ElectionDate = m.ElectionDate,
        Description = m.Description, LogoUrl = images.Url(m.LogoFileName, "logos") ?? string.Empty
    };
    public async Task<IEnumerable<MunicipalityViewModel>> GetAllMunicipalitiesAsync() =>
        (await context.Municipalities.AsNoTracking().OrderBy(m => m.Name).ToListAsync()).Select(Map);
    public async Task<MunicipalityViewModel?> GetMunicipalityByIdAsync(int id)
    {
        var municipality = await context.Municipalities.AsNoTracking().SingleOrDefaultAsync(m => m.MunicipalityId == id);
        return municipality is null ? null : Map(municipality);
    }
    public async Task<IEnumerable<PositionViewModel>> GetAllPositionsAsync() =>
        await context.Positions.AsNoTracking().OrderBy(p => p.Name).Select(p => new PositionViewModel
        {
            Id = p.PositionId, Name = p.Name, SeatsaVAILABLE = p.SeatsAvailable
        }).ToListAsync();
}
