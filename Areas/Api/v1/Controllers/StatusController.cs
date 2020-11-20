using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PikaCore.Areas.Api.v1.Models;
using PikaCore.Areas.Api.v1.Models.DTO;
using PikaCore.Areas.Api.v1.Services;
using PikaCore.Areas.Infrastructure.Data;
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
        private readonly IStatusService _statusService;
        private readonly ISystemService _systemService;
        
        public StatusController(IMessageService messageService,
                                IStatusService statusService,
                                ISystemService systemService
                                )
        {
            _messageService = messageService;
            _statusService = statusService;
            _systemService = systemService;
        }
        
        [HttpGet]
        [ActionName("index")]
        public async Task<IActionResult> Index()
        {
            var allStatuses = await _statusService.CheckAllSystems();
            var isAllOk = allStatuses.All(m => m.Value);
            var isAnyOk = allStatuses.Any(m => m.Value);
            var apiMsg = new ApiMessage<MessageDto> {Status = isAllOk};
            var overallStatusMessage = isAllOk 
                ? "System is in graceful state." 
                : (isAnyOk ? "System is degraded." : "All systems are down.");
            apiMsg.Messages.Push(overallStatusMessage);

            if (isAllOk) return Ok(apiMsg);
            
            var desertedSystems = 
                allStatuses.Where(s => !s.Value).ToList();
            desertedSystems.ForEach(n =>
            {
                apiMsg.Messages.Push($"{n.Key} is in downstate due to maintenance or a critical system fault.");
            });
            return Ok(apiMsg);
        }
        
        [HttpGet]
        [ActionName("{systemName}/messages")]
        [AllowAnonymous]
        public async Task<IActionResult> Messages(string systemName, int order = 0, int count = 10, int offset = 0)
        {
            try
            {
                var messages = await _messageService.GetAllMessagesForSystem(systemName);
                
                var messagesForSystem = messages.ToList();
                if (order == 1)
                {
                    messagesForSystem = messagesForSystem.OrderByDescending(m => m.Id).ToList();
                }
                _messageService.ApplyPaging(ref messagesForSystem, count, offset);
                var dtos = new List<MessageDto>();
                messagesForSystem.ForEach(m => dtos.Add(MessageDto.FromMessageEntity(m)));
                var apiMessage = new ApiMessage<IList<MessageDto>> {Data = dtos, Status = true};
                apiMessage.Messages.Push("Messages available for current role.");
                return Ok(apiMessage);
            }
            catch (Exception e)
            {
                var apiMessage = new ApiMessage<IList<MessageDto>> { Status = false};
                apiMessage.Messages.Push($"Bad request: {e.Message}");
                return BadRequest(apiMessage);
            }
        }
        
        [HttpGet]
        [ActionName("{systemName}/issues")]
        [AllowAnonymous]
        public async Task<IActionResult> Issues(string systemName, int order = 0, int count = 10, int offset = 0)
        {
            try
            {
                var messages = await _messageService.GetAllIssues(systemName);
                var dtos = new List<IssueEntity>();
                var messagesForSystem = messages.ToList();
                if (order == 1)
                {
                    messagesForSystem = messagesForSystem.OrderByDescending(m => m.Id).ToList();
                }
                _messageService.ApplyPaging(ref messagesForSystem, count, offset);
                messagesForSystem
                    .ToList()
                    .ForEach(m => dtos.Add(m));
                var apiMessage = new ApiMessage<IList<IssueEntity>> {Data = dtos, Status = true};
                apiMessage.Messages.Push("Issues available for current role.");
                return Ok(apiMessage);
            }
            catch (Exception e)
            {
                var apiMessage = new ApiMessage<string>{ Status = false};
                apiMessage.Messages.Push($"Bad request: {e.Message}");
                return BadRequest(apiMessage);
            }
        }
        
        [HttpGet]
        [ActionName("{systemName}/messages/{id}/issues")]
        [AllowAnonymous]
        public async Task<IActionResult> IssuesForMessage(int id, int order = 0, int count = 10, int offset = 0)
        {
            try
            {
                var issues = (await _messageService.GetMessageById(id)).RelatedIssues;
                var dtos = new List<IssueEntity>();
                var messagesForSystem = issues.ToList();
                if (order == 1)
                {
                    messagesForSystem = messagesForSystem.OrderByDescending(m => m.Id).ToList();
                }
                _messageService.ApplyPaging(ref messagesForSystem, count, offset);
                messagesForSystem
                    .ToList()
                    .ForEach(m => dtos.Add(m));
                var apiMessage = new ApiMessage<IList<IssueEntity>> {Data = dtos, Status = true};
                apiMessage.Messages.Push("Issues available for selected message id.");
                return Ok(apiMessage);
            }
            catch (Exception e)
            {
                var apiMessage = new ApiMessage<string>{ Status = false};
                apiMessage.Messages.Push($"Bad request: {e.Message}");
                return BadRequest(apiMessage);
            }
        }
        
        [HttpGet]
        [ActionName("{systemName}/messages/{from}/{to}")]
        public async Task<IActionResult> MessagesByDateCreated(string systemName, DateTime from, DateTime to, int order = 0, int count = 1)
        {
            if (from.Date > to.Date)
            {
                var apiMessage = new ApiMessage<IList<MessageDto>> { Status = false};
                apiMessage.Messages.Push($"Bad request: Date from cannot be later than to date.");
                return BadRequest(apiMessage);
            }

            try
            {
                var messages = await _messageService.GetAllMessagesForSystem(systemName);
                
                var messagesForSystem = messages.ToList();
                if (order == 1)
                {
                    messagesForSystem = messagesForSystem.OrderByDescending(m => m.UpdatedAt).ToList();
                }
                _messageService.ApplyPagingByDate(ref messagesForSystem, count, from, to);
                
                var dtos = new List<MessageDto>();
                messagesForSystem.ForEach(m => dtos.Add(MessageDto.FromMessageEntity(m)));
                var apiMessage = new ApiMessage<IList<MessageDto>> {Data = dtos, Status = true};
                apiMessage.Messages.Push("Messages available for current role in given date range.");
                return Ok(apiMessage);
            }
            catch (Exception e)
            {
                var apiMessage = new ApiMessage<IList<MessageDto>> { Status = false};
                apiMessage.Messages.Push($"Bad request: {e.Message}");
                return BadRequest(apiMessage);
            }
        }

        [HttpGet]
        [ActionName("messages/{id}")]
        public async Task<IActionResult> MessageById(int id)
        {
            var apiMessage = new ApiMessage<MessageDto>();
            try
            {
                apiMessage.Data = MessageDto.FromMessageEntity(await _messageService.GetMessageById(id));
                apiMessage.Status = true;
                apiMessage.Messages.Push($"Successfully returned a message of id {id}");
                return Ok(apiMessage);
            }
            catch
            {
                apiMessage.Messages.Push("The server couldn't find requested message.");
                apiMessage.Status = false;
                return StatusCode(StatusCodes.Status404NotFound, apiMessage);
            }
        }

        [HttpGet]
        [ActionName("systems")]
        public async Task<IActionResult> Systems()
        {
            var apiMessage = new ApiMessage<List<string>>();
            try
            {
                var systemNames = await _systemService.GetAllSystemNames();
                apiMessage.Data = systemNames;
                apiMessage.Status = true;
                apiMessage.Messages.Push("All public systems in PikaCloud.");
                return Ok(apiMessage);
            }
            catch (Exception e)
            {
                apiMessage.Status = false;
                apiMessage.Messages.Push(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, apiMessage);
            }
        }

        [HttpGet]
        [ActionName("systems/{systemName}/state")]
        public async Task<IActionResult> SystemTextState(string systemName)
        {
            var apiMessage = new ApiMessage<string>();
            try
            {
                var isUp = await _statusService.CheckSpecificSystem(
                    await _systemService.GetDescriptorByName(systemName)
                    );
                apiMessage.Data = isUp ? "Graceful" : "Down";
                apiMessage.Status = true;
                apiMessage.Messages.Push($"Text state for {systemName}");
                return Ok(apiMessage);
            }
            catch (Exception e)
            {
                apiMessage.Status = false;
                apiMessage.Messages.Push(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, apiMessage);
            }
        }
    }
}