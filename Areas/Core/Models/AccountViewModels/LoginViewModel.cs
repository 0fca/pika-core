using System.ComponentModel.DataAnnotations;

namespace PikaCore.Areas.Core.Models.AccountViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Please, enter a nickname")]
        [Display(Name = "Username")]
        [StringLength(100, MinimumLength = 5)]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Please, enter a valid password")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        [Display(Name = "Remember me?")] 
        public bool RememberMe { get; set; } = false;
    }
}
