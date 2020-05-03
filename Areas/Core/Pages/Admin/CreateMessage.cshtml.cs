using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PikaCore.Areas.Core.Pages.Admin.DTO;
using PikaCore.Areas.Infrastructure.Services;

namespace PikaCore.Areas.Core.Pages.Admin
{
    public class CreateMessage : PageModel
    {
        private readonly IMessageService _messageService;
        
        public CreateMessage(IMessageService messageService)
        {
            _messageService = messageService;
        }
        
        [BindProperty]
        public MessageDTO Message { get; set; } = new MessageDTO();

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await _messageService.CreateMessage(Message.ToMessageEntity());
            return RedirectPermanent("/Admin/Index");
        }
    }
}