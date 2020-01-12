using FMS2.Controllers;
using FMS2.Controllers.Helpers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

namespace FMS2.Services
{
    public class FileService : IFileService
    {
        private readonly IFileLoggerService _fileLoggerService;
        private readonly IFileProvider _fileProvider;

        public FileService(IFileLoggerService fileLoggerService,
                           IFileProvider fileProvider)
        {
            _fileLoggerService = fileLoggerService;
            _fileProvider = fileProvider;
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
                Debug.WriteLine(e.Message + ": Downloading cancled by user.");
            }
            finally
            {
                _tokenSource.Dispose();
            }
        }

        public async Task<Stream> DownloadAsStreamAsync(string absolutPath)
        {
            return await Task<Stream>.Factory.StartNew(() =>
            {
	    	_fileLoggerService.LogToFileAsync(LogLevel.Information, "localhost", $"File: {absolutPath}");
                return File.Exists(absolutPath) ? System.IO.File.OpenRead(absolutPath) : null;
            }, _tokenSource.Token);
        }

        public async Task<DirectoryInfo> Create(string returnPath, string name)
        {
            return await Task.Factory.StartNew(() => Directory.CreateDirectory(string.Concat(
                        _fileProvider.GetFileInfo(returnPath).PhysicalPath,
                        Path.DirectorySeparatorChar,
                        name
            )));
        }

        public async Task<byte[]> DownloadAsync(string absolutPath)
        {
            return await Task<byte[]>.Factory.StartNew(() =>
            {
                return File.Exists(absolutPath) ? System.IO.File.ReadAllBytes(absolutPath) : null;
            }, _tokenSource.Token);
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
            var fileStream = new FileStream(file, FileMode.Open);

            var buffer = new byte[fileStream.Length];
            await fileStream.ReadAsync(buffer);
            await File.WriteAllBytesAsync(toWhere + fileName, buffer);
            _fileLoggerService.LogToFileAsync(LogLevel.Information, "localhost", $"File {fileName} moved from tmp to " + toWhere);
            fileStream.Flush();
            fileStream.Close();
        }

        public Task Move(string absolutePath, string toWhere)
        {
            throw new NotImplementedException();
        }

        public Task Copy(string absolutePath, string toWhere)
        {
            throw new NotImplementedException();
        }

        public async Task<List<string>> ListPath(string path)
        {
            var hostPath = UnixHelper.MapToPhysical(Constants.FileSystemRoot, path);
            return (await Task.Factory.StartNew(() => Directory.GetDirectories(hostPath))).ToList();
        }

        public async Task<IEnumerable<string>> WalkFileTree(string path, int depth)
        {
            var hostPath = UnixHelper.MapToPhysical(Constants.FileSystemRoot, path);
            return await Task<IEnumerable<string>>.Factory.StartNew(() => TraverseFiles(hostPath, depth));
        }

        public async Task<IEnumerable<string>> WalkDirectoryTree(string path)
        {
            var hostPath = UnixHelper.MapToPhysical(Constants.FileSystemRoot, path);
            return await Task<IEnumerable<string>>.Factory.StartNew(() => TraverseDirectories(hostPath));
        }

        private IEnumerable<string> TraverseDirectories(string rootDirectory)
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

        private IEnumerable<string> TraverseFiles(string rootDirectory, int depth)
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

            foreach (var file in files)
            {
                yield return file;
            }

            foreach (var directory in directories)
            {
                yield return directory;
            }

            var subdirectoryItems = files.SelectMany(TraverseFiles);

            foreach (var result in subdirectoryItems)
            {
                yield return result;
            }
        }

        public async Task<List<IFileInfo>> SortContents(IDirectoryContents tmp)
        {
            var asyncFileEnum = await Task.Factory.StartNew(() => tmp.Where(entry => !entry.IsDirectory).OrderBy(predicate => predicate.Name));
            var asyncDirEnum = await Task.Factory.StartNew(() => tmp.Where(entry => entry.IsDirectory).OrderBy(predicate => predicate.Name));
            var resultList = new List<IFileInfo>();
            resultList.AddRange(asyncDirEnum);
            resultList.AddRange(asyncFileEnum);
            return resultList;
        }

        public async Task Delete(IAsyncEnumerable<string> fileList)
        {
            await fileList.ForEachAsync(item =>
            {
                if (Directory.Exists(item))
                {
                    Directory.Delete(item, true);
                }
                else
                {
                    System.IO.File.Delete(item);
                }
            });
        }
    }
}
