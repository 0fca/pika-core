using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace PikaCore.Areas.Infrastructure.Services
{
    public interface IFileService
    {
        Task<DirectoryInfo?> MkdirAsync(string systemPath);

        Task<FileStream?> TouchAsync(string physicalAbsolutePath);

        Task<string?> DumpFileStreamAsync(FileStream? physicalAbsolutePath);

        Task<Tuple<string, Dictionary<string, string>>> SanitizeFileUpload(List<IFormFile> formFileList, string destinationDirectoryPath, bool isAdmin);

        Task PostSanitizeUpload(Dictionary<string, string> files);
        
        Task<byte[]> AsBytesAsync(string absolutePath);
        Stream AsStreamAsync(string absolutePath, int bufferSize = 8192, bool useAsync = true);
        Task Copy(string what, string toWhere);
        bool Move(string what, string toWhere);
        Task Delete(List<string> fileList);
        Task<IEnumerable<string>> WalkFileTree(string path);
        Task<IEnumerable<string>> WalkDirectoryTree(string path);

        Task<List<string>?> ListPath(string path);
        void Cancel();
        string RetrieveAbsoluteFromSystemPath(string path);

        string RetrieveSystemPathFromAbsolute(string absolutePath);

        IFileInfo RetrieveFileInfoFromSystemPath(string systemPath);

        IFileInfo RetrieveFileInfoFromAbsolutePath(string path);

        IDirectoryContents GetDirectoryContents(string systemPath);

        bool HideFile(string absolutePath);

        bool ShowFile(string absolutePath);

        bool IsSameFile(string original, string copy);
    }
}