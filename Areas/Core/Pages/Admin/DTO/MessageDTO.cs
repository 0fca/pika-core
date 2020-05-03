using PikaCore.Areas.Api.v1.Data;

namespace PikaCore.Areas.Core.Pages.Admin.DTO
{
    public class MessageDTO
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public bool IsVisible { get; set; }

        public MessageType MessageType { get; set; } = MessageType.None;

        public int RelatedIssueCount { get; set; } = 0;

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
    }
}