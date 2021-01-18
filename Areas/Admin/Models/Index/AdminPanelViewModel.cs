using System.Collections.Generic;
using PikaCore.Areas.Core.Models;

namespace PikaCore.Areas.Admin.Models.Index
{
    public class AdminPanelViewModel
    {
        public MessageViewModel MessageViewModel { get; set; }
        public Dictionary<ApplicationUser, IList<string>> UsersWithRoles { get; set; }
    }
}
