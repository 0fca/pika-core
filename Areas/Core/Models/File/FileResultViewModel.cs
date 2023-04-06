using System.Collections.Generic;

namespace PikaCore.Areas.Core.Models.File
{
    public class FileResultViewModel
    {
        public List<ObjectInfo> Objects { get; set; } = new();
        public string? SelectedTag { get; set; }
        public int SelectedPage { get; set; }
        public int PerPage { get; set; }
        public int Total { get; set; }
        public int OnePageMaxTotal { get; set; }
        public List<string> Tags { get; set; }
        public string BucketId { get; set; }
        public string CategoryId { get; set; }
    }
}
