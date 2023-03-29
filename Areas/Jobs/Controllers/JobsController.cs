using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using PikaCore.Areas.Core.Callables;
using PikaCore.Areas.Jobs.Models;

namespace PikaCore.Areas.Jobs.Controllers;

[ApiController]
[Area("Jobs")]
[Route("/{area}/v1/jobs/[action]")]
public class JobsController : Controller
{
    private readonly IMediator _mediator;
    private readonly IDistributedCache _distributedCache;
    public JobsController(IMediator mediator,
        IDistributedCache distributedCache)
    {
        this._mediator = mediator;
        this._distributedCache = distributedCache;
    }
    
    [HttpPost]
    [ActionName("queue")]
    public IActionResult QueueRecurringJob([FromBody] QueuedJobViewModel queuedJobViewModel)
    {
        return Ok();
    }
}