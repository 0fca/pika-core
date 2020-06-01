using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace PikaCore.Areas.Infrastructure.Data
{
    public class SystemDependency
    {
        [Key]
        [Required]
        [NotNull]
        public int Id { get; set; }

        [Required]
        [NotNull]
        public SystemDescriptor SystemDescriptor { get; set; }
    }
}