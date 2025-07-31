namespace VisitorLog_PBFD.ViewModels
{
    public class ContinentViewModel
    {
        public int ContinentId { get; set; }              // ID of the continent
        public required string ContinentName { get; set; }        // Name of the continent
        public int PersonId { get; set; } // The PersonId mapped to this continent
        public int LocationId {  get; set; }   //LocationId mapped to this continent
        public required string NameTypeName { get; set; }      // Name of the NameType
        public bool IsSelected { get; set; } // Whether the continent is selected
    }
}
