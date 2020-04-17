using System;
using System.Threading.Tasks;
using Quartz;

namespace PikaCore.Areas.Core.Services
{
    public interface ISchedulerService
    {
        Task Init();
        IJobDetail CreateJob(Type jobType);
        void StartJob(IJobDetail job, ITrigger trigger);
        void Dispose();
    }
}
