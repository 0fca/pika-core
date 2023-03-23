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
        return this.Size < Math.Pow(10, 6) 
            ? $"{this.GetSizeInKiloBytes()} kB" 
            : $"{this.GetSizeInMegaBytes()} MB";
    }
}