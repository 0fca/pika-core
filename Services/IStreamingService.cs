using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FMS2.Services
{
    public interface IStreamingService
    {
         Task<Stream> GetVideoByPath(string path);
    }
}
