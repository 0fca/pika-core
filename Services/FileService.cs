using FMS2.Controllers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace FMS2.Services{
    public class FileService : IFileOperator
    {
        private readonly IFileLoggerService _fileLoggerService;
        private readonly IConfiguration _configuration;
        
        public FileService(IFileLoggerService fileLoggerService, IConfiguration configuration) {
            _fileLoggerService = fileLoggerService;
            _configuration = configuration;
        }
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        //private CancellationToken ct;
        public void Cancel()
        {
            _tokenSource.Cancel();
            if (!_tokenSource.IsCancellationRequested) return;
            
            try{
                _tokenSource.Token.ThrowIfCancellationRequested();
            }catch(OperationCanceledException e){
                Debug.WriteLine(e.Message+": Downloading cancled by user.");
            }finally{
                _tokenSource.Dispose();
            }
        }

        public async Task<Stream> DownloadAsStreamAsync(string absolutPath)
        {
            return await Task<Stream>.Factory.StartNew(() => {
                Debug.WriteLine(absolutPath);
                return File.Exists(absolutPath) ? System.IO.File.OpenRead(absolutPath) : null;
            }, _tokenSource.Token);
        }

        public async Task<byte[]> DownloadAsync(string absolutPath)
        {
            return await Task<byte[]>.Factory.StartNew(() =>{
                Debug.WriteLine(absolutPath);
                return File.Exists(absolutPath) ? System.IO.File.ReadAllBytes(absolutPath) : null;
            }, _tokenSource.Token); 
        }

        public async Task MoveFromTmpAsync(string fileName, string toWhere = null) {
            var file = Constants.Tmp + Constants.UploadTmp + Path.DirectorySeparatorChar + fileName;

            if (string.IsNullOrEmpty(toWhere)) {
                toWhere = Constants.UploadDirectory + fileName;
            }

            if (!Directory.Exists(toWhere)) {
                Directory.CreateDirectory(toWhere);
            }
                var fileStream = new FileStream(file, FileMode.Open);

                //var outputStream = new FileStream(toWhere+fileName, FileMode.Create);
                var buffer = new byte[fileStream.Length];
                await fileStream.ReadAsync(buffer);
                await File.WriteAllBytesAsync(toWhere + fileName, buffer);
                _fileLoggerService.LogToFileAsync(LogLevel.Information, "localhost", $"File {fileName} moved from tmp to " + toWhere);
        }

        public Task Move(string absolutePath, string toWhere)
        {
            throw new NotImplementedException();
        }

        public Task Copy(string absolutePath, string toWhere)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<string>> WalkFileTree(string path)
        {

            var storage = path;

            if (storage == null) {
               storage = _configuration.GetSection("Paths")["storage"];
            }
            
            return await Task<IEnumerable<string>>.Factory.StartNew(() => Traverse(storage));
        }
        
        private static IEnumerable<string> Traverse(string rootDirectory)
        {
            var files = Enumerable.Empty<string>();
            var directories = Enumerable.Empty<string>();
            try
            {
                var permission = new FileIOPermission(FileIOPermissionAccess.PathDiscovery, rootDirectory);
                permission.Demand();

                files = Directory.GetFiles(rootDirectory);
                directories = Directory.GetDirectories(rootDirectory);
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

            var subdirectoryItems = directories.SelectMany(Traverse);
            foreach (var result in subdirectoryItems)
            {
                yield return result;
            }
        }
    }
}