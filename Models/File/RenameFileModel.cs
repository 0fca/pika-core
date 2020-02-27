namespace PikaCore.Models.File
{
    public class RenameFileModel
    {
        public string OldName { get; set; }
        public string NewName { get; set; }
        public bool IsDirectory { get; set; }
        public string AbsolutePath { get; set; }
    }
}