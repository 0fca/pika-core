using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PikaCore.Controllers;
using PikaCore.Controllers.App;

namespace PikaCore.Services
{
    public class FileService : IFileService
    {
        private readonly IFileLoggerService _fileLoggerService;
        private readonly IFileProvider _fileProvider;
        private readonly IConfiguration _configuration;

        public FileService(IFileLoggerService fileLoggerService,
                           IFileProvider fileProvider,
                           IConfiguration configuration)
        {
            _fileLoggerService = fileLoggerService;
            _fileProvider = fileProvider;
            _configuration = configuration;
        }
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public void Cancel()
        {
            _tokenSource.Cancel();
            if (!_tokenSource.IsCancellationRequested) return;

            try
            {
                _tokenSource.Token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException e)
            {
                _fileLoggerService.LogToFileAsync(LogLevel.Warning, "localhost", e.Message + " : Downloading canceled by the user.");
            }
            finally
            {
                _tokenSource.Dispose();
            }
        }

        public string RetrieveAbsoluteFromSystemPath(string systemPath)
        {
            var fileInfo = _fileProvider.GetFileInfo(systemPath);
            return fileInfo.PhysicalPath;
        }

        public string RetrieveSystemPathFromAbsolute(string absolutePath)
        {
            return absolutePath.Remove(0,
                (_configuration.GetSection("Paths")[Constants.OsName + "-root"]).Length - 1);
        }

        public IFileInfo RetrieveFileInfoFromSystemPath(string systemPath)
        {
            return this._fileProvider.GetFileInfo(systemPath);
        }

        public IFileInfo RetrieveFileInfoFromAbsolutePath(string absolutePath)
        {
            var fileInfo = _fileProvider.GetFileInfo(absolutePath.Remove(0,
                (_configuration.GetSection("Paths")[Constants.OsName + "-root"]).Length - 2));
            return fileInfo;
        }

        public IDirectoryContents GetDirectoryContents(string systemPath)
        {
            return _fileProvider.GetDirectoryContents(systemPath);
        }

        public Stream AsStreamAsync(string absolutePath, int bufferSize = 8192, bool useAsync = true)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
            {
                _fileLoggerService.LogToFileAsync(LogLevel.Critical, "localhost", $"Path for AsStreamAsync() cannot be null!");
                throw new ArgumentException("Path cannot be null!");
            }
            
            var fs = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize, useAsync);
            _fileLoggerService.LogToFileAsync(LogLevel.Information, "localhost", $"File: {absolutePath}");
            return fs;
        }

        public async Task<DirectoryInfo> Create(string systemPath)
        {
            return await Task.Factory.StartNew(() => 
                Directory.CreateDirectory(systemPath)
                );
        }

        public async Task<byte[]> AsBytesAsync(string absolutPath)
        {
            return await Task<byte[]>.Factory.StartNew(() => 
                File.Exists(absolutPath) 
                    ? File.ReadAllBytes(absolutPath) 
                    : null, _tokenSource.Token
                );
        }

        public async Task MoveFromTmpAsync(string fileName, string toWhere = null)
	    {
            var file = Constants.Tmp + Constants.UploadTmp + Path.DirectorySeparatorChar + fileName;

            if (string.IsNullOrEmpty(toWhere))
            {
                toWhere = Constants.UploadDirectory + fileName;
            }

            if (!Directory.Exists(toWhere))
            {
                Directory.CreateDirectory(toWhere);
            }

            await using var fileStream = new FileStream(file, FileMode.Open);
            var buffer = new byte[fileStream.Length];
            await fileStream.ReadAsync(buffer);
            await File.WriteAllBytesAsync(toWhere + fileName, buffer);
            _fileLoggerService.LogToFileAsync(LogLevel.Information, "localhost",
                $"File {fileName} moved from tmp to " + toWhere);
            fileStream.Flush();
        }

        public bool Move(string absolutePath, string toWhere)
        {
            var isMoved = true;
            try
            {
                if (Directory.Exists(absolutePath))
                {
                    Directory.Move(absolutePath, toWhere);
                }
                else
                {
                    File.Move(absolutePath, toWhere);
                }
            }
            catch (Exception e)
            {
                _fileLoggerService.LogToFileAsync(LogLevel.Error, "localhost", $"Couldn't move {absolutePath} to {toWhere}");
                isMoved = false;
            }

            return isMoved;
        }

        public Task Copy(string absolutePath, string toWhere)
        {
            throw new NotImplementedException();
        }

        public async Task<List<string>> ListPath(string path)
        {
            var hostPath = this.RetrieveAbsoluteFromSystemPath(path);
            return (await Task.Factory.StartNew(() => Directory.GetDirectories(hostPath))).ToList();
        }

        public async Task<IEnumerable<string>> WalkFileTree(string path, int depth)
        {
            var hostPath = this.RetrieveAbsoluteFromSystemPath(path);
            return await Task<IEnumerable<string>>.Factory.StartNew(() => TraverseFiles(hostPath, depth));
        }

        public async Task<IEnumerable<string>> WalkDirectoryTree(string path)
        {
            var hostPath = this.RetrieveAbsoluteFromSystemPath(path);
            return await Task<IEnumerable<string>>.Factory.StartNew(() => TraverseDirectories(hostPath));
        }

        private static IEnumerable<string> TraverseDirectories(string rootDirectory)
        {
            var directories = Enumerable.Empty<string>();
            try
            {
                var permission = new FileIOPermission(FileIOPermissionAccess.PathDiscovery, rootDirectory);
                permission.Demand();

                directories = Directory.GetDirectories(rootDirectory);
            }
            catch
            {
                rootDirectory = null;
            }

            if (rootDirectory != null)
                yield return rootDirectory;

            var subdirectoryItems = directories.SelectMany(TraverseDirectories);

            foreach (var result in subdirectoryItems)
            {
                yield return result;
            }
        }

        private static IEnumerable<string> TraverseFiles(string rootDirectory, int depth)
        {
            var files = Enumerable.Empty<string>();
            var directories = Enumerable.Empty<string>();

            try
            {
                var permission = new FileIOPermission(FileIOPermissionAccess.PathDiscovery, rootDirectory);
                permission.Demand();

                directories = Directory.GetDirectories(rootDirectory);
                files = Directory.GetFiles(rootDirectory);
            }
            catch
            {
                rootDirectory = null;
            }

            if (rootDirectory != null)
                yield return rootDirectory;

            var enumerable = files.ToList();
            foreach (var file in enumerable)
            {
                yield return file;
            }

            foreach (var directory in directories)
            {
                yield return directory;
            }

            var subdirectoryItems = enumerable.SelectMany(TraverseFiles);

            foreach (var result in subdirectoryItems)
            {
                yield return result;
            }
        }

        public async Task Delete(List<string> fileList)
        {
            await Task.Factory.StartNew(() => fileList.ForEach(item =>
            {
                if (Directory.Exists(item))
                {
                    Directory.Delete(item, true);
                }
                else
                {
                    File.Delete(item);
                }
            }));
        }
    }
}
