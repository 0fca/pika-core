using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PikaCore.Areas.Admin.Pages.Index.DTO;
using PikaCore.Infrastructure.Services;

namespace PikaCore.Areas.Admin.Pages.Index;

public class MessageEdit : PageModel
{
    private readonly IMessageService _messageService;
    private readonly ISystemService _systemService;

    public MessageEdit(IMessageService messageService,
        ISystemService systemService)
    {
        _messageService = messageService;
        _systemService = systemService;
    }

    [BindProperty] public MessageDTO Message { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var message = await _messageService.GetMessageById(id);
        Message = MessageDTO.FromMessageEntity(message);
        Message.Systems = await _systemService.GetAll();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var e = Message.ToMessageEntity();
        e.SystemDescriptor = await _systemService.GetDescriptorByName(Message.SystemName);
        await _messageService.UpdateMessage(e);
        return RedirectPermanent("/Admin/Index");
    }
}