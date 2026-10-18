using System.Collections.Generic;
using System.Threading.Tasks;
using MunicipalElections.Web.ViewModels;

namespace MunicipalElections.Web.Services
{
    public interface ICandidateService
    {
        Task<IEnumerable<CandidateViewModel>> GetCandidatesByMunicipalityAsync(int municipalityId);
        Task<CandidateViewModel> GetCandidateByIdAsync(int id);
    }
}