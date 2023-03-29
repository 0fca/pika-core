using System;
using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;
using PikaCore.Infrastructure.Services.Helpers;

namespace PikaCore.Areas.Core.Models.File
{
    public class ResourceInformationViewModel
    {
        public string FullName { get; set; }
        public string MimeType { get; set; }
        public long Size { get; set; } = 0;
        public DateTime LastModified;
        public string CategoryId { get; set; }
        public string BucketId { get; set; }
        public string ETag { get; set; }

        public ResourceInformationViewModel()
        {
            this.FullName = "N"; 
            this.Size = 0L;
            this.LastModified = DateTime.Now;
            this.MimeType = "";
        }
    }
}