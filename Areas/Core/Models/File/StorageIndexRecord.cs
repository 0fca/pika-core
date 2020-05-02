using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PikaCore.Areas.Core.Controllers.App;

namespace PikaCore.Areas.Core.Models.File
{
    public class StorageIndexRecord
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
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

        [Required] public DateTime ExpireDate { get; set; } = ComputeDateTime();

        private static DateTime ComputeDateTime()
        {
            var now = DateTime.Now;
            now = now.AddDays(Constants.DayCount);
            return now;
        }
    }
}
