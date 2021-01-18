using System;
using System.Collections.Generic;
using System.Linq;
using PikaCore.Infrastructure.Data;

namespace PikaCore.Areas.Admin.Models.Index
{
    public class MessageViewModel
    {
        public IList<MessageEntity> Messages { get; set; } = new List<MessageEntity>();
        public int PageCount { get; set; } = 1;

        public void OrganizeMessages(ref List<MessageEntity> messages, int messagesPerPageCount)
        {
            messages = messages.OrderBy(m => m.Id).ToList();
            PageCount = (int)Math.Round(messages.Count / (float) messagesPerPageCount, MidpointRounding.AwayFromZero);
        }
    }
}