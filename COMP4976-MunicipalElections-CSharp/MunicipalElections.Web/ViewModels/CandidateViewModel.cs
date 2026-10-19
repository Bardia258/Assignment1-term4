namespace MunicipalElections.Web.ViewModels
{
    public class CandidateViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;
        public string PositionTitle { get; set; } = string.Empty;
        public int MunicipalityId { get; set; }
        public string MunicipalityName { get; set; } = string.Empty;
        public string Profile { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int PositionId { get; set; }
    }
}