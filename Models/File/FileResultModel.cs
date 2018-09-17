using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FMS2.Models
{
    public class FileResultModel
    {
        public IDirectoryContents Contents { get; set; }
    }
}
