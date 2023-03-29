namespace PikaCore.Areas.Identity.Models.AccountViewModels
{
    public class LoginResultModel
    {
        public bool Success { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
    }
}