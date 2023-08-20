using System.Diagnostics;

namespace PikaCore.Areas.Core.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; } = Activity.Current?.Id ?? "No data";
        public int ErrorCode { get; set; } = 404;
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public string Url { get; set; } = "Unknown";
        public string Message { get; set; } = "Unknown error occured.";
        public string ContentType { get; set; } = "text/html";
    }
}