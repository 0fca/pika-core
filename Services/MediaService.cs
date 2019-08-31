using FMS2.Controllers;
using FMS2.Controllers.Helpers;
using FMS2.Services;
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
        private readonly IFileService _fileService;
        private readonly ImageCache _cache;
        private readonly IFileLoggerService _fileLoggerService;

        public MediaService(IConfiguration configuration,
                            IFileService fileService,
                            ImageCache memoryCache,
                            IFileLoggerService fileLoggerService)
        {
            _configuration = configuration;
            _fileService = fileService;
            _cache = memoryCache;
            _fileLoggerService = fileLoggerService;
        }

        public void Dispose()
        {
            //not used
        }

        public void Init()
        {
            //not used
        }

        public async Task<string> CreateThumb(string path, string guid, MediaType mediaType)
        {
            if (mediaType == MediaType.Image)
            {
                return await CreateThumbFromImageAsync(path, guid);
            }

            return string.Empty;
        }

        private async Task<string> CreateThumbFromImageAsync(string path, string guid)
        {
            var absoluteHostPath = UnixHelper.MapToPhysical(Constants.FileSystemRoot, path);
            var thumbAbsolutePath = Path.Combine(_configuration.GetSection("Images")["ThumbDirectory"],
                                                $"{guid}.{_configuration.GetSection("Images")["Format"].ToLower()}");

            if (!File.Exists(thumbAbsolutePath))
            {
                return await Scale(absoluteHostPath, guid, int.Parse(_configuration.GetSection("Images")["Width"]), int.Parse(_configuration.GetSection("Images")["Height"]));
            }
            return guid;
        }

        public async Task<string> Scale(string absoluteSystemPath, string id, int height, int width)
        {
            return await Task.Factory.StartNew(() =>
            {
                try
                {
                    using (FileStream pngStream = new FileStream(absoluteSystemPath, FileMode.Open, FileAccess.Read))
                    {
                        _fileLoggerService.LogToFileAsync(Microsoft.Extensions.Logging.LogLevel.Information, "localhost", $"Filestream for {absoluteSystemPath} opened.");
                        using (var image = new Bitmap(pngStream))
                        {
                            var resized = new Bitmap(width, height);
                            using (var graphics = Graphics.FromImage(resized))
                            {
                                _fileLoggerService.LogToFileAsync(Microsoft.Extensions.Logging.LogLevel.Information, "localhost", $"{absoluteSystemPath} loaded as Image.");

                                graphics.CompositingQuality = CompositingQuality.HighQuality;
                                graphics.InterpolationMode = InterpolationMode.Bicubic;
                                graphics.CompositingMode = CompositingMode.SourceCopy;
                                graphics.DrawImage(image, 0, 0, width, height);

                                var whereToSave = _configuration.GetSection("Images")["ThumbDirectory"];

                                var field = typeof(ImageFormat)
                                                            .GetProperties()
                                                            .First(f =>
                                                                   f.Name.Equals(_configuration.GetSection("Images")["Format"]));
                                var imageFormat = (ImageFormat)field.GetValue(field);

                                var name = $"{Path.Combine(whereToSave, id)}.{imageFormat.ToString().ToLower()}";
                                _fileLoggerService.LogToFileAsync(Microsoft.Extensions.Logging.LogLevel.Information, "localhost", $"Saving... {name}");

                                resized.Save(name, imageFormat);

                                _fileLoggerService.LogToFileAsync(Microsoft.Extensions.Logging.LogLevel.Information, "localhost", $"Saved... {name}");
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
    }
}
