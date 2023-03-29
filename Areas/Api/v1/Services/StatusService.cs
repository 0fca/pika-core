using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pika.Domain.Status.Data;
using PikaCore.Areas.Api.v1.Services.Utils;
using PikaCore.Infrastructure.Services;

namespace PikaCore.Areas.Api.v1.Services
{
    public class StatusService : IStatusService
    {
        private readonly ISystemService _systemService;
        public StatusService(ISystemService systemService)
        {
            _systemService = systemService;
        }

        public async Task<Dictionary<string, bool>> CheckAllSystems()
        {
            var systemDescriptors = await _systemService.GetAll();
            var resultDictionary = new Dictionary<string, bool>();
            systemDescriptors.ForEach(async d =>
            {
                resultDictionary.Add(d.SystemName, await Pinger.Ping($"{d.Address}", d.Port));
            });
            return resultDictionary;
        }

        public async Task<bool> CheckSpecificSystem(SystemDescriptor descriptor)
        {
            return await Pinger.Ping($"{descriptor.Address}", descriptor.Port);
        }
    }
}