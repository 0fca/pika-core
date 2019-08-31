using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;

namespace FMS2.Models
{
    public class FileResultModel
    {
        public bool HasFound { get; set; } = false;
        public List<IFileInfo> Contents { get; set; }
        public List<string> ToBeDeleted { get; set; }
    }
}
