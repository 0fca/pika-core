using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PikaCore.Areas.Identity.Models.AccountViewModels
{
    public class ExportDataViewModel
    {
        public List<SelectListItem> DataCollections = new List<SelectListItem>();
        public List<string> SelectedCollections = new List<string>();
    }
}