using System;

namespace PikaCore.Areas.Core.Models.File;

public class ObjectInfo
{
    public string Name { get; set; }
    public bool IsDir { get; set; } = false;
    public DateTime LastModified { get; set; }
    public string ETag { get; set; }
    
    public ulong Size { get; set; }
}