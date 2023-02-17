using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using PikaCore.Infrastructure.Services;

namespace PikaCore.Areas.Core.Controllers.App
{
    [Area("Core")]
    [ResponseCache(CacheProfileName = "Default")]
    public class JobsController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IDistributedCache _distributedCache;
        private readonly ISchedulerService _schedulerService;
        
        public JobsController(IConfiguration configuration,
            IDistributedCache distributedCache,
            ISchedulerService schedulerService)
        {
            _configuration = configuration;
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
           
            return RedirectToAction(nameof(Index));
        }
    }
}