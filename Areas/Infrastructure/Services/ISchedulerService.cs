using System;
using System.Threading.Tasks;
using Quartz;

namespace PikaCore.Areas.Infrastructure.Services
{
    public interface ISchedulerService
    {
        Task Init();
        Task StartJob(string name, ITrigger trigger);
        void Dispose();
    }
}
