using System.ComponentModel.DataAnnotations;

namespace PikaCore.Areas.Core.Models.AccountViewModels
{
    public class ExternalLoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public bool WasRegistered { get; set; }
    }
}