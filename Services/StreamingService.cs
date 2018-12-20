using FMS2.Controllers.Api;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace FMS2.Services
{
    public class StreamingService : IStreamingService
    {
        private HttpClient _client;
        private readonly IFileOperator _fileDownloader;

        public StreamingService(IFileOperator fileDownloader)
        {
            _client = new HttpClient();
            _fileDownloader = fileDownloader;
        }

        public async Task<Stream> GetVideoByPath(string path)
        {
            var urlBlob = string.Empty;
            var extension = Path.GetExtension(path);
            Stream outStream = null;
            switch (extension) {
                case ".mp4":
                case ".mp3":
                    outStream = await _client.GetStreamAsync(urlBlob);
                    break;
                case ".avi":
                    /*
                    var output = Constants.Tmp + Path.GetFileName(path);
                    fFMpegConverter.ConvertMedia(path, output, Format.mp4);
                    outStream = await _fileDownloader.DownloadAsStreamAsync(output);
                    */
                    VideoStreamDecoder videoStreamDecoder = new VideoStreamDecoder(path);
                    Debug.WriteLine(videoStreamDecoder.CodecName);
                    break;
            }
            return outStream;
        }

        ~StreamingService()
        {
            if (_client != null)
                _client.Dispose();
        }
    }
}
