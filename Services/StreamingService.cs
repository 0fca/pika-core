using System.IO;

namespace PikaCore.Services
{
    public class StreamingService : IStreamingService
    {
        private readonly IFileService _fileDownloader;

        public StreamingService(IFileService fileDownloader)
        {
            _fileDownloader = fileDownloader;
        }

        public Stream GetVideoByPath(string path)
        {
            var extension = Path.GetExtension(path);
            var outStream = extension switch
            {
                ".mp4" => _fileDownloader.AsStreamAsync(path),
                ".mp3" => _fileDownloader.AsStreamAsync(path),
                ".m4a" => _fileDownloader.AsStreamAsync(path),
                _ => null
            };
            return outStream;
        }
    }
}
