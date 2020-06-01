using System.ComponentModel.DataAnnotations;
using AspNetCore.CustomValidation.Attributes;

namespace PikaCore.Areas.Core.Models.AccountViewModels
{
    public class ExternalLoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Username must be at least {2} and not longer than {0}", MinimumLength = 4)]
        [CompareTo("Email", ComparisonType.NotEqual, ErrorMessage = "Username cannot be the same as email.")]
        public string Username { get; set; }

        public bool WasRegistered { get; set; }
    }
}