using Microsoft.AspNetCore.SignalR;
using PikaCore.Areas.Core.Services;

namespace PikaCore.Areas.Core.Controllers.Hubs
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