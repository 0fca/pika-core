using System.Collections.Generic;
using System.Threading.Tasks;
using FMS2.Services;
using Microsoft.AspNetCore.SignalR;

namespace FMS2.Controllers.Api.Hubs
{
    public class FileOperationHub : Hub
    {
        private object FileServiceInstance;
        private readonly FileService _fileService;
        
        public FileOperationHub()
        {
            _fileService = (FileService) FileServiceInstance;
        }

        public void Copy()
        {
            
        }

        public void Move()
        {

        }

        public async Task<IEnumerable<string>> WalkFileTree()
        {
            return await _fileService.WalkFileTree();
        }
    }
}