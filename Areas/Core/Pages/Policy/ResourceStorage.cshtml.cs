using System;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog;

namespace PikaCore.Areas.Core.Pages.Policy
{
    public class ResourceStorage : PageModel
    {
        public string DataPolicyMarkdown = "### Couldn't load statement.";
        
        public void OnGet()
        {
            try
            {
                DataPolicyMarkdown = System.IO.File.ReadAllText("wwwroot/files/data_policy.md");
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
            }
        }
    }
}