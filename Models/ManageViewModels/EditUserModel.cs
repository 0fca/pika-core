using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FMS2.Models.ManageViewModels
{
    public class EditUserModel
    {
        [Required]
        public string Id { get; set; }
        [Required]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
        [DataType(DataType.PhoneNumber)]
        public string Phone { get; set; }
        [DataType(DataType.Password)]
        public string NewPasswd { get; set; }
        public string UserName { get; set; }
        public IList<string> Roles { get; set; }
    }
}
