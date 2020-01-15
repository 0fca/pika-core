using Microsoft.AspNetCore.SignalR;
using PikaCore.Services;

namespace PikaCore.Controllers.Api.Hubs
{
    public class StatusHub : Hub
    {
        private object _statusObject;

        public void CancelArchivingService()
        {
            var archivingService = (ArchiveService)_statusObject;
            archivingService.Cancel();
        }
    }
}