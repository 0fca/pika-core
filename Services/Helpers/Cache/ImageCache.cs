using Microsoft.Extensions.Caching.Memory;
using System;

namespace PikaCore.Services.Helpers
{
    public class ImageCache
    {
        public MemoryCache Cache { get; set; }


        public ImageCache()
        {
            Cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 1024,
                ExpirationScanFrequency = TimeSpan.FromMinutes(60)
            }); ;
        }
    }
}
