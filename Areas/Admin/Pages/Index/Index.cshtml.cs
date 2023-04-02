using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PikaCore.Areas.Admin.Pages.Index;

public class Index : PageModel
{
    public IActionResult OnGet()
    {
        return Page();
    }
}