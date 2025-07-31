namespace VisitorLog_PBFD.Models
{
    public class BaseEntity
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int NameTypeId { get; set; } // Foreign Key
        public NameType? NameType { get; set; } // Navigation property
    }
}
