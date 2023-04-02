using System;

namespace PikaCore.Areas.Core.Models.File;

public class ObjectInfo
{
    public string Name { get; set; }
    public bool IsDir { get; set; } = false;
    public DateTime LastModified { get; set; }
    public string ETag { get; set; }
    
    public ulong Size { get; set; }
    
    public string MimeType { get; set; }
    public ulong GetSizeInGigaBytes()
    {
        return (ulong)(this.Size / Math.Pow(10,9));
    }
    public ulong GetSizeInMegaBytes()
    {
        return (ulong)(this.Size / Math.Pow(10,6));
    }

    public ulong GetSizeInKiloBytes()
    {
        return (ulong)(this.Size / Math.Pow(10, 3));
    }
    
    public string SizeWithUnit()
    {
        if (this.Size < Math.Pow(10, 6))
        {
            return $"{this.GetSizeInKiloBytes()} kB";
        }
        if (this.Size > Math.Pow(10, 6) && this.Size < Math.Pow(10, 9))
        { 
            return $"{this.GetSizeInMegaBytes()} MB";
        }
        if (this.Size > Math.Pow(10, 9))
        {
            return $"{this.GetSizeInMegaBytes()} GB";
        }

        return $"{this.Size}B";
    }
}