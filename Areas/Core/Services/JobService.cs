using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Quartz;

namespace PikaCore.Areas.Core.Services
{
    public class JobService : IJobService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly Dictionary<string, string> _jobToUser = new Dictionary<string, string>();
        
        public JobService(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        public async Task<string> CreateJob<T>(JobDataMap jobDataMap) where T : IJob
        {
            var name = Guid.NewGuid().ToString();
            var jobDetail = JobBuilder.Create<T>()
                .WithIdentity(name)
                .SetJobData(jobDataMap)
                .Build();
            await _distributedCache.SetAsync(name, System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(jobDetail)));
            var userId = jobDataMap.Get("userId").ToString();
            if(string.IsNullOrEmpty(userId))
                throw new ArgumentException("JobDataMap must contain value with key userId which is an actual user's id.");
            
            _jobToUser.Add(name, userId);
            return name;
        }

        public async Task RemoveJob<T>(string name) where T : IJob
        {
            await _distributedCache.RemoveAsync(name);
        }

        public async Task<IList<IJobDetail>> FindJobsByUser(string id)
        {
            return await Task.Factory.StartNew(() =>
            {
                var list = _jobToUser.Where((jobToUserPair) => jobToUserPair.Value.Equals(id)).ToList();
                var r = new List<IJobDetail>();
                list.ForEach(l =>
                {
                    r.Add(JsonConvert.DeserializeObject<IJobDetail>(
                        System.Text.Encoding.UTF8.GetString(_distributedCache.Get(l.Key))));
                });
                return r;
            });
        }

        public async Task<IList<IJobDetail>> GetAll()
        {
            return await Task.Factory.StartNew(() =>
            {
                var list = _jobToUser.ToList();
                var r = new List<IJobDetail>();
                list.ForEach(l =>
                {
                    r.Add(JsonConvert.DeserializeObject<IJobDetail>(
                        System.Text.Encoding.UTF8.GetString(_distributedCache.Get(l.Key))));
                });
                return r;
            });
        }
        
        public async Task PurgeJobs()
        {
            await Task.Factory.StartNew(() =>
            {
                var list = _jobToUser.ToList();
                list.ForEach(async l =>
                {
                    await _distributedCache.RemoveAsync(l.Key);
                });
            });
        }
    }
}