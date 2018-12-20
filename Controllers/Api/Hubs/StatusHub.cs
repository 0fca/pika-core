using FMS2.Services;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Api.Hubs
{
    public class StatusHub : Hub
    {
        public object StatusObject;

        public void CancelArchivingService() {
            var archivingService = (ArchiveService)StatusObject;
            archivingService.Cancel();
        }
    }
}