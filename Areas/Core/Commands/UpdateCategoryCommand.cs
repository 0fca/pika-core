using System;
using System.Collections.Generic;
using MediatR;

namespace PikaCore.Areas.Core.Commands;

public class UpdateCategoryCommand : IRequest<Unit>
{
    public Guid Guid { get; set; }
    public string Name { get; set; }
    public List<string> Mimes { get; set; }

    public string Description { get; set; } = "";
    
    public Dictionary<string, List<string>>? Tags { get; set; }
}