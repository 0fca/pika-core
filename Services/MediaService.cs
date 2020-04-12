using FFmpeg.NET;
using PikaCore.Controllers.Helpers;
using Microsoft.Extensions.Configuration;
using PikaCore.Services.Helpers;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PikaCore.Services
{
    public class MediaService : IMediaService
    {
        private readonly IConfiguration _configuration;
        private readonly IFileLoggerService _fileLoggerService;
        private readonly IFileService _fileService;

        public MediaService(IConfiguration configuration,
                            IFileLoggerService fileLoggerService,
                            IFileService fileService)
        {
            _configuration = configuration;
            _fileLoggerService = fileLoggerService;
            _fileService = fileService;
        }

        public void Dispose()
        {
            //not used
        }

        public void Init()
        {
            //not used
        }

        public async Task<string> CreateThumb(string path, string guid, int size = 0)
        {
	        var physicalPath = _fileService.RetrieveAbsoluteFromSystemPath(path);
            var mime = MimeAssistant.GetMimeType(physicalPath);
	        _fileLoggerService.LogToFileAsync(Microsoft.Extensions.Logging.LogLevel.Information, "localhost" ,$"{path} : {mime}");
            var mediaType = DetectType(mime);
            switch(mediaType)
            {
                case MediaType.Image:
                    return await CreateThumbFromImageAsync(path, guid, size);
                case MediaType.Video:
                    return await CreateThumbFromVideoAsync(path, guid, size);
                default:
                    return string.Empty;
            }
        }

        private MediaType DetectType(string mime)
        {
            var props = (MediaType[])Enum.GetValues(typeof(MediaType));
            var mediaType = Array.Find(props, x => mime.Split("/").Contains(x.ToString().ToLower()));
	        _fileLoggerService.LogToFileAsync(Microsoft.Extensions.Logging.LogLevel.Information, "localhost", $"{mediaType} mediaType detected from MIME: {mime}");	   
            return mediaType;
        }

        private async Task<string> CreateThumbFromVideoAsync(string path, string guid, int size)
        {
            var absoluteHostPath = _fileService.RetrieveAbsoluteFromSystemPath(path);
            var thumbAbsolutePath = Path.Combine(_configuration.GetSection("Images")["ThumbDirectory"],
                                                $"{guid}.{_configuration.GetSection("Images")["Format"].ToLower()}");

            _fileLoggerService.LogToFileAsync(Microsoft.Extensions.Logging.LogLevel.Information, "localhost",
                    absoluteHostPath);
                if (File.Exists(thumbAbsolutePath)) return guid;

                var options = new ConversionOptions()
                {
                    Seek = TimeSpan.FromSeconds(int.Parse(_configuration.GetSection("ConversionOptions")["Seek"])),
                    CustomWidth = 128,
                    CustomHeight = 128
                };
                await GrabFromVideo(absoluteHostPath, thumbAbsolutePath, options);
                return guid;
        }

        private async Task<string> CreateThumbFromImageAsync(string path, string guid, int size)
        {
            //0 is small, 1 is big as in configuration: Images/Width, Images/Height, Images/BigHeigth, Images/BigWidth
            var wScale = int.Parse(_configuration.GetSection("Images")["Width"]);
            var hScale = int.Parse(_configuration.GetSection("Images")["Height"]);

            var absoluteThumbPath = Path.Combine(_configuration.GetSection("Images")["ThumbDirectory"],
                                                $"{guid}.{_configuration.GetSection("Images")["Format"].ToLower()}");
            _fileLoggerService.LogToFileAsync(Microsoft.Extensions.Logging.LogLevel.Information, "localhost", absoluteThumbPath);
            if (size == 1) 
            {
                hScale = int.Parse(_configuration.GetSection("Images")["HeightBig"]);
                wScale = int.Parse(_configuration.GetSection("Images")["WidthBig"]);
            }

            var absoluteHostPath = _fileService.RetrieveAbsoluteFromSystemPath(path);

            if (!File.Exists(absoluteThumbPath))
            {
                return await GrabFromImage(absoluteHostPath, guid, hScale, wScale);
            }
            return guid;
        }

        public async Task<string> GrabFromImage(string absoluteSystemPath, string id, int hScale, int wScale)
        {
            return await Task.Factory.StartNew(() =>
            {
                try
                {
                    using (var pngStream = new FileStream(absoluteSystemPath, FileMode.Open, FileAccess.Read))
                    {
                        _fileLoggerService.LogToFileAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                                                          "localhost", 
                                                          $"Filestream for {absoluteSystemPath} opened."
                                                          );
                        using (var image = new Bitmap(pngStream))
                        {
                            var resized = new Bitmap(image.Width/wScale, image.Height/hScale);
                            using (var graphics = Graphics.FromImage(resized))
                            {
                                _fileLoggerService.LogToFileAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                                                                  "localhost", 
                                                                  $"{absoluteSystemPath} loaded as Image."
                                                                  );

                                graphics.CompositingQuality = CompositingQuality.HighQuality;
                                graphics.InterpolationMode = InterpolationMode.High;
                                graphics.CompositingMode = CompositingMode.SourceCopy;
                                graphics.DrawImage(image, 0, 0, resized.Width, resized.Height);

                                var whereToSave = _configuration.GetSection("Images")["ThumbDirectory"];

                                var field = typeof(ImageFormat)
                                                            .GetProperties()
                                                            .First(f =>
                                                                   f.Name.Equals(_configuration.GetSection("Images")["Format"]));
                                var imageFormat = (ImageFormat)field.GetValue(field);

                                var name = $"{Path.Combine(whereToSave,id)}.{imageFormat.ToString().ToLower()}";
                                _fileLoggerService.LogToFileAsync(Microsoft.Extensions.Logging.LogLevel.Information, "localhost", $"Saving... {name}");

                                resized.Save(name, imageFormat);
                                _fileLoggerService.LogToFileAsync(Microsoft.Extensions.Logging.LogLevel.Information, "localhost", $"Saved... {name}");
				                resized.Dispose();
				                return id;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _fileLoggerService.LogToFileAsync(Microsoft.Extensions.Logging.LogLevel.Error, "localhost", e.Message);
                    return "";
                }
            });
        }

        public async Task GrabFromVideo(string absoluteSystemVideoPath, string absoluteSystemOutputPath, ConversionOptions conversionOptions)
        {
            var inputFile = new MediaFile(absoluteSystemVideoPath);
            var outputFile = new MediaFile(absoluteSystemOutputPath);

            var ffmpeg = new Engine(_configuration.GetSection("Images")["Ffmpeg"]);
            await ffmpeg.GetThumbnailAsync(inputFile, outputFile, conversionOptions);
        }
    }
}
