using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using PikaCore.Areas.Core.Models.File;
using PikaCore.Areas.Identity.Attributes;

namespace PikaCore.Areas.Api.v1.Controllers;

[ApiController]
[Area("Api")]
public class StorageController : Controller
{
    private readonly IDistributedCache _cache;
    private readonly IMediator _mediator;

    public StorageController(IMediator mediator, IDistributedCache cache)
    {
        _mediator = mediator;
        _cache = cache;
    }

    [HttpGet]
    [Route("[area]/[controller]/[action]")]
    [AuthorizeUserBucketAccess]
    [AllowAnonymous]
    public async Task<IActionResult> Browse([FromQuery] string categoryId, [FromQuery] string bucketId,
        [FromQuery] string? tag = null)
    {
        var objects = JsonSerializer
            .Deserialize<List<ObjectInfo>>(
                await _cache.GetStringAsync($"{bucketId}.category.contents.{categoryId}") ?? "[]"
            );
        return Ok(new
        {
            Objects = objects
        });
    }
}