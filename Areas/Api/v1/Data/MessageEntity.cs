using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using PikaCore.Areas.Core.Pages.Admin.DTO;

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
        [DisplayName("Date created")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [Required]
        [DisplayName("Date last updated")]
        public DateTime UpdatedAt { get; set; }

        [Required] 
        [NotNull] 
        [StringLength(1000, ErrorMessage = "Message should not be longer than {1} and has at least {2}", MinimumLength = 10)]
        [DisplayName("Message content")]
        public string Message { get; set; } = "";
        
        [Required]
        [DisplayName("Message type")]
        public MessageType MessageType { get; set; } = MessageType.None;
        
        [Required]
        [DisplayName("Message related issues")]
        public IList<IssueEntity> RelatedIssues { get; set; } = new List<IssueEntity>();

        [Required] 
        [DisplayName("Is visible to public?")]
        [NotNull]
        public bool IsVisible { get; set; } = true;

        public MessageDTO ToMessageDto()
        {
            return new MessageDTO()
            {
                Id = Id,
                Content = Message,
                IsVisible = IsVisible,
                RelatedIssueCount = RelatedIssues.Count,
                MessageType = MessageType
            };
        }
    }
}