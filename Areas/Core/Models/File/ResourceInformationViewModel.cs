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

        public ResourceInformationViewModel(IFileInfo fileInfo)
        {
            this.FullName = fileInfo.Name;
            this.Size = fileInfo.Length;
            this.LastModified = fileInfo.LastModified.DateTime;
            this.MimeType = MimeAssistant.GetMimeType(fileInfo.PhysicalPath);
        }
    }
}