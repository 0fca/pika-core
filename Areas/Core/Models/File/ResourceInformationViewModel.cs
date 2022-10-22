using System;
using Microsoft.Extensions.FileProviders;
using PikaCore.Infrastructure.Services.Helpers;

namespace PikaCore.Areas.Core.Models.File
{
    public class ResourceInformationViewModel
    {
        public string FullName { get; set; }
        public string MimeType { get; set; }
        public bool IsHidden { get; set; }
        public long Size { get; set; } = 0;
        
        public DateTime LastModified;

        public ResourceInformationViewModel()
        {
            this.FullName = "N"; 
            this.Size = 0L;
            this.LastModified = DateTime.Now;
            this.MimeType = "";
        }
    }
}