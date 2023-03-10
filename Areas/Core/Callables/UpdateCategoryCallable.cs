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

public class UpdateCategoryCallable : BaseJobCallable
{
    private readonly IMediator _mediator;
    private readonly IDistributedCache _distributedCache;
    public UpdateCategoryCallable(IMediator mediator,
        IDistributedCache distributedCache)
    {
        this._mediator = mediator;
        this._distributedCache = distributedCache;
    }

    public override async Task Execute(Dictionary<string, ParameterValueType>? parameterValueTypes)
    {
        var serializedIds = await _distributedCache.GetStringAsync("category.streamids");
        var streamIds = new List<Guid>();
        if (!string.IsNullOrEmpty(serializedIds))
        {
            streamIds = JsonSerializer.Deserialize<List<Guid>>(serializedIds);
        }

        switch (parameterValueTypes)
        {
            case { Count: 0 }:
                throw new ArgumentException("Parameters are not null, but still empty!");
            case null:
                throw new ApplicationException("Create category job needs parameters: Name, Mimes.");
        }

        var c = new UpdateCategoryCommand
        {
            Name = parameterValueTypes["Name"].Value<string>(),
            Mimes = parameterValueTypes["Mimes"].Value<List<string>>(),
        }; 
        await _mediator.Send(c);

        await _distributedCache.SetAsync("category.streamids",
            JsonSerializer.SerializeToUtf8Bytes(streamIds));
    }
}