using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PikaCore.Areas.Admin.Pages.Index.DTO;
using PikaCore.Infrastructure.Services;

namespace PikaCore.Areas.Admin.Pages.Index;

public class CreateMessage : PageModel
{
    private readonly IMessageService _messageService;
    private readonly ISystemService _systemService;

    public CreateMessage(IMessageService messageService,
        ISystemService systemService)
    {
        _messageService = messageService;
        _systemService = systemService;
    }

    [BindProperty] public MessageDTO Message { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        Message.Systems = await _systemService.GetAll();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var e = Message.ToMessageEntity();
        e.SystemDescriptor = await _systemService.GetDescriptorByName(Message.SystemName);
        await _messageService.CreateMessage(e);
        return RedirectPermanent("/Admin/Index");
    }
}