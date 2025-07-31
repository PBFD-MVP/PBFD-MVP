namespace VisitorLog_PBFD.ViewModels
{
    public class LocationMappingViewModel
    {
        public required string ParentNode { get; set; }
        public required string GrandparentNode { get; set; }
        public required string ChildNode { get; set; }
        public int ChildId { get; set; }
        public int ChildLocationId { get; set; }
        public int? NameTypeId { get; set; }
        public int ParentId { get; set; }

    }
}
