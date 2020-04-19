using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;

namespace PikaCore.Areas.Infrastructure.Services
{
    public class SchedulerService : ISchedulerService
    {
        private StdScheduler scheduler;

        public IJobDetail CreateJob(Type jobType)
        {
            return JobBuilder.Create(jobType).Build();
        }

        public void Dispose()
        {
            scheduler.Clear();
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
            scheduler = (StdScheduler)sched;
        }

        public void StartJob(IJobDetail job, ITrigger trigger)
        {
            scheduler.ScheduleJob(job, trigger);
        }
    }
}
