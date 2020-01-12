using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace FMS2.Services
{
    public class ArchiveService : IZipper, INotifyPropertyChanged
    {
        private Task _task;
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private string _outputPath = "";
        private bool CanBeCancelled { get; set; } = true;
        public event PropertyChangedEventHandler PropertyChanged;

        public void Cancel()
        {
            if (CanBeCancelled)
            {
                _tokenSource.Cancel();
                try
                {
                    _tokenSource.Token.ThrowIfCancellationRequested();
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
            _outputPath = output;
            if (File.Exists(output))
            {
                File.Delete(output);
            }
            if (_tokenSource.IsCancellationRequested)
            {
                _tokenSource.Dispose();
                _tokenSource = new CancellationTokenSource();
            }

            //_task = Task.Delay(TimeSpan.FromSeconds(10d));
            _task = Task.Factory.StartNew(() =>
            {

                if (!_tokenSource.IsCancellationRequested)
                {
                    CanBeCancelled = false;
                    OnPropertyChanged("CanBeCancelled");
                    ZipFile.CreateFromDirectory(absolutePath, output, CompressionLevel.Fastest, false);
                    CanBeCancelled = true;
                }
            }, _tokenSource.Token);
            return _task;
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
