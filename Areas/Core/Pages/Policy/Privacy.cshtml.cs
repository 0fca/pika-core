using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PikaCore.Areas.Core.Pages.Policy
{
    public class Privacy : PageModel
    {
        public string PrivacyPolicyMarkdown = "### Couldn't load statement.";
        public void OnGet()
        {
            PrivacyPolicyMarkdown = System.IO.File.ReadAllText("wwwroot/files/privacy_policy.md");
        }
    }
}