using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PikaCore.Areas.Core.Pages.Admin.DTO;
using PikaCore.Areas.Infrastructure.Services;

namespace PikaCore.Areas.Core.Pages.Admin
{
    public class MessageEdit : PageModel
    {
        private readonly IMessageService _messageService;
        
        public MessageEdit(IMessageService messageService)
        {
            _messageService = messageService;
        }
        
        [BindProperty]
        public MessageDTO Message { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var message = await _messageService.GetMessageById(id);
            Message = message.ToMessageDto();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            
            await _messageService.UpdateMessage(Message.ToMessageEntity());
            return RedirectPermanent("/Admin/Index");
        }
    }
}