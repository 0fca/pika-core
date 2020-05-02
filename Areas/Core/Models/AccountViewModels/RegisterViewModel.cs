using System.ComponentModel.DataAnnotations;
using AspNetCore.CustomValidation.Attributes;

namespace PikaCore.Areas.Core.Models.AccountViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [StringLength(100, MinimumLength = 4, ErrorMessage = "Your username cannot be longer than {2} and must have at least {1}")]
        [Display(Name = "Username")]
        [RegularExpression("^[a-zA-Z0-9]+$", ErrorMessage = "The username must only contain alphanumeric characters")]
        public string Username { get; set; }
        
        [Required]
        [EmailAddress(ErrorMessage = "This is not a correctly formed email address")]
        [Display(Name = "Email")]
        [CompareTo("Username", ComparisonType.NotEqual, ErrorMessage = "Your username cannot be the same as an email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 10)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
