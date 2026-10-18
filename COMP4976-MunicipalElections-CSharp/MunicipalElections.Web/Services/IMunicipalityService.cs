using System.Collections.Generic;
using System.Threading.Tasks;
using MunicipalElections.Web.ViewModels;

namespace MunicipalElections.Web.Services
{
    public interface IMunicipalityService
    {
        Task<IEnumerable<MunicipalityViewModel>> GetAllMunicipalitiesAsync();
        Task<MunicipalityViewModel> GetMunicipalityByIdAsync(int id);
    }
}