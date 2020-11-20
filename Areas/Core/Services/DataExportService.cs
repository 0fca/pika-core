using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PikaCore.Areas.Infrastructure.Services;
using PikaCore.Areas.Infrastructure.Services.Helpers;
using Quartz;

namespace PikaCore.Areas.Core.Services
{
    public class DataExportService : IDataExportService
    {
        private readonly ISchedulerService _schedulerService;
        private readonly IConfiguration _configuration;
        
        public DataExportService(ISchedulerService schedulerService, 
                                 IConfiguration configuration)
        {
            _schedulerService = schedulerService;
            _configuration = configuration;
        }

        public void ExportData(IList<string> dataCollections, string userId)
        {
            var triggerFactory = TriggerFactory.GetInstance();
            var triggerIdentity = new TriggerIdentity()
            {
                Name = Guid.NewGuid().ToString()
            };
            var scheduleBuilder = SimpleScheduleBuilder.Create().WithRepeatCount(0);
            var triggerData = new TriggerData()
            {
                When = DateTimeOffset.Now,
                ShouldStartNow = true,
                TriggerIdentity = triggerIdentity,
                ScheduleBuilder = scheduleBuilder
            };
            var trigger = triggerFactory.CreateTrigger(triggerData);
            trigger.JobDataMap.Add("data", "");
            trigger.JobDataMap.Add("exportPath", 
                Path.Combine(_configuration.GetSection("Storage:exportPath").Value, userId));
            trigger.JobDataMap.Add("uid", userId);
            _schedulerService.Init();
            _schedulerService.StartJob(string.Concat("DataExportJob_", userId), trigger);
        }

        public string RetrieveFilePath(string userId)
        {
            return Path.Combine(_configuration.GetSection("Storage:exportPath").Value, userId, "export_data.csv");
        }
        
        #region HelperMethods

        public string RetrieveSerializedData()
        {
            return "";
        }

        #endregion
    }
}