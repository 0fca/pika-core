using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;

namespace PikaCore.Areas.Core.Models.File
{
    public class FileResultViewModel
    {
        public IDirectoryContents Contents { get; set; } = new NotFoundDirectoryContents();
        public List<string> ToBeDeleted { get; set; } = new List<string>();
        public List<IFileInfo> ContentsList { get; set; } = new List<IFileInfo>();
    }
}
