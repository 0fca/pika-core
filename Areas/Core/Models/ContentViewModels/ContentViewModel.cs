namespace PikaCore.Areas.Core.Models.ContentViewModels
{
    public class ContentViewModel
    {
        public string? PresentableName { get; set; }
        public string? TempFileId { get; set; }
        public string? DataType { get; set; }

        public string ReturnUrl { get; set; } = "/Core/Storage/Browse";
    }
}