﻿using System.Collections.Generic;

namespace PikaCore.Areas.Core.Models.DTO;

public class BucketDTO
{
    public string Name { get; set; }
    public List<string> Roles { get; set; }
}