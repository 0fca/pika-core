using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PikaCore.Areas.Core.Exceptions;

namespace PikaCore.Infrastructure.Services
{
    //TODO: Add logging here.
    public class StaticContentService : IStaticContentService
    {
        private readonly IFileService _fileService;
        private readonly IConfiguration _configuration;
        
        public StaticContentService(IFileService fileService, 
                                    IConfiguration configuration)
        {
            _fileService = fileService;
            _configuration = configuration;
        }

        public async Task<string> CopyToCdn(string physicalPath)
        {
            var cdnDirectory = _configuration.GetSection("Storage")["staticFiles"];
            var id = string.Concat(Guid.NewGuid().ToString(), Path.GetExtension(physicalPath));
            await _fileService.Copy(physicalPath, Path.Combine(cdnDirectory, id));
            return id;
        }

        public void RemoveFromCdn(string id)
        {
            var cdnDirectory = _configuration.GetSection("Storage")["staticFiles"];
            var fileList = new List<string>
            {
                Path.Combine(cdnDirectory, id)
            };
            _fileService.Delete(fileList);
        }

        public void CleanCdn()
        {
            var cdnDirectory = _configuration.GetSection("Storage")["staticFiles"];
            var files = Directory.GetFiles(cdnDirectory);
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }

        public bool IsInCdn(string physicalPath)
        {
            if (!File.Exists(physicalPath))
            {
                throw new InvalidPathException("This file does not exist or the path points to directory.");
            }
            
            var cdnDirectory = _configuration.GetSection("Storage")["staticFiles"];
            return Directory.EnumerateFiles(cdnDirectory).Any(file =>
                _fileService.IsSameFile(physicalPath, file)
                );
        }

        public string RetrieveFromCdn(string physicalPath)
        {
            if (!File.Exists(physicalPath))
            {
                throw new InvalidPathException("This file does not exist or the path points to directory.");
            }
            
            var cdnDirectory = _configuration.GetSection("Storage")["staticFiles"];
            return Path.GetFileName(Directory.EnumerateFiles(cdnDirectory).First(file =>
                _fileService.IsSameFile(physicalPath, file)
            ));
        }
    }
}