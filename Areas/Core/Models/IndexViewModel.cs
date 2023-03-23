using System;
using System.Collections.Generic;
using PikaCore.Areas.Core.Models.DTO;

namespace PikaCore.Areas.Core.Models;

public class IndexViewModel
{
    public Guid? CurrentBucketId { get; set; } = null;
    public string CurrentBucketName { get; set; } = "";
    public List<CategoryDTO> Categories { get; set; } = new();

    public List<BucketDTO> Buckets { get; set; } = new();
}