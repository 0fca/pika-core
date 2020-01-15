using System.IO;
using System.Threading.Tasks;

namespace PikaCore.Services
{
    public interface IStreamingService
    {
        Task<Stream> GetVideoByPath(string path);
    }
}
