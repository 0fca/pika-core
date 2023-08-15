using System.Net.Mime;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PikaCore.Infrastructure.Adapters.Console;
using PikaCore.Infrastructure.Adapters.Console.Queries;

namespace PikaCore.Areas.Api.v1.Controllers;

[ApiController]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
[Area("Api")]
[Route("{area}/v1/[controller]/[action]")]

public class CommandController : ControllerBase
{
    private readonly CloudConsoleAdapter _cloudConsoleAdapter;
    private readonly IMediator _mediator;
    
    public CommandController(CloudConsoleAdapter cloudConsoleAdapter,
        IMediator mediator)
    {
        this._cloudConsoleAdapter = cloudConsoleAdapter;
        this._mediator = mediator;
    }

    [HttpPost]
    [ActionName("{command}")]  
    public async Task<IActionResult> Execute(string command, [FromQuery] string body)
    {
        // FIXME: Validation of command and body
        var commandsView = await _mediator.Send(new FindCommandByNameQuery(command.Trim()));
        if (!string.IsNullOrEmpty(body))
        {
            commandsView.Body = body;
        }
        var output = _cloudConsoleAdapter.ExecuteCommand(commandsView);
        return Ok(new
        {
            output
        });
    }
}