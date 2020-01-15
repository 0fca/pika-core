using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PikaCore.Services
{
    public interface IFileService
    {
        Task<DirectoryInfo> Create(string returnPath, string name);
        Task<byte[]> DownloadAsync(string absolutPath);
        Task<Stream> DownloadAsStreamAsync(string absolutPath);
        Task MoveFromTmpAsync(string fileName, string toWhere);
        Task Copy(string what, string toWhere);
        Task Move(string what, string toWhere);
        Task Delete(List<string> fileList);
        Task<IEnumerable<string>> WalkFileTree(string path, int depth = 1);
        Task<IEnumerable<string>> WalkDirectoryTree(string path);

        Task<List<string>> ListPath(string path);
        Task<List<IFileInfo>> SortContents(IDirectoryContents tmp);
        void Cancel();
    }
}