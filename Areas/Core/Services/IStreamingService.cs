using System.IO;

namespace PikaCore.Areas.Core.Services
{
    public interface IStreamingService
    {
        Stream GetVideoByPath(string path);
    }
}
