using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using PikaCore.Infrastructure.Services;
using PikaCore.Infrastructure.Services.Helpers;

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