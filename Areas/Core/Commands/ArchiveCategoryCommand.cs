using System;
using MediatR;

namespace PikaCore.Areas.Core.Commands;

public class ArchiveCategoryCommand : IRequest<Guid>
{
    public ArchiveCategoryCommand(Guid id)
    {
        this.Id = id;
    }
    public Guid Id { get; set; } 
}