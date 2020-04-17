using System.Collections.Generic;
using PikaCore.Models;

namespace PikaCore.Areas.Core.Models.ManageViewModels
{
    public class AdminPanelViewModel
    {
        public LogsListViewModel LogsListViewModel { get; set; }
        public Dictionary<ApplicationUser, IList<string>> UsersWithRoles { get; set; }
    }
}
