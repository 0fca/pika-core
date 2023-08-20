using System;
using System.Collections.Generic;
using System.IO;
using System.Security;

namespace PikaCore.Infrastructure.Adapters.Filesystem
{
    public static class FileSecurityHelper
    {
        public static void ProcessTemporaryStoredFile(
            string originalEncodedName,
            Stream fileStream,
            List<string> permittedExtensions,
            List<string> permittedMimes
        )
        {
            if (fileStream == null || permittedMimes.Count == 0 || permittedExtensions.Count == 0)
            {
                throw new SecurityException(
                    "Invalid configuration or stream, cannot validate file contents, aborting"
                );
            }

            try
            {
                if (fileStream.Length == 0)
                {
                    throw new SecurityException("The file is empty.");
                }

                if (!IsValidFileExtensionAndMime(originalEncodedName,
                        fileStream,
                        permittedExtensions,
                        permittedMimes))
                {
                    throw new SecurityException(
                        "The file type isn't permitted or the file's MIME type doesn't match the file's extension."
                    );
                }
            }
            catch (Exception ex)
            {
                throw new SecurityException($"Sanitization failed: {ex.Message}");
            }
        }

        private static bool IsValidFileExtensionAndMime(string originalEncodedName,
            Stream data,
            ICollection<string> permittedExtensions,
            ICollection<string> permittedMimes)
        {
            if (string.IsNullOrEmpty(originalEncodedName) || data.Length == 0)
            {
                return false;
            }

            var ext = Path.GetExtension(originalEncodedName);
            var mime = MimeTypes.GetMimeType(originalEncodedName);

            return !string.IsNullOrEmpty(ext) && (permittedExtensions.Contains(ext) && permittedMimes.Contains(mime));
        }
    }
}