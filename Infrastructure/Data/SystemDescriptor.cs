using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace PikaCore.Infrastructure.Data
{
    [Table("Systems")]
    public class SystemDescriptor
    {
        [Key]
        [Required]
        [NotNull]
        public int SystemId { get; set; }

        [Required]
        public string Address { get; set; } = "localhost";
        
        [Required]
        [NotNull]
        public int Port { get; set; }
        
        [Required] 
        [NotNull] 
        public List<SystemDependency> Dependencies { get; set; }

        [Required] 
        [NotNull] 
        public string SystemName { get; set; } = "";
    }
}