using System.ComponentModel.DataAnnotations;

namespace VisitorLog_PBFD.Models
{
    public class Person
    {
        [Required]
        public int PersonId { get; set; }

        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Middle Name")]
        public string? MiddleName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [Display(Name = "Email Address")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string? Email { get; set; }
    }

}
