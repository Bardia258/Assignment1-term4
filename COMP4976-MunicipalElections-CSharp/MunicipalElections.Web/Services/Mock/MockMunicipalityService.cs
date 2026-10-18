using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MunicipalElections.Web.ViewModels;

namespace MunicipalElections.Web.Services.Mock
{
    public class MockMunicipalityService : IMunicipalityService
    {
        private readonly List<MunicipalityViewModel> _municipalities = new()
        {
            new MunicipalityViewModel { Id = 1, Name = "Pitt Meadows", Province = "BC", ElectionDate = new DateTime(2026, 10, 17), Description = "The City of Pitt Meadows." },
            new MunicipalityViewModel { Id = 2, Name = "Maple Ridge", Province = "BC", ElectionDate = new DateTime(2026, 10, 17), Description = "The City of Maple Ridge." }
        };

        public Task<IEnumerable<MunicipalityViewModel>> GetAllMunicipalitiesAsync()
        {
            return Task.FromResult<IEnumerable<MunicipalityViewModel>>(_municipalities);
        }

        public Task<MunicipalityViewModel> GetMunicipalityByIdAsync(int id)
        {
            var muni = _municipalities.Find(m => m.Id == id);
            return Task.FromResult(muni);
        }
    }
}