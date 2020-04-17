using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;

namespace PikaCore.Areas.Core.Services
{
    public interface IFileService
    {
        Task<DirectoryInfo> Create(string systemPath);
        Task<byte[]> AsBytesAsync(string absolutePath);
        Stream AsStreamAsync(string absolutePath, int bufferSize = 8192, bool useAsync = true);
        Task MoveFromTmpAsync(string fileName, string toWhere);
        Task Copy(string what, string toWhere);
        bool Move(string what, string toWhere);
        Task Delete(List<string> fileList);
        Task<IEnumerable<string>> WalkFileTree(string path, int depth = 1);
        Task<IEnumerable<string>> WalkDirectoryTree(string path);

        Task<List<string>> ListPath(string path);
        void Cancel();
        string RetrieveAbsoluteFromSystemPath(string path);

        string RetrieveSystemPathFromAbsolute(string absolutePath);

        IFileInfo RetrieveFileInfoFromSystemPath(string systemPath);

        IFileInfo RetrieveFileInfoFromAbsolutePath(string path);

        IDirectoryContents GetDirectoryContents(string systemPath);
    }
}