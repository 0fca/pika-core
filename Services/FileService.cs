using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FMS2.Services{
    public class FileService : IFileDownloader
    {
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

        public Task<byte[]> DownloadAsync(string absolutPath)
        {
            return Task<byte[]>.Factory.StartNew(() =>{
                Debug.WriteLine(absolutPath);
                return File.Exists(absolutPath) ? System.IO.File.ReadAllBytes(absolutPath) : null;
            }, tokenSource.Token); 
        }
    }
}