using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Quartz;

namespace PikaCore.Areas.Core.Services.Jobs
{
    public class ArchiveJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            string output = dataMap.GetString("output");
            string absolutePath = dataMap.GetString("absolutePath");
            
            if (File.Exists(output))
            {
                return;
            }
            await Task.Factory.StartNew(() => 
                ZipFile.CreateFromDirectory(absolutePath, output, CompressionLevel.Fastest, false)
            );
        }
    }
}