using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace PikaCore.Areas.Infrastructure.Data
{
    [Table("Issues")]
    public class IssueEntity
    {
        [Key]
        [Required]
        [NotNull]
        public int Id { get; set; }

        [Required]
        [NotNull]
        public string IssueDetails { get; set; } = "";

        [Required]
        [NotNull]
        public string IssuedComponents { get; set; } = "";

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        [NotNull]
        public DateTime UpdatedAt { get; set; }

        [Required] 
        public bool IsFixed { get; set; } = false;
    }
}