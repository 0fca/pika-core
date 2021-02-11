using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace PikaCore.Areas.Identity.Models.ManageViewModels
{
    public class IndexViewModel
    {
        [Required(ErrorMessage = "This field is required")]
        [Display(Name = "Username")]
        [StringLength(maximumLength: 100, ErrorMessage = "Username cannot be longer than {0}, but longer than {1}", MinimumLength = 5)]
        public string Username { get; set; }

        public bool IsEmailConfirmed { get; set; }
        
        [AllowNull]
        [EmailAddress(ErrorMessage = "This is not a valid email address")]
        public string Email { get; set; }

        [AllowNull]
        [Phone(ErrorMessage = "This is not a valid phone number")]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }
    }
}
