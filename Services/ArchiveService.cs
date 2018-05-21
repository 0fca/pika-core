using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace FMS2.Services{
    public class ArchiveService : IFilesystemInterface
    {
        private Task task;
        CancellationTokenSource tokenSource;
        public void Cancel()
        {
            tokenSource.Cancel();
        }

        public Task ZipDirectoryAsync(string absolutePath, string output)
        {
            tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;
            task = Task.Factory.StartNew(() =>{
                ct.ThrowIfCancellationRequested();
                ZipFile.CreateFromDirectory(absolutePath, output);
                ct.ThrowIfCancellationRequested();
            },tokenSource.Token);
            
            return task;
        }
    }
}