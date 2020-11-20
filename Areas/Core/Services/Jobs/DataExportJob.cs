using System.IO;
using System.Threading.Tasks;
using NReco.Csv;
using Quartz;

namespace PikaCore.Areas.Core.Services.Jobs
{
    public class DataExportJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var userId = dataMap.GetString("uid");
            var exportPath = dataMap.GetString("exportPath");
            var collectionNames = dataMap.GetString("data");
            if (!Directory.Exists(exportPath))
            {
                Directory.CreateDirectory(exportPath);
            }

            await using var streamWriter = new StreamWriter(Path.Combine(exportPath, "export_data.csv"));
            var writer = new CsvWriter(streamWriter);
            
        }
    }
}