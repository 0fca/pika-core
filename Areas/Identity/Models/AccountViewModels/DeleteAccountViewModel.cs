using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using TanvirArjel.CustomValidation.Attributes;

namespace PikaCore.Areas.Identity.Models.AccountViewModels
{
    public class DeleteAccountViewModel
    {
        [Required(ErrorMessage = "This field is required")]
        [Display(Name = "Confirmation Code")]
        [StringLength(maximumLength: 12, MinimumLength = 4)]
        public string ConfirmationCode { get; set; }
        
        [DisplayName("Repeat Confirmation Code")]
        [StringLength(maximumLength: 12, MinimumLength = 4)]
        [CompareTo("ConfirmationCode", ComparisonType.Equal, ErrorMessage = "Confirmation code doesn't match")]
        public string ConfirmationCodeReply { get; set; }
    }
}