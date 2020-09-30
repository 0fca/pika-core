using System.ComponentModel.DataAnnotations;
using AspNetCore.CustomValidation.Attributes;

namespace PikaCore.Areas.Core.Models.AccountViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(100, ErrorMessage = "Username must be at least {2} and not longer than {0}", MinimumLength = 4)]
        [CompareTo("Email", ComparisonType.NotEqual, ErrorMessage = "Username cannot be the same as email.")]
        [RegularExpression(@"[A-Za-zÀ-ú0-9ა-ჰ一-蠼赋]+", 
            ErrorMessage = "You cannot use any special character inside your nickname")]
        public string Username { get; set; } = "";

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.",
            MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
