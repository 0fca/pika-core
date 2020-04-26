using System;
using System.IO;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog;

namespace PikaCore.Areas.Core.Pages.Home
{
    public class Policy : PageModel
    {
        public string PrivacyPolicyMarkdown = "### Couldn't load statement.";
        public string DataPolicyMarkdown = "### Couldn't load statement.";
        
        public void OnGet()
        {
            try
            {
                PrivacyPolicyMarkdown = System.IO.File.ReadAllText("wwwroot/files/privacy_policy.md");
                DataPolicyMarkdown = System.IO.File.ReadAllText("wwwroot/files/data_policy.md");
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
            }
        }
    }
}