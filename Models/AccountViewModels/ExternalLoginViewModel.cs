using System.ComponentModel.DataAnnotations;

namespace FMS2.Models.AccountViewModels
{
    public class ExternalLoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public bool WasRegistered { get; set; }
    }
}
