using System.ComponentModel.DataAnnotations;
using TanvirArjel.CustomValidation.Attributes;

namespace PikaCore.Areas.Core.Models.AccountViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Please, enter a valid email address")]
        [EmailAddress(ErrorMessage = "Please, enter a valid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required]
        [Display(Name = "Username")]
        [StringLength(100, ErrorMessage = "Username must be at least {2} and not longer than {0}", MinimumLength = 5)]
        [CompareTo(nameof(Email), ComparisonType.NotEqual, ErrorMessage = "Username cannot be the same as email")]
        [RegularExpression(@"[A-Za-zÀ-ú0-9ა-ჰ一-蠼赋]+", 
            ErrorMessage = "You cannot use any special character inside your nickname")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "This field is required")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long",
            MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";
        
        [Required(ErrorMessage = "Confirmed password must not be empty")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match")]
        public string ConfirmPassword { get; set; } = "";
    }
}
