using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using PikaCore.Areas.Core.Models.File;
using PikaCore.Areas.Core.Queries;
using PikaCore.Infrastructure.Adapters;
using PikaCore.Infrastructure.Services;

namespace PikaCore.Areas.Core.Controllers.Hubs
{
    public class FileOperationHub : Hub
    {
        private readonly IConfiguration _configuration;
        private readonly IMediator _mediator;
        private readonly IStorage _storage;

        public FileOperationHub(
            IConfiguration configuration,
            IMediator mediator,
            IStorage storage
        )
        {
            _configuration = configuration;
            _mediator = mediator;
            _storage = storage;
        }
        
        public async Task List(string search, string categoryId, string buckedId)
        {
            var user = this.Context.GetHttpContext()?.User;
            if (string.IsNullOrEmpty(search) || user == null)
            {
                await this.Clients.Client(this.Context.ConnectionId).SendAsync("ReceiveListing", new
                {
                    status = false,
                    listing = new List<ObjectInfo>()
                });
                return;
            }

            if (!await _storage.UserHasBucketAccess(Guid.Parse(buckedId), user))
            {
                await this.Clients.Client(this.Context.ConnectionId).SendAsync("ReceiveListing", new
                {
                    status = false,
                    listing = new List<ObjectInfo>()
                });
                return; 
            }

            var listing = await _mediator.Send(
                new FindAllObjectsByNameQuery(search, categoryId, buckedId)
            );
            await this.Clients.Client(this.Context.ConnectionId).SendAsync("ReceiveListing", new
            {
                status = true,
                listing
            });
        }
    }
}