using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PikaCore.Areas.Api.v1.Models;
using PikaCore.Areas.Api.v1.Services;

namespace PikaCore.Areas.Api.v1.Controllers
{
    [ApiController]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [Area("Api")]
    [Route("/{area}/v1/users/[action]")]
    public class ApiAuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        
        public ApiAuthController(IAuthService authService)
        {
            _authService = authService;
        }
        
        [HttpPost]
        public async Task<IActionResult> Authenticate([FromBody] ApiUser user)
        {
           var token = await _authService.Authenticate(user.Username, user.Password);
           var apiMessage = new ApiMessage<string>();
           if (string.IsNullOrEmpty(token))
           {
               apiMessage.Status = false;
               apiMessage.Messages.Push("Authentication failed.");
               return BadRequest(apiMessage);
           }

           apiMessage.Data = token;
           apiMessage.Messages.Push("Authentication successful.");
           apiMessage.Status = true;
           return Ok(apiMessage);
        }

        [HttpPost]
        public async Task<IActionResult> SignOut()
        {
           var result= await _authService.SignOut();
           var apiMessage = new ApiMessage<string>();
           if (string.IsNullOrEmpty(result))
           {
               apiMessage.Status = false;
               apiMessage.Messages.Push("Couldn't sign out.");
               return BadRequest(apiMessage);
           }

           apiMessage.Status = true;
           apiMessage.Data = result;
           apiMessage.Messages.Push($"User {result} has been signed out.");
           return Ok(apiMessage);
        }
    }
}