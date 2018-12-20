using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace FMS2.Services{
    public class ArchiveService : IZipper, INotifyPropertyChanged
    {
        private Task task;
        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        private string outputPath = "";
        private bool CanBeCancelled { get; set; } = true;
        public event PropertyChangedEventHandler PropertyChanged;

        public void Cancel()
        {
            if (CanBeCancelled) {
                tokenSource.Cancel();
                try
                {
                    tokenSource.Token.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException e)
                {
                    CanBeCancelled = true;
                    Debug.WriteLine(e.Message + " Zipping cancelled by user.");
                }
            }
        }

        public async Task<Task> ZipDirectoryAsync(string absolutePath, string output)
        {
            outputPath = output;
            if(File.Exists(output)){
                File.Delete(output);
            }
            if (tokenSource.IsCancellationRequested) {
                tokenSource.Dispose();
                tokenSource = new CancellationTokenSource();
            }

            await Task.Delay(TimeSpan.FromSeconds(10d));
            task = Task.Factory.StartNew(() => {
                
                if (!tokenSource.IsCancellationRequested)
                {
                    CanBeCancelled = false;
                    OnPropertyChanged("CanBeCancelled");
                    //tokenSource.Token.ThrowIfCancellationRequested();
                    ZipFile.CreateFromDirectory(absolutePath, output, CompressionLevel.Fastest, false);
                    CanBeCancelled = true;
                }
            },tokenSource.Token);
            return task;
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public bool WasStartedAlready()
        {
            return !CanBeCancelled;
        }
    }
}