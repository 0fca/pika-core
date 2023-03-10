using System;
using System.Collections.Generic;

namespace PikaCore.Areas.Core.Models.DTO;

public class CategoryDTO
{
    public Guid Guid { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> Tags { get; set; }
}