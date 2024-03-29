﻿using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Pika.Domain.Storage.Entity;
using Pika.Domain.Storage.Repository;

namespace PikaCore.Areas.Core.Commands;

public class ArchiveCategoryHandler : IRequestHandler<ArchiveCategoryCommand, Guid>
{
    private readonly AggregateRepository _aggregateRepository;
    public ArchiveCategoryHandler(AggregateRepository aggregateRepository)
    {
        this._aggregateRepository = aggregateRepository;
    }
    
    public async Task<Guid> Handle(ArchiveCategoryCommand request, CancellationToken cancellationToken)
    {
        return await _aggregateRepository.Archive<Category>(request.Id, cancellationToken); 
    } 
}