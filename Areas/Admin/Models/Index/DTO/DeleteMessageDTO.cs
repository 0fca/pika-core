using System.Collections.Generic;
using System.Linq;
using Pika.Domain.Status.Data;

namespace PikaCore.Areas.Admin.Models.Index.DTO;

public class DeleteMessageDto
{
    public Dictionary<int, string> Messages { get; set; } = new();

    public static Dictionary<int, string> MessageListToDto(IList<MessageEntity> messageEntities)
    {
        return messageEntities.ToDictionary(message => message.Id, message => message.Message);
    }
}