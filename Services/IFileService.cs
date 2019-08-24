using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FMS2.Services{
    public interface IFileService
    {
        Task<byte[]> DownloadAsync(string absolutPath);
        Task<Stream> DownloadAsStreamAsync(string absolutPath);
        Task MoveFromTmpAsync(string fileName, string toWhere);
        Task Copy(string what, string toWhere);
        Task Move(string what, string toWhere);
        Task<IEnumerable<string>> WalkFileTree(string path, int depth = 1);
        Task<List<string>> ListPath(string path);
        void Cancel();
    }
}