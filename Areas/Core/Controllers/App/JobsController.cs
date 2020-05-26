using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using PikaCore.Areas.Core.Services;

namespace PikaCore.Areas.Core.Controllers.App
{
    public class JobsController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IJobService _jobService;
        private readonly IDistributedCache _distributedCache;
        
        public JobsController(IConfiguration configuration,
            IJobService jobService,
            IDistributedCache distributedCache)
        {
            _configuration = configuration;
            _jobService = jobService;
            _distributedCache = distributedCache;
        }

        public IActionResult Index()
        {
            
            return View();
        }
    }
}