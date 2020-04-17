using FFmpeg.NET;
using PikaCore.Services.Helpers;
using System.Threading.Tasks;

namespace PikaCore.Services
{
    public interface IMediaService
    {
        void Init();
        Task<string> CreateThumb(string absoluteSystemPath, string guid, int size);
        Task<string>  GrabFromImage(string absoluteSystemPath, string id, int height, int width);

        Task GrabFromVideo(string absoluteSystemVideoPath, string absoluteSystemOutputPath, ConversionOptions conversionOptions);

        void Dispose();
    }
}
