using System;
using System.ComponentModel.DataAnnotations;

namespace FMS2.Models
{
    public class StorageIndexRecord
    {
        [Key]
        [Required]
        public int Urlid { get; set; }
        [Required]
        public string Urlhash { get; set; }
        [Required]
        public string AbsolutePath { get; set; }
        [Required]
        public string UserId { get; set; }

        [Required]
        public bool Expires { get; set; }

        [Required]
        public DateTime ExpireDate { get; set; }
    }
}
