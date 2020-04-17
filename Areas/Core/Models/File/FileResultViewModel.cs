using System;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PikaCore.Controllers.Helpers;

namespace PikaCore.Models
{
    public class FileResultViewModel
    {
        public IDirectoryContents Contents { get; set; }
        public List<string> ToBeDeleted { get; set; }
        public List<IFileInfo> ContentsList { get; set; }

        public async Task SortContents()
        {
            var asyncFileEnum = await Task.Factory.StartNew(() => this.Contents.Where(entry => !entry.IsDirectory).OrderBy(predicate => predicate.Name));
            var asyncDirEnum = await Task.Factory.StartNew(() => this.Contents.Where(entry => entry.IsDirectory).OrderBy(predicate => predicate.Name));
            var resultList = new List<IFileInfo>();
            resultList.AddRange(asyncDirEnum);
            resultList.AddRange(asyncFileEnum);
            ContentsList = resultList;
        }

        public void ApplyAcl(string osUser)
        {
            this.ContentsList.RemoveAll(entry =>
                !UnixHelper.HasAccess(osUser, entry.PhysicalPath));
        }

        public void ApplyPaging(int offset, int count)
        {
            this.ContentsList = this.ContentsList.GetRange(offset, count);
        }
    }
}
