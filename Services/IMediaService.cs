using PikaCore.Services.Helpers;
using System.Threading.Tasks;

namespace PikaCore.Services
{
    public interface IMediaService
    {
        void Init();
        Task<string> CreateThumb(string absoluteSystemPath, string guid, MediaType mediaType = MediaType.Image);
        Task<string> Scale(string absoluteSystemPath, string id, int height, int width);

        void Dispose();
    }
}
