using System;

namespace FMS2.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public int ErrorCode {get; set;} = 500;
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}