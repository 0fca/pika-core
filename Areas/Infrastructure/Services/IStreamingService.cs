using System.IO;

namespace PikaCore.Areas.Infrastructure.Services
{
    public interface IStreamingService
    {
        Stream GetVideoByPath(string path);
    }
}
