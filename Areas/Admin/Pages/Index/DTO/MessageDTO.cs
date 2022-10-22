using System.Collections.Generic;
using Pika.Domain.Status.Data;
using Pika.Domain.Status.Models;

namespace PikaCore.Areas.Admin.Pages.Index.DTO
{
    public class MessageDTO
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public bool IsVisible { get; set; }

        public MessageType MessageType { get; set; } = MessageType.None;

        public int RelatedIssueCount { get; set; } = 0;

        public List<SystemDescriptor> Systems { get; set; } = new List<SystemDescriptor>();

        public string SystemName { get; set; } = "";

        public MessageEntity ToMessageEntity()
        {
            return new MessageEntity()
            {
                Id = Id,
                Message = this.Content,
                IsVisible = IsVisible,
                MessageType = MessageType
            };
        }

        public static MessageDTO FromMessageEntity(MessageEntity messageEntity)
        {
            return new MessageDTO
            {
                Id = messageEntity.Id,
                Content = messageEntity.Message,
                MessageType = messageEntity.MessageType,
                SystemName = messageEntity.SystemDescriptor.SystemName
            };
        }
    }
}