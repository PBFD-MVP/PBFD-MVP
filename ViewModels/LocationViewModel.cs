namespace VisitorLog_PBFD.ViewModels
{
    public class LocationViewModel
    {
        public int ChildId { get; set; }              // Child Id of the location table
        public int ChildLocationId { get; set; }        //global Id of the location table
        public required string ChildNode { get; set; }        // Name of the child to be displayed
        public int ParentId { get; set; } // The ParentId mapped to the current table
        public required string ParentNode { get; set; }      // The name of the current table for grouping
        public string? NameTypeName { get; set; }       // The name of the NameType
        public bool IsSelected { get; set; }
        public int PersonId { get; set; } // The PersonId mapped to this location
    }
}
