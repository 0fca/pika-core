using System.Collections.Generic;
using System.Linq;
using PikaCore.Areas.Api.v1.Data;

namespace PikaCore.Areas.Core.Models.AdminViewModels.DTO
{
    public class DeleteMessageDto
    {
        public Dictionary<int, string> Messages { get; set; } = new Dictionary<int, string>();

        public static Dictionary<int, string> MessageListToDto(IList<MessageEntity> messageEntities)
        {
            return messageEntities.ToDictionary(message => message.Id, message => message.Message);
        }
    }
}