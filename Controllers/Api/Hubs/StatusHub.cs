using FMS2.Services;
using Microsoft.AspNetCore.SignalR;

namespace FMS2.Controllers.Api.Hubs
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