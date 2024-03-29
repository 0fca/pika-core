﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hangfire;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Pika.Domain.Storage.Callables;
using Pika.Domain.Storage.Callables.ValueTypes;
using PikaCore.Areas.Core.Commands;
using PikaCore.Areas.Core.Queries;

namespace PikaCore.Areas.Core.Callables;

public class UpdateCategoryCallable : BaseJobCallable
{
    private readonly IMediator _mediator;
    private readonly IDistributedCache _cache;

    public UpdateCategoryCallable(IMediator mediator, IDistributedCache cache): base(cache)
    {
        this._mediator = mediator;
        this._cache = cache;
    }

    public override async Task Execute(Dictionary<string, ParameterValueType>? parameterValueTypes)
    {
        if (parameterValueTypes == null)
        {
            await ExecuteMultipleUpdates();
        }
        else
        {
            await ExecuteSingleUpdate(parameterValueTypes);
        }
    }

    private async Task ExecuteSingleUpdate(Dictionary<string, ParameterValueType>? parameterValueTypes)
    {
        switch (parameterValueTypes)
        {
            case { Count: 0 }:
                throw new ArgumentException("Parameters are not null, but still empty!");
            case null:
                throw new ApplicationException("Create category job needs parameters: Id, Name, Mimes, Tags");
        }
        var updateCommand = MapParameterTypeValueDictToUpdateCommand(parameterValueTypes);
        if (await CheckIfUpdateNeeded(updateCommand.Guid))
        {
            await _mediator.Send(updateCommand);
            await UpdateCategoryHash(updateCommand.Guid);
        }
        await _cache.RemoveAsync("update.categories.parameters");
    }

    private async Task ExecuteMultipleUpdates()
    {
        var updateCategoriesParamsString = await _cache.GetStringAsync("update.categories.parameters");
        var jobId = await _cache.GetStringAsync("update.job.identifier");
        if (string.IsNullOrEmpty(updateCategoriesParamsString))
        {
            BackgroundJob.Delete(jobId);
            throw new InvalidOperationException("Cannot run multiple update without data in relay cache");
        }

        var parameterDictList = JsonConvert
            .DeserializeObject<List<Dictionary<string, ParameterValueType>>>(updateCategoriesParamsString); 
        foreach (var parameterDict in parameterDictList)
        {
            await ExecuteSingleUpdate(parameterDict);
        }
    }

    private static UpdateCategoryCommand MapParameterTypeValueDictToUpdateCommand(
        IReadOnlyDictionary<string, ParameterValueType> parameterValueTypes
    )
    {
        return new UpdateCategoryCommand
        {
            Guid = parameterValueTypes["Id"].Value<Guid>(),
            Name = parameterValueTypes["Name"].Value<string>(),
            Mimes = parameterValueTypes["Mimes"].Value<List<string>>(),
            Tags = parameterValueTypes["Tags"].Value<Dictionary<string, List<string>>>()
        }; 
    }

    private async Task<bool> CheckIfUpdateNeeded(Guid guid)
    {
        var category = await _mediator.Send(new GetCategoryByIdQuery(guid));
        var categoryHash = category.NormalizedHash(); 
        var cachedHash = await _cache.GetStringAsync($"{guid}.category.hash");
        return string.IsNullOrEmpty(cachedHash) 
               || !cachedHash.Normalize().ToUpper().Equals(categoryHash);
    }

    private async Task UpdateCategoryHash(Guid guid)
    {
        var category = await _mediator.Send(new GetCategoryByIdQuery(guid));
        await _cache.SetStringAsync($"{guid}.category.hash", category.NormalizedHash());
    }
}