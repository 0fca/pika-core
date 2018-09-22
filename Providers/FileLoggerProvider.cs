using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.AzureAppServices.Internal;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FMS2.Providers
{
    [ProviderAlias("File")]
    public class FileLoggerProvider : Extensions.BatchingLoggerProvider
    {
        private readonly string _path;
        private readonly string _fileName;
        private readonly int? _maxFileSize;
        private readonly int? _maxRetainedFiles;
        private readonly bool _shouldBackup = true;
        private readonly string _backupDir = "";
        private string fullName = "";

        public FileLoggerProvider(IOptions<FileLoggerOptions> options) : base(options)
        {
            var loggerOptions = options.Value;
            _path = loggerOptions.LogDirectory;
            _fileName = loggerOptions.FileName;
            _maxFileSize = loggerOptions.FileSizeLimit;
            _maxRetainedFiles = loggerOptions.RetainedFileCountLimit;
            _shouldBackup = loggerOptions.ShouldBackupLogs;
            _backupDir = loggerOptions.BackupLogDir;
        }

        // Write the provided messages to the file system
        protected override async Task WriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(_path);

            // Group messages by log date
            foreach (var group in messages.GroupBy(GetGrouping))
            {
                var fullName = GetFullName(group.Key);
                this.fullName = fullName;
                var fileInfo = new FileInfo(fullName);
                // If we've exceeded the max file size, don't write any logs
                if (_maxFileSize > 0 && fileInfo.Exists && fileInfo.Length > _maxFileSize)
                {
                    return;
                }

                // Write the log messages to the file
                using (var streamWriter = File.AppendText(fullName))
                {
                    foreach (var item in group)
                    {
                        await streamWriter.WriteAsync(item.Message);
                    }
                }
            }

            DoCleanup();
        }

        // Get the file name
        private string GetFullName((int Year, int Month, int Day) group)
        {
            return Path.Combine(_path, $"{_fileName}{group.Year:0000}{group.Month:00}{group.Day:00}.log");
        }

        private (int Year, int Month, int Day) GetGrouping(LogMessage message)
        {
            return (message.Timestamp.Year, message.Timestamp.Month, message.Timestamp.Day);
        }

        // Delete files if we have too many
        protected void RollFiles()
        {
            if (_maxRetainedFiles > 0)
            {
                var files = new DirectoryInfo(_path)
                    .GetFiles(_fileName + "*")
                    .OrderByDescending(f => f.Name)
                    .Skip(_maxRetainedFiles.Value);

                foreach (var item in files)
                {
                    item.Delete();
                }
            }
        }

        private void DoCleanup()
        {
            if(_shouldBackup && _maxRetainedFiles > 0){
                var files = new DirectoryInfo(_path)
                        .GetFiles(_fileName + "*")
                        .OrderByDescending(f => f.Name)
                        .Skip(_maxRetainedFiles.Value);
                foreach (var file in files) {
                    File.Copy(file.FullName, _backupDir+Path.DirectorySeparatorChar+file.Name);
                }
                RollFiles();
            }
        }

        public void IdleForCleanup()
        {
            DoCleanup();
        }

        public IEnumerable<string> GetLogs()
        {
            return File.ReadAllLines(fullName).AsEnumerable<string>();
        }
    }
}
