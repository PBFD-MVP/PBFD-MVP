using System.ComponentModel.DataAnnotations;

namespace VisitorLog_PBFD.Models
{
    public class SchemaColumn
    {
        [Key]
        public required string COLUMN_NAME { get; set; }
    }
}
