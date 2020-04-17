using System.Collections.Generic;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PikaCore.Areas.Api.v1.Models;

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
        [Produces(MediaTypeNames.Application.Json)]
        [Route("[action]")]
        public IActionResult Index()
        {
            var data = new List<string> {"Welcome to PikaCore REST API."};
            var apiMessage = new ApiMessage<IList<string>>();
            apiMessage.SetData(data);
            apiMessage.AddMessage("Everythin is fine.");
            return Ok(apiMessage);
        }
        
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("[action]")]
        public IActionResult NotFoundHandler()
        {
            return NotFound();
        }
    }
}