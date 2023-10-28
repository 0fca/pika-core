using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using PikaCore.Areas.Core.Models.File;
using PikaCore.Areas.Core.Queries;
using PikaCore.Infrastructure.Adapters;
using PikaCore.Infrastructure.Security;
using Serilog;

namespace PikaCore.Areas.Core.Controllers.Hubs
{
    public class FileOperationHub : Hub
    {
        private readonly IMediator _mediator;
        private readonly IStorage _storage;
        private readonly IdDataProtection _idDataProtection;

        public FileOperationHub(
            IMediator mediator,
            IStorage storage,
            IdDataProtection dataProtection
        )
        {
            _mediator = mediator;
            _storage = storage;
            _idDataProtection = dataProtection;
        }
        
        public async Task List(string search, string categoryId, string buckedId)
        {
            var user = this.Context.GetHttpContext()?.User;
            if (string.IsNullOrEmpty(search) || user == null)
            {
                await this.Clients.Client(this.Context.ConnectionId).SendAsync("ReceiveListing", new
                {
                    status = false,
                    message = "No user claim principal available",
                    listing = new List<ObjectInfo>()
                });
                Log.Error("There was an incident: Denied access to bucket through WS - client had no Claims Principal");
                return;
            }

            if (!await _storage.UserHasBucketAccess(Guid.Parse(buckedId), user))
            {
                await this.Clients.Client(this.Context.ConnectionId).SendAsync("ReceiveListing", new
                {
                    status = false,
                    message = "Access Denied",
                    listing = new List<ObjectInfo>()
                });
                Log.Error("There was an incident: Denied access to bucket through WS - client had no proper roles!");
                return;
            }

            var listing = await _mediator.Send(
                new FindAllObjectsByNameQuery(search, categoryId, buckedId)
            );
            foreach (var oi in listing)
            {
                oi.FullName = _idDataProtection.Encode(oi.FullName);
            } 
            await this.Clients.Client(this.Context.ConnectionId).SendAsync("ReceiveListing", new
            {
                status = true,
                listing
            });
        }
    }
}