using System.ComponentModel.DataAnnotations;
using TanvirArjel.CustomValidation.Attributes;

namespace PikaCore.Areas.Core.Models.AccountViewModels
{
    public class ExternalLoginViewModel
    {
        [Required(ErrorMessage = "Email address is required for external authentication")]
        [EmailAddress(ErrorMessage = "Please, enter a valid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Please, provide a username")]
        [StringLength(100, ErrorMessage = "Username must be at least {2} and not longer than {1}", MinimumLength = 4)]
        [CompareTo("Email", ComparisonType.NotEqual, ErrorMessage = "Username cannot be the same as email")]
        public string Username { get; set; }

        public bool WasRegistered { get; set; }
    }
}