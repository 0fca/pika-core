using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Pika.Domain.Status.Data;
using PikaCore.Infrastructure.Services;

namespace PikaCore.Areas.Core.Pages.Admin
{
    public class CreateSystem : PageModel
    {
        private readonly ISystemService _systemService;
        
        public CreateSystem(ISystemService systemService)
        {
            _systemService = systemService;
        }
        
        [BindProperty]
        public SystemDescriptor SystemDescriptor { get; set;  } = new SystemDescriptor();
        
        public async Task<IActionResult> OnGetAsync()
        {
            
            return Page();
        }
        
        public async Task<IActionResult> OnPostAsync()
        {
            
            return RedirectPermanent("/Admin/Index");
        }
    }
}