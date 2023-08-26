using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using PikaCore.Areas.Core.Models.File;
using PikaCore.Areas.Core.Queries;
using PikaCore.Areas.Identity.Attributes;
using Serilog;

namespace PikaCore.Areas.Core.Controllers.Hubs
{
    public class FileOperationHub : Hub
    {
        private readonly IConfiguration _configuration;
        private readonly IMediator _mediator;

        public FileOperationHub(
            IConfiguration configuration,
            IMediator mediator
        )
        {
            _configuration = configuration;
            _mediator = mediator;
        }

        public async Task List(string search, string categoryId, string buckedId)
        {
            if (string.IsNullOrEmpty(search))
            {
                await this.Clients.User(this.Context.UserIdentifier).SendAsync("ReceiveListing", new
                {
                    status = false,
                    listing = new List<ObjectInfo>()
                });
            }

            var listing = await _mediator.Send(
                new FindAllObjectsByNameQuery(search, categoryId, buckedId)
            );
            await this.Clients.User(this.Context.UserIdentifier).SendAsync("ReceiveListing", new
            {
                status = true,
                listing
            });
        }
    }
}