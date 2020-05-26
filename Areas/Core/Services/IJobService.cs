using System.Collections.Generic;
using System.Threading.Tasks;
using Quartz;

namespace PikaCore.Areas.Core.Services
{
    public interface IJobService
    {
        Task<string> CreateJob<T>(JobDataMap jobDataMap) where T : IJob;
        public Task RemoveJob<T>(string name) where T : IJob;
        Task<IList<IJobDetail>> FindJobsByUser(string id);
        Task<IList<IJobDetail>> GetAll();
        Task PurgeJobs();
    }
}