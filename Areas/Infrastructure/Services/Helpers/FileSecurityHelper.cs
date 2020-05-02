using System;
using System.Collections.Generic;
using System.IO;
using PikaCore.Areas.Core.Controllers.Helpers;

namespace PikaCore.Areas.Infrastructure.Services.Helpers
{
    public static class FileSecurityHelper
    {
        public static string? ProcessTemporaryStoredFile(string originalEncodedName,
            FileStream? fileStream, 
            List<string> permittedExtensions,
            List<string> permittedMimes,
            long sizeLimit,
            bool isAdmin)
        {
            if (fileStream == null)
            {
                return "Error during sanitizing files.";
            }

            try
            {
                if (fileStream.Length == 0)
                {
                   return "The file is empty.";
                }

                if (fileStream.Length > sizeLimit)
                {
                    var megabyteSizeLimit = sizeLimit / 1048576;
                    return $"The file exceeds {megabyteSizeLimit:N1} MB.";
                }
                if (!IsValidFileExtensionAndMime(originalEncodedName,
                    fileStream, 
                    permittedExtensions,
                    permittedMimes)
                && !isAdmin)
                {
                    return "The file type isn't permitted or the file's MIME type doesn't match the file's extension.";
                }
                return null;
            }
            catch (Exception ex)
            {
                return "The upload failed." + $" Error: {ex.HResult}";
            }
        }

        private static bool IsValidFileExtensionAndMime(string originalEncodedName,
            FileStream data, 
            ICollection<string> permittedExtensions,
            ICollection<string> permittedMimes)
        {
            if (string.IsNullOrEmpty(originalEncodedName) || data.Length == 0)
            {
                return false;
            }

            var ext = Path.GetExtension(originalEncodedName);
            var mime = MimeAssistant.GetMimeType(data.Name);
            
            return !string.IsNullOrEmpty(ext) && (permittedExtensions.Contains(ext) && permittedMimes.Contains(mime));
        }
    }
}