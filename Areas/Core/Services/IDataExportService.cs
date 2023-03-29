using System.Collections.Generic;

namespace PikaCore.Areas.Core.Services
{
    internal interface IDataExportService
    {
        void ExportData(IList<string> dataCollection, string userId);
        string RetrieveFilePath(string userId);
    }
}