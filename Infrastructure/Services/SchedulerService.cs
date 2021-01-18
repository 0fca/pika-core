using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;

namespace PikaCore.Infrastructure.Services
{
    public class SchedulerService : ISchedulerService
    {
        private StdScheduler? _scheduler;
        private IJobService _jobService;
        
        public SchedulerService(IJobService jobService)
        {
            _jobService = jobService;
        }
        
        public void Dispose()
        {
            if(_scheduler == null)
                throw new ApplicationException("Invalid state: Not initialized.");
            _scheduler.Clear();
        }

        public async Task Init()
        {
            NameValueCollection props = new NameValueCollection
            {
                { "quartz.serializer.type", "binary" }
            };

            StdSchedulerFactory factory = new StdSchedulerFactory(props);

            IScheduler sched = await factory.GetScheduler();
            await sched.Start();
            _scheduler = (StdScheduler)sched;
        }

        public async Task StartJob(string name, ITrigger trigger)
        {
            if(_scheduler == null)
                throw new ApplicationException("Invalid state: Not initialized.");
            var job = await _jobService.GetByName(name);
            await _scheduler.ScheduleJob(job, trigger);
        }
    }
}
