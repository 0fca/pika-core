using System.Collections.Generic;
using System.Threading.Tasks;
using Pika.Domain.Status.Data;

namespace PikaCore.Areas.Api.v1.Services;

public interface IStatusService
{
    Task<Dictionary<string, bool>> CheckAllSystems();
    Task<bool> CheckSpecificSystem(SystemDescriptor descriptor);
}