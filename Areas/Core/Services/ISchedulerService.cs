using Quartz;
using System;
using System.Threading.Tasks;

namespace PikaCore.Services
{
    public interface ISchedulerService
    {
        Task Init();
        IJobDetail CreateJob(Type jobType);
        void StartJob(IJobDetail job, ITrigger trigger);
        void Dispose();
    }
}
