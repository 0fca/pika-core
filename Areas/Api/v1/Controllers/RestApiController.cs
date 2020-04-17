using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PikaCore.Areas.Api.v1.Controllers
{
    [ApiController]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [Area("Api")]
    [Route("/{area}/v1")]
    public class RestApiController : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("[action]")]
        public IActionResult Index()
        {
            return Ok("Welcome to PikaCore REST API.");
        }
    }
}