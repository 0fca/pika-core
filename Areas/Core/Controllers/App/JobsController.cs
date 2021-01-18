using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using PikaCore.Areas.Core.Services;
using PikaCore.Infrastructure.Services;
using Quartz;

namespace PikaCore.Areas.Core.Controllers.App
{
    [Area("Core")]
    public class JobsController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IJobService _jobService;
        private readonly IDistributedCache _distributedCache;
        private readonly ISchedulerService _schedulerService;
        
        public JobsController(IConfiguration configuration,
            IJobService jobService,
            IDistributedCache distributedCache,
            ISchedulerService schedulerService)
        {
            _configuration = configuration;
            _jobService = jobService;
            _distributedCache = distributedCache;
            _schedulerService = schedulerService;
            _schedulerService.Init();
        }
        
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        
        [HttpGet]
        public IActionResult Submit(string name)
        {
            var trigger = TriggerBuilder.Create()
                .WithIdentity("trigger1", "group1")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(10))
                .Build();

            _schedulerService.StartJob(name, trigger);
            return RedirectToAction(nameof(Index));
        }
    }
}