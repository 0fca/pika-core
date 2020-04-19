using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.FileProviders;

namespace PikaCore.Areas.Core.Models.File
{
    public class DeleteResourcesViewModel
    {
        [MinLength(1)]
        [MaxLength(50)]
        public List<SelectListItem> ResourceList { get; set; } = new List<SelectListItem>();

        [MinLength(1)]
        public List<string> ToBeDeletedItems { get; } = new List<string>();

        public void FromFileInfoList(List<IFileInfo> fileInfos)
        {
            fileInfos.ForEach(x =>
            {
                ResourceList.Add(new SelectListItem()
                {
                    Text   = x.Name,
                    Value  = x.PhysicalPath
                });
            });
        }

        public void ApplyPaging(int offset, int count)
        {
            count = count > ResourceList.Count ? ResourceList.Count : count;
            this.ResourceList.GetRange(offset, count);
        }
    }
}
