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
        public int urlid {get; set;}
        [Required]
        public string urlhash {get; set;}
        [Required]
        public string absolute_path {get; set;}
        [Required]
        public string user_id {get;set;}

        [Required]
        public bool expires {get; set;}

        [Required]
        public DateTime expire_date {get; set;}
    }
}
