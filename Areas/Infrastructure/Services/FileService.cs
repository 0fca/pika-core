using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using PikaCore.Areas.Core.Controllers.App;
using PikaCore.Areas.Core.Services;

namespace PikaCore.Areas.Infrastructure.Services
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
                (_configuration.GetSection("Paths")[Constants.OsName + "-root"]).Length));
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

        public async Task<DirectoryInfo> MkdirAsync(string systemPath)
        {
            return await Task.Factory.StartNew(() =>
                {
                    try
                    {
                        return Directory.CreateDirectory(systemPath);
                    }
                    catch (Exception e)
                    {
                        _fileLoggerService.LogToFileAsync(LogLevel.Error, "localhost", e.Message);
                        return null;
                    }
                }
            );
        }

        public async Task<FileStream> TouchAsync(string physicalAbsolutePath)
        {
            return await Task.Factory.StartNew(() =>
            {
                try
                {
                    return File.Create(physicalAbsolutePath);
                }
                catch (Exception e)
                {
                    _fileLoggerService.LogToFileAsync(LogLevel.Error, "localhost", e.Message);
                    return null;
                }
            });
        }

        public async Task<string> DumpFileStreamAsync(FileStream physicalAbsolutePath)
        {
            if (physicalAbsolutePath.Length == 0) return null;
            await physicalAbsolutePath.FlushAsync();
            physicalAbsolutePath.Close();

            return physicalAbsolutePath.Name;
        }

        public async Task<byte[]> AsBytesAsync(string absolutePath)
        {
            return await Task<byte[]>.Factory.StartNew(() => 
                File.Exists(absolutePath) 
                    ? File.ReadAllBytes(absolutePath) 
                    : null, _tokenSource.Token
                );
        }

        public bool Move(string absolutePath, string toWhere)
        {
            var isMoved = true;
            Task.Factory.StartNew(() =>
            {
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
                    _fileLoggerService.LogToFileAsync(LogLevel.Error, "localhost",
                        $"Couldn't move {absolutePath} to {toWhere} because of {e.Message}");
                    isMoved = false;
                }
            });
            return isMoved;
        }

        public async Task Copy(string absolutePath, string toWhere)
        {
            try
            {
                if (Directory.Exists(absolutePath))
                {
                    var directories = await WalkDirectoryTree(absolutePath);
                    directories.ToList().ForEach(dir => { Directory.CreateDirectory(dir); });
                    var filePaths = await WalkFileTree(absolutePath);
                    filePaths.ToList().ForEach(path =>
                    {
                        File.Copy(path, Path.Combine(toWhere, Path.GetFileName(path)));
                    });       
                }
                else
                {
                    File.Copy(absolutePath, toWhere);
                }
            }
            catch (Exception e)
            {
                _fileLoggerService.LogToFileAsync(LogLevel.Error, "localhost",
                    $"Couldn't move {absolutePath} to {toWhere} because of {e.Message}");
            }
        }

        public async Task<List<string>> ListPath(string path)
        {
            var hostPath = this.RetrieveAbsoluteFromSystemPath(path);
            return !Directory.Exists(hostPath) ? null : (await Task.Factory.StartNew(() => Directory.GetDirectories(hostPath))).ToList();
        }

        public async Task<IEnumerable<string>> WalkFileTree(string path)
        {
            var hostPath = this.RetrieveAbsoluteFromSystemPath(path);
            return await Task<IEnumerable<string>>.Factory.StartNew(() => TraverseFiles(hostPath));
        }

        public async Task<IEnumerable<string>> WalkDirectoryTree(string path)
        {
            var hostPath = this.RetrieveAbsoluteFromSystemPath(path);
            return await Task<IEnumerable<string>>.Factory.StartNew(() => TraverseDirectories(hostPath));
        }

        public static IEnumerable<string> TraverseDirectories(string rootDirectory)
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

        private static IEnumerable<string> TraverseFiles(string rootDirectory)
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
