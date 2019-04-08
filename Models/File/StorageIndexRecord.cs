using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace FMS2.Models
{
    public class StorageIndexRecord
    {
        [Key]
        [Required]
        public int Urlid {get; set;}
        [Required]
        public string Urlhash {get; set;}
        [Required]
        public string AbsolutePath {get; set;}
        [Required]
        public string UserId {get;set;}

        [Required]
        public bool Expires {get; set;}

        [Required]
        public DateTime ExpireDate {get; set;}
    }
}
