namespace PikaCore.Areas.Core.Models.AccountViewModels
{
    public class LoginResultModel
    {
        public bool Success { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
    }
}