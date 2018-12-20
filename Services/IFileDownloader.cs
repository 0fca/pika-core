using System.IO;
using System.Threading.Tasks;

namespace FMS2.Services{
    public interface IFileOperator
    {
        Task<byte[]> DownloadAsync(string absolutPath);
        Task<Stream> DownloadAsStreamAsync(string absolutPath);
        Task MoveFromTmpAsync(string fileName, string toWhere);
        void Cancel();
    }
}