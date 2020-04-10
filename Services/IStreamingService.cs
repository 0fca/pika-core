using System.IO;

namespace PikaCore.Services
{
    public interface IStreamingService
    {
        Stream GetVideoByPath(string path);
    }
}
