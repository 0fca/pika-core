using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using AspNetCore.CustomValidation.Attributes;

namespace PikaCore.Areas.Core.Models.AccountViewModels
{
    public class DeleteAccountViewModel
    {
        [StringLength(maximumLength: 12, MinimumLength = 4)]
        public string ConfirmationCode { get; set; }
        
        [DisplayName("Confirmation code")]
        [StringLength(maximumLength: 12, MinimumLength = 4)]
        [CompareTo("ConfirmationCode", ComparisonType.Equal, ErrorMessage = "Confirmation code doesn't match")]
        public string ConfirmationCodeReply { get; set; }
    }
}