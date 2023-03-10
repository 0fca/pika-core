using System.Collections.Generic;
using PikaCore.Areas.Core.Models.DTO;

namespace PikaCore.Areas.Core.Models;

public class IndexViewModel
{
    public List<CategoryDTO> Categories { get; set; } = new();
}