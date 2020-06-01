using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using PikaCore.Areas.Infrastructure.Services.Helpers;

namespace PikaCore.Areas.Core.Models.File
{
    public class FileResultViewModel
    {
        public IDirectoryContents Contents { get; set; } = new NotFoundDirectoryContents();
        public List<string> ToBeDeleted { get; set; } = new List<string>();
        public List<IFileInfo> ContentsList { get; set; } = new List<IFileInfo>();

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
                !UnixHelper.HasAccess(osUser, entry.PhysicalPath) 
                || entry.Name.StartsWith("~"));
        }

        public void ApplyPaging(int offset, int count)
        {
            this.ContentsList = this.ContentsList.GetRange(offset, count);
        }
    }
}
