using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Pika.Adapters.Filesystem;
using PikaCore.Infrastructure.Services.Helpers;

namespace PikaCore.Areas.Core.Models.File
{
    public class FileResultViewModel
    {
        public IDirectoryContents Contents { get; set; } = new NotFoundDirectoryContents();
        public List<string> ToBeDeleted { get; set; } = new List<string>();
        public List<IFileInfo> ContentsList { get; set; } = new List<IFileInfo>();
    }
}
