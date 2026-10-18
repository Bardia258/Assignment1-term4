using System.Collections.Generic;
using System.Threading.Tasks;
using MunicipalElections.Web.ViewModels;

namespace MunicipalElections.Web.Services.Mock
{
    public class MockCandidateService : ICandidateService
    {
        private readonly List<CandidateViewModel> _candidates = new()
        {
            new CandidateViewModel { Id = 1, Name = "John Doe", PositionTitle = "Mayor", MunicipalityId = 1, MunicipalityName = "Pitt Meadows", Bio = "Experienced leader." },
            new CandidateViewModel { Id = 2, Name = "Jane Smith", PositionTitle = "Councilor", MunicipalityId = 1, MunicipalityName = "Pitt Meadows", Bio = "Community advocate." },
            new CandidateViewModel { Id = 3, Name = "Bob Johnson", PositionTitle = "Mayor", MunicipalityId = 2, MunicipalityName = "Maple Ridge", Bio = "Fiscal responsibility." }
        };

        public Task<IEnumerable<CandidateViewModel>> GetCandidatesByMunicipalityAsync(int municipalityId)
        {
            var filtered = _candidates.FindAll(c => c.MunicipalityId == municipalityId);
            return Task.FromResult<IEnumerable<CandidateViewModel>>(filtered);
        }

        public Task<CandidateViewModel> GetCandidateByIdAsync(int id)
        {
            var cand = _candidates.Find(c => c.Id == id);
            return Task.FromResult(cand);
        }
    }
}