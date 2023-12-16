using System;
using System.Collections.Generic;
using System.Text;

namespace PikaCore.Areas.Core.Models.DTO;

public class CategoryDTO
{
    public Guid Guid { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Dictionary<string, List<string>> Tags { get; set; }

    public string TagsAsString(string bucketId)
    {
        var outputString = new StringBuilder();
        if (!Tags.ContainsKey(bucketId))
        {
            return string.Empty;
        }
        foreach (var tag in Tags[bucketId])
        {
            outputString.Append(tag);
            outputString.Append(';');
        }

        return outputString.ToString();
    }
}