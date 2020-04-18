using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using PikaCore.Areas.Core.Controllers.Helpers;

namespace PikaCore.Areas.Infrastructure.Services.Helpers
{
    public static class FileSecurityHelper
    {
        private static readonly byte[] AllowedChars = { };

        public static string ProcessTemporaryStoredFile(
            IFileInfo temporaryFileInfo, ContentDispositionHeaderValue contentDisposition, 
            ModelStateDictionary modelState, IConfigurationSection permittedExtensions, long sizeLimit)
        {
            try
            {
                if (temporaryFileInfo.Length == 0)
                {
                    modelState.AddModelError("File", "The file is empty.");
                }
                else if (temporaryFileInfo.Length > sizeLimit)
                {
                    var megabyteSizeLimit = sizeLimit / 1048576;
                    modelState.AddModelError("File",
                        $"The file exceeds {megabyteSizeLimit:N1} MB.");
                }
                else if (!IsValidFileExtensionAndSignature(
                    temporaryFileInfo, 
                    permittedExtensions))
                {
                    modelState.AddModelError("File",
                        "The file type isn't permitted or the file's " +
                        "signature doesn't match the file's extension.");
                }
                else
                {
                    return temporaryFileInfo.PhysicalPath;
                }
            }
            catch (Exception ex)
            {
                modelState.AddModelError("File",
                    "The upload failed. Please contact the Help Desk " +
                    $" for support. Error: {ex.HResult}");
                // Log the exception
                
            }
            return null;
        }

        private static bool IsValidFileExtensionAndSignature(IFileInfo data, IConfiguration permittedExtensions)
        {
            if (string.IsNullOrEmpty(data.Name) || data.Length == 0)
            {
                return false;
            }

            var ext = Path.GetExtension(data.Name);

            if (string.IsNullOrEmpty(ext) || permittedExtensions[ext] == null)
            {
                return false;
            }

            var dataStream = data.CreateReadStream();
            
            dataStream.Position = 0;

            using var reader = new BinaryReader(dataStream);
            if (!MimeAssistant.GetMimeType(data.PhysicalPath).Contains("text"))
                return false;
            
            if (AllowedChars.Length == 0)
            {
                for (var i = 0; i < data.Length; i++)
                {
                    if (reader.ReadByte() > sbyte.MaxValue)
                    {
                        return false;
                    }
                }
            }
            else
            {
                for (var i = 0; i < data.Length; i++)
                {
                    var b = reader.ReadByte();
                    if (b > sbyte.MaxValue ||
                        !AllowedChars.Contains(b))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}