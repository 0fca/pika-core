using System.ComponentModel.DataAnnotations;
using AspNetCore.CustomValidation.Attributes;

namespace PikaCore.Areas.Core.Models.AccountViewModels
{
    public class ExternalLoginViewModel
    {
        [Required]
        [EmailAddress]
        [StringLength(100, MinimumLength = 4, ErrorMessage = "Your username cannot be longer than {0} and must have at least {1}.")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 4, ErrorMessage = "Your username cannot be longer than {0} and must have at least {1}.")]
        [CompareTo("Email", ComparisonType.NotEqual, ErrorMessage = "Your username cannot be the same as an email")]
        [RegularExpression("^[a-zA-Z0-9]+$", ErrorMessage = "The username must only contain alphanumeric characters")]
        public string Username { get; set; }
    }
}