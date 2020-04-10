using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace PikaCore.Models.File
{
    public class RenameFileModel
    {
        [Required]
        public string OldName { get; set; }
        [Required]
        public string NewName { get; set; }
        [Required]
        public bool IsDirectory { get; set; }
        [Required]
        public string AbsoluteParentPath { get; set; }
        public string ReturnUrl { get; set; }
    }
}