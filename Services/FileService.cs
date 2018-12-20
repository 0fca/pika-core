using FMS2.Controllers;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FMS2.Services{
    public class FileService : IFileOperator
    {
        private readonly IFileLoggerService _fileLoggerService;
        public FileService(IFileLoggerService fileLoggerService) {
            _fileLoggerService = fileLoggerService;
        }
        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        //private CancellationToken ct;
        public void Cancel()
        {
            tokenSource.Cancel();
            if(tokenSource.IsCancellationRequested){
                try{
                    tokenSource.Token.ThrowIfCancellationRequested();
                }catch(OperationCanceledException e){
                    Debug.WriteLine(e.Message+": Downloading cancled by user.");
                }finally{
                    tokenSource.Dispose();
                }
            }
        }

        public async Task<Stream> DownloadAsStreamAsync(string absolutPath)
        {
            return await Task<Stream>.Factory.StartNew(() => {
                Debug.WriteLine(absolutPath);
                return File.Exists(absolutPath) ? System.IO.File.OpenRead(absolutPath) : null;
            }, tokenSource.Token);
        }

        public async Task<byte[]> DownloadAsync(string absolutPath)
        {
            return await Task<byte[]>.Factory.StartNew(() =>{
                Debug.WriteLine(absolutPath);
                return File.Exists(absolutPath) ? System.IO.File.ReadAllBytes(absolutPath) : null;
            }, tokenSource.Token); 
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
    }
}