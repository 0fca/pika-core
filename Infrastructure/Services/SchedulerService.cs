using System;
using System.Collections.Specialized;
using System.Threading.Tasks;


namespace PikaCore.Infrastructure.Services
{
    public class SchedulerService : ISchedulerService
    {
        public void Dispose()
        {

        }

        public async Task Init()
        {
            NameValueCollection props = new NameValueCollection
            {
                { "quartz.serializer.type", "binary" }
            };
        }

        public async Task StartJob(string name)
        {
        }
    }
}
