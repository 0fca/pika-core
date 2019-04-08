using System.Threading.Tasks;
using FMS2.Services;
using Microsoft.AspNetCore.SignalR;

namespace FMS2.Controllers.Api.Hubs
{
    public class FileOperationHub : Hub
    {
        private readonly object _fileServiceInstance;
        private readonly FileService _fileService;
        
        public FileOperationHub()
        {
            _fileService = (FileService) _fileServiceInstance;
        }

        public void Copy()
        {
            
        }

        public void Move()
        {

        }

        public async Task List(string path)
        {
            var listing = await _fileService.WalkFileTree(path);
            await this.Clients.All.SendAsync("ReceiveListing", listing);
        }
    }
}