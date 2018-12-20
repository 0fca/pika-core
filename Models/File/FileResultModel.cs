using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FMS2.Models
{
    public class FileResultModel
    {
        public bool HasFound { get; set; } = false;
        public List<IFileInfo> Contents { get; set; }
        public List<string> ToBeDeleted { get; set; }
    }
}
