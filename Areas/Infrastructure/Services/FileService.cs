using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using PikaCore.Areas.Core.Controllers.App;
using PikaCore.Areas.Infrastructure.Services.Helpers;
using Serilog;

namespace PikaCore.Areas.Infrastructure.Services
{
    public class FileService : IFileService
    {
        private readonly IFileProvider _fileProvider;
        private readonly IConfiguration _configuration;

        public FileService(IFileProvider fileProvider,
                           IConfiguration configuration)
        {
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
                Log.Error(e, "FileService#Cancel");
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

        public bool HideFile(string absolutePath)
        {
            var currentName = "";
            currentName = Directory.Exists(absolutePath) 
                ? Path.GetDirectoryName(absolutePath) 
                : Path.GetFileName(absolutePath);
            var hiddenName = string.Concat("~", currentName);
            
            return Move(absolutePath,  
                Path.Combine(Directory.GetParent(absolutePath).FullName, hiddenName));
        }

        public bool ShowFile(string absolutePath)
        {
            var currentName = "";
            currentName = Directory.Exists(absolutePath) 
                ? Path.GetDirectoryName(absolutePath) 
                : Path.GetFileName(absolutePath);
            var shownName = currentName?.Replace("~", "");
            
            return Move(absolutePath,  
                Path.Combine(Directory.GetParent(absolutePath).FullName, shownName));
        }

        public bool IsSameFile(string original, string copy)
        {
            byte[] origHash, copyHash;
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(original))
                origHash = md5.ComputeHash(stream);
            
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(copy))
                copyHash = md5.ComputeHash(stream);
            
            return origHash.SequenceEqual(copyHash);
        }

        public Stream AsStreamAsync(string absolutePath, int bufferSize = 8192, bool useAsync = true)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
            {
                Log.Error("Path cannot be null!");
                throw new ArgumentException("Path cannot be null!");
            }
            
            var fs = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize, useAsync);
            return fs;
        }

        public async Task<DirectoryInfo?> MkdirAsync(string systemPath)
        {
            return await Task.Factory.StartNew(() =>
                {
                    try
                    {
                        return Directory.CreateDirectory(systemPath);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "FileService#MkdirAsync");
                        return null;
                    }
                }
            );
        }

        public async Task<FileStream?> TouchAsync(string physicalAbsolutePath)
        {
            return await Task.Factory.StartNew(() =>
            {
                try
                {
                    return File.Create(physicalAbsolutePath);
                }
                catch (Exception e)
                {
                    Log.Error(e, "FileService#TouchAsync");
                    return null;
                }
            });
        }

        public async Task<string?> DumpFileStreamAsync(FileStream? physicalAbsolutePath)
        {
            if (physicalAbsolutePath != null && physicalAbsolutePath.Length == 0) return null;
            await physicalAbsolutePath?.FlushAsync()!;
            physicalAbsolutePath.Close();

            return physicalAbsolutePath.Name;
        }

        public async Task<Tuple<string?, Dictionary<string, string>>> SanitizeFileUpload(List<IFormFile> formFileList,
            string destinationPath,
            bool isAdmin = false)
        {
            var tmpFilesMap = new Dictionary<string, string>();
            var filePath = Constants.UploadTmp;

            var checkResultMessage = "";
            foreach (var formFile in formFileList.Where(formFile => formFile.Length > 0))
            {
                var originalName = WebUtility.HtmlEncode(formFile.FileName);
                var tmpPath = Path.Combine(filePath, Path.GetRandomFileName());
                await using var fs = await TouchAsync(tmpPath);
                tmpFilesMap.Add(tmpPath, Path.Combine(RetrieveAbsoluteFromSystemPath(destinationPath), originalName));
                await formFile.CopyToAsync(fs);

                checkResultMessage = FileSecurityHelper.ProcessTemporaryStoredFile(
                    originalName,
                    fs, 
                    _configuration.GetSection("Storage:PermittedExtensions").Get<List<string>>(),
                    _configuration.GetSection("Storage:PermittedMimes").Get<List<string>>(),
                    Constants.MaxUploadSize,
                    isAdmin);
                await DumpFileStreamAsync(fs);
            }

            return Tuple.Create(checkResultMessage, tmpFilesMap);
        }

        public async Task PostSanitizeUpload(Dictionary<string, string> files)
        {
            var enumerator = files.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var (tmpFilePath, resultDestination) = enumerator.Current;
                await Copy(tmpFilePath, resultDestination);
            }
            enumerator.Dispose();
        }

        public async Task<byte[]> AsBytesAsync(string absolutePath)
        {
            return await Task<byte[]>.Factory.StartNew(() => 
                (File.Exists(absolutePath) 
                    ? File.ReadAllBytes(absolutePath) 
                    : null) ?? Array.Empty<byte>(), _tokenSource.Token
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
                    Log.Error(e, "FileService#Move");
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
                Log.Error(e, "FileService#Copy");
            }
        }

        public async Task<List<string>?> ListPath(string path)
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

        public static IEnumerable<string> TraverseDirectories(string? rootDirectory)
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

        private static IEnumerable<string> TraverseFiles(string? rootDirectory)
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
