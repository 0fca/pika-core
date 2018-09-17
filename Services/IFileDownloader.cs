using System.Threading.Tasks;

namespace FMS2.Services{
    public interface IFileDownloader
    {
        Task<byte[]> DownloadAsync(string absolutPath);
        void Cancel();
    }
}