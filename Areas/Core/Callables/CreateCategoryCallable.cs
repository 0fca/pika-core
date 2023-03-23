using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Pika.Domain.Storage.Callables;
using Pika.Domain.Storage.Callables.ValueTypes;
using PikaCore.Areas.Core.Commands;

namespace PikaCore.Areas.Core.Callables;

public class CreateCategoryCallable : BaseJobCallable
{
    private readonly IMediator _mediator;
    public CreateCategoryCallable(IMediator mediator)
    {
        this._mediator = mediator;
    }

    public override async Task Execute(Dictionary<string, ParameterValueType>? parameterValueTypes)
    {
        switch (parameterValueTypes)
        {
            case { Count: 0 }:
                throw new ArgumentException("Parameters are not null, but still empty!");
            case null:
                throw new ApplicationException("Create category job needs parameters: Name, Mimes.");
        }

        var c = new CreateCategoryCommand
        {
            Guid = Guid.NewGuid(),
            Name = parameterValueTypes["Name"].Value<string>(),
            Description = parameterValueTypes["Description"].Value<string>(),
            Mimes = parameterValueTypes["Mimes"].Value<List<string>>(),
        }; 
        await _mediator.Send(c);
    }
}