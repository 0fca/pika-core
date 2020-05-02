using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace PikaCore.Areas.Api.v1.Data
{
    [Table("Messages")]
    public class MessageEntity
    {
        [Key]
        [Required]
        [NotNull]
        public int Id { get; set; }
        
        [Required]
        [NotNull]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [Required]
        [NotNull]
        public DateTime UpdatedAt { get; set; }

        [Required] 
        [NotNull] 
        [StringLength(1000, ErrorMessage = "Message should not be longer than {1} and has at least {2}", MinimumLength = 10)]
        public string Message { get; set; } = "";
        
        [Required]
        public MessageType MessageType { get; set; } = MessageType.None;
        
        [Required]
        public IList<IssueEntity> RelatedIssues { get; set; } = new List<IssueEntity>();

        [Required] 
        public bool IsVisible { get; set; } = true;
    }
}