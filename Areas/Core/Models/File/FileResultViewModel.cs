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
        public static FileResultViewModel FromMinioItems(IEnumerable<Item> items)
        {
            var objects = new List<ObjectInfo>();
            items.ToList().ForEach(i =>
            {
                objects.Add(new ObjectInfo()
                {
                    Name = i.Key,
                    IsDir = i.IsDir,
                    LastModified = i.LastModifiedDateTime ?? DateTime.Now,
                    ETag = i.ETag,
                    Size = i.Size
                });
            });
            return new FileResultViewModel()
            {
                Objects = objects
            };
        } 
    }
}
