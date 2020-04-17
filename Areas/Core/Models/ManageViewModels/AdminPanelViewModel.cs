using System.Collections.Generic;

namespace PikaCore.Models.ManageViewModels
{
    public class AdminPanelViewModel
    {
        public LogsListViewModel LogsListViewModel { get; set; }
        public Dictionary<ApplicationUser, IList<string>> UsersWithRoles { get; set; }
    }
}
