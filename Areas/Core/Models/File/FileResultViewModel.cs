using System;
using System.Collections.Generic;
using System.Linq;
using Minio.DataModel;

namespace PikaCore.Areas.Core.Models.File
{
    public class FileResultViewModel
    {
        public List<ObjectInfo> Objects { get; set; } = new();
        public string? SelectedTag { get; set; }
        public List<string> Tags { get; set; }
        public string BucketId { get; set; }
        public string CategoryId { get; set; }
    }
}
