using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace FMS2.Services{
    public class ArchiveService : IZipper
    {
        private static Task task;
        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        //private CancellationToken ct;
        public void Cancel()
        {
            tokenSource.Cancel();
            if(tokenSource.IsCancellationRequested){
                try{
                    tokenSource.Token.ThrowIfCancellationRequested();
                }catch(OperationCanceledException e){
                    Debug.WriteLine(e.Message+": Zipping cancled by user.");
                }finally{
                    tokenSource.Dispose();
                }
            }
        }

        public Task ZipDirectoryAsync(string absolutePath, string output)
        {
        
            Debug.WriteLine("Token inited.");
            if(File.Exists(output)){
                File.Delete(output);
            }
            task = Task.Factory.StartNew(() =>{
                ZipFile.CreateFromDirectory(absolutePath, output);
            },tokenSource.Token);
            return task;
        }
    }
}