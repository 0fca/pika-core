using System.IO;

namespace PikaCore.Infrastructure.Services
{
    public interface IStreamingService
    {
        Stream GetVideoByPath(string path);
    }
}
