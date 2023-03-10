using System;
using System.Collections.Generic;
using MediatR;

namespace PikaCore.Areas.Core.Commands;

public class UpdateCategoryCommand : IRequest
{
    public Guid Guid { get; set; }
    public string Name { get; set; }
    public List<string> Mimes { get; set; }

    public string Description { get; set; } = "";
}