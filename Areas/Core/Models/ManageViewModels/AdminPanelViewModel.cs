using System.Collections.Generic;
using PikaCore.Areas.Core.Models.AdminViewModels;

namespace PikaCore.Areas.Core.Models.ManageViewModels
{
    public class AdminPanelViewModel
    {
        public MessageViewModel MessageViewModel { get; set; }
        public Dictionary<ApplicationUser, IList<string>> UsersWithRoles { get; set; }
    }
}
