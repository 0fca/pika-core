namespace PikaCore.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public int ErrorCode { get; set; } = 500;
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public string Url { get; set; }
        public string Message { get; set; } = "Unknown error occured.";
    }
}