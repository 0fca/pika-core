using System.ComponentModel.DataAnnotations;

namespace PikaCore.Areas.Core.Models.AccountViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
