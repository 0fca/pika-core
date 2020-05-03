using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PikaCore.Areas.Api.v1.Data;
using PikaCore.Areas.Api.v1.Models;
using PikaCore.Areas.Api.v1.Services;
using PikaCore.Areas.Infrastructure.Services;

namespace PikaCore.Areas.Api.v1.Controllers
{
    [ApiController]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [Area("Api")]
    [Route("/{area}/v1/status/[action]")]
    public class StatusController : ControllerBase
    {
        private readonly IMessageService _messageService;
        
        public StatusController(IMessageService messageService)
        {
            _messageService = messageService;
        }
        
        [HttpGet]
        [ActionName("index")]
        public IActionResult Index()
        {
            var apiMsg = new ApiMessage<string> {Status = true};
            apiMsg.Messages.Push("Overall status ok.");
            return Ok(apiMsg);
        }
        
        [HttpGet]
        [ActionName("messages")]
        [AllowAnonymous]
        public async Task<IActionResult> Messages(int order = 0, int count = 10, int offset = 0)
        {
            try
            {
                var messages = await _messageService.GetAllMessages();
                _messageService.ApplyPaging(ref messages, count, offset);
                if (order == 1)
                {
                    messages = messages.OrderByDescending(m => m.Id).ToList();
                }

                var apiMessage = new ApiMessage<IList<MessageEntity>> {Data = messages, Status = true};
                apiMessage.Messages.Push("Messages available for current role.");
                return Ok(apiMessage);
            }
            catch (Exception e)
            {
                var apiMessage = new ApiMessage<IList<MessageEntity>> { Status = false};
                apiMessage.Messages.Push("The request couldn't be understood.");
                return BadRequest(apiMessage);
            }
        }
        
        [HttpGet]
        [ActionName("messages/filter")]
        public async Task<IActionResult> MessagesByDateCreated([FromQuery]DateTime from, [FromQuery]DateTime to, int order = 0)
        {
            return Ok("By date");
        }

        [HttpGet]
        [ActionName("messages/{id}")]
        public async Task<IActionResult> MessageById(int id)
        {
            var apiMessage = new ApiMessage<MessageEntity>();
            try
            {
                apiMessage.Data = await _messageService.GetMessageById(id);
                apiMessage.Status = true;
                apiMessage.Messages.Push($"Successfully returned a message of id {id}");
                return Ok(apiMessage);
            }
            catch (Exception e)
            {
                apiMessage.Messages.Push("The server couldn't find requested message.");
                apiMessage.Status = false;
                return StatusCode(404, apiMessage);
            }
        }
    }
}