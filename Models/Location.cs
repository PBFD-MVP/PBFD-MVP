namespace VisitorLog_PBFD.Models
{
    public class Location
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int? NameTypeId { get; set; }
        public required string Type { get; set; }
        public int? ParentId { get; set; } // Allow NULL for top-level entries like continents
        public required int ChildId { get; set; }
        public required int Level { get; set; }
    }
}
