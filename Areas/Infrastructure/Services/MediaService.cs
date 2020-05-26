using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FFmpeg.NET;
using Microsoft.Extensions.Configuration;
using PikaCore.Areas.Core.Controllers.Helpers;
using PikaCore.Areas.Infrastructure.Services.Helpers;
using Serilog;

namespace PikaCore.Areas.Infrastructure.Services
{
    public class MediaService : IMediaService
    {
        private readonly IConfiguration _configuration;
        private readonly IFileService _fileService;

        public MediaService(IConfiguration configuration,
            IFileService fileService)
        {
            _configuration = configuration;
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
            var mediaType = DetectType(mime);
            return mediaType switch
            {
                MediaType.Image => await CreateThumbFromImageAsync(physicalPath, guid, size),
                MediaType.Video => await CreateThumbFromVideoAsync(physicalPath, guid, size),
                _ => string.Empty
            };
        }

        private static MediaType DetectType(string mime)
        {
            var props = (MediaType[])Enum.GetValues(typeof(MediaType));
            var mediaType = Array.Find(props, x => mime.Split("/").Contains(x.ToString().ToLower()));
            return mediaType;
        }

        private async Task<string> CreateThumbFromVideoAsync(string absoluteHostPath, string guid, int size)
        {
            var thumbAbsolutePath = Path.Combine(_configuration.GetSection("Images")["ThumbDirectory"],
                                                $"{guid}.{_configuration.GetSection("Images")["Format"].ToLower()}");
            var wScale = int.Parse(_configuration.GetSection("Images")["Width"]);
                var hScale = int.Parse(_configuration.GetSection("Images")["Height"]);
                
                if (size == 1) 
                {
                    hScale = int.Parse(_configuration.GetSection("Images")["HeightBig"]);
                    wScale = int.Parse(_configuration.GetSection("Images")["WidthBig"]);
                }
                
                var options = new ConversionOptions()
                {
                    Seek = TimeSpan.FromSeconds(int.Parse(_configuration.GetSection("ConversionOptions")["Seek"])),
                    CustomWidth = wScale,
                    CustomHeight = hScale
                };
                
                if (!File.Exists(thumbAbsolutePath))
                {
                    Log.Information($"Not found thumb from image {absoluteHostPath} as {thumbAbsolutePath}");
                    await GrabFromVideo(absoluteHostPath, thumbAbsolutePath, options);
                }
                Log.Information($"Returning thumb: {thumbAbsolutePath}");
                return guid;
        }

        private async Task<string> CreateThumbFromImageAsync(string absoluteHostPath, string guid, int size)
        {
            //0 is small, 1 is big as in configuration: Images/Width, Images/Height, Images/BigHeight, Images/BigWidth
            var wScale = int.Parse(_configuration.GetSection("Images")["Width"]);
            var hScale = int.Parse(_configuration.GetSection("Images")["Height"]);

            var absoluteThumbPath = Path.Combine(_configuration.GetSection("Images")["ThumbDirectory"],
                                                $"{guid}.{_configuration.GetSection("Images")["Format"].ToLower()}");
            
            if (size == 1) 
            {
                hScale = int.Parse(_configuration.GetSection("Images")["HeightBig"]);
                wScale = int.Parse(_configuration.GetSection("Images")["WidthBig"]);
            }

            if (!File.Exists(absoluteThumbPath))
            {
                Log.Information($"Not found thumb from image {absoluteHostPath} as {absoluteThumbPath}");
                return await GrabFromImage(absoluteHostPath, guid, hScale, wScale);
            }
            Log.Information($" Returning thumb: {absoluteThumbPath}");
            return guid;
        }

        public async Task<string> GrabFromImage(string absoluteSystemPath, string id, int hScale, int wScale)
        {
            return await Task.Factory.StartNew(() =>
            {
                try
                {
                    using var pngStream = new FileStream(absoluteSystemPath, FileMode.Open, FileAccess.Read);
                    using var image = new Bitmap(pngStream);
                    var resized = new Bitmap(image.Width/wScale, image.Height/hScale);
                    using var graphics = Graphics.FromImage(resized);
                    

                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.Bicubic;
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.DrawImage(image, 0, 0, resized.Width, resized.Height);

                    var whereToSave = _configuration.GetSection("Images")["ThumbDirectory"];

                    var field = typeof(ImageFormat)
                        .GetProperties()
                        .First(f =>
                            f.Name.Equals(_configuration.GetSection("Images")["Format"]));
                    var imageFormat = field.GetValue(field);

                    if (imageFormat != null)
                    {
                        var name = $"{Path.Combine(whereToSave,id)}.{imageFormat.ToString().ToLower()}";
                        resized.Save(name, (ImageFormat)imageFormat);
                        Log.Information($"Thumb saved: {name}");
                    }
                    resized.Dispose();
                    return id;
                }
                catch (Exception e)
                {
                    Log.Error(e, "MediaService#GrabFromImage");
                    return "";
                }
            });
        }

        public async Task GrabFromVideo(string absoluteSystemVideoPath, string absoluteSystemOutputPath, ConversionOptions conversionOptions)
        {
            try
            {
                var inputFile = new MediaFile(absoluteSystemVideoPath);
                var outputFile = new MediaFile(absoluteSystemOutputPath);

                var ffmpeg = new Engine(_configuration.GetSection("Images")["Ffmpeg"]);
                var mediaFile = await ffmpeg.GetThumbnailAsync(inputFile, outputFile, conversionOptions);
                Log.Information($"Thumb from video: {mediaFile.FileInfo.FullName} : {mediaFile.FileInfo.Exists}");
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
            }
        }
    }
}
