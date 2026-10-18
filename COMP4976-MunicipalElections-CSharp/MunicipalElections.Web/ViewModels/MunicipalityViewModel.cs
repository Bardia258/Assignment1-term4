using System;

namespace MunicipalElections.Web.ViewModels
{
    public class MunicipalityViewModel
    {
        public int Id { get; set; }
        public string Name {get; set;} = string.Empty;
        public string Province { get; set; } = string.Empty;
        public DateTime ElectionDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;

    }
}