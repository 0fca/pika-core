using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace FMS2.Services{
    public class ArchiveService : IZipper
    {
        private Task task;
        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        public void Cancel()
        {
            
            tokenSource.Cancel();
                try{
                    tokenSource.Token.ThrowIfCancellationRequested();
                }catch(OperationCanceledException e){
                    Debug.WriteLine(e.Message+" Zipping cancled by user.");
                }finally{
                    try
                    {
                        task.Wait();
                    }
                    catch (AggregateException e)
                    {
                        foreach (var v in e.InnerExceptions)
                            Console.WriteLine(e.Message + " " + v.Message);
                    }
                    finally
                    {
                        tokenSource.Dispose();
                    }
                }
        }

        public Task ZipDirectoryAsync(string absolutePath, string output)
        {
            if(File.Exists(output)){
                File.Delete(output);
            }
            task = Task.Factory.StartNew(() =>{
                tokenSource.Token.ThrowIfCancellationRequested();
                ZipFile.CreateFromDirectory(absolutePath, output);
            },tokenSource.Token);
            
            return task;
        }
    }
}