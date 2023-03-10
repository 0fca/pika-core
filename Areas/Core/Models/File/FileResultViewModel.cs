using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders;
using Minio.DataModel;

namespace PikaCore.Areas.Core.Models.File
{
    public class FileResultViewModel
    {
        public List<string> ToBeDeleted { get; set; } = new List<string>();
        public List<ObjectInfo> Objects { get; set; } = new List<ObjectInfo>();

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
