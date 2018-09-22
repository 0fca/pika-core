using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FMS2.Models.ManageViewModels
{
    public class AdminPanelViewModel
    {
        public LogsListViewModel LogsListViewModel { get; set; }
        public Dictionary<ApplicationUser, IList<string>> UsersWithRoles { get; set; }
    }
}
