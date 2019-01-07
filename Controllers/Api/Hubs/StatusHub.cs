using FMS2.Services;
using Microsoft.AspNetCore.SignalR;

namespace FMS2.Controllers.Api.Hubs
{
    public class StatusHub : Hub
    {
        private object StatusObject;

        public void CancelArchivingService() {
            var archivingService = (ArchiveService)StatusObject;
            archivingService.Cancel();
        }
    }
}