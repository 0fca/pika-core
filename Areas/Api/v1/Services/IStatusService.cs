using System.Collections.Generic;
using System.Threading.Tasks;
using PikaCore.Infrastructure.Data;

namespace PikaCore.Areas.Api.v1.Services
{
    public interface IStatusService
    {
        Task<Dictionary<string, bool>> CheckAllSystems();
        Task<bool> CheckSpecificSystem(SystemDescriptor descriptor);
    }
}